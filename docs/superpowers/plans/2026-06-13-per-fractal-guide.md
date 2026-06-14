# Per-Fractal Guide Window + Schema Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a non-modal, read-only "Guide" window that explains the fractal currently being viewed (what it is, how Parsec computes it, brief math, every setting with its live range, and tuning tips), and fix the schema bugs/range gaps the audit surfaced.

**Architecture:** A pure, unit-tested content layer (`FractalGuide` builds a `GuideDocument` data model from per-fractal `GuideContent` plus the live `ParamSchema`) feeds a thin Avalonia `GuideWindow` renderer. `MainWindow` owns one window instance and refreshes it when the active fractal/formula changes. Schema edits land first so the guide auto-shows corrected ranges. The fractal `*State.cs` classes carry both the editable defaults and the slider ranges.

**Tech Stack:** C# / .NET 8, Avalonia UI, xUnit (new test project). No new runtime dependencies.

**Source of truth for content:** `docs/superpowers/research/fractals-1..6-*.md` and `docs/superpowers/research/00-audit-summary.md` (already committed). Calculation prose comes from each file's "How Parsec computes it"; setting notes from the per-parameter audit tables; tips from "For best results."

---

## File Structure

New files:
- `src/Parsec.App/GuideTypes.cs` — `GuideBlock`, `GuideDocument`, `GuideContent` data types.
- `src/Parsec.App/FractalGuide.cs` — registry + shared notes + pure `Build`/`BuildDocument`/helpers.
- `src/Parsec.App/GuideContent.Registry.cs` — the 22 fractal + 4 deep-formula `GuideContent` entries (kept in its own file because it is large prose data).
- `src/Parsec.App/GuideWindow.cs` — read-only Avalonia window renderer.
- `src/Parsec.App.Tests/Parsec.App.Tests.csproj` — xUnit project.
- `src/Parsec.App.Tests/SchemaAuditTests.cs` — schema default/range/Reset tests.
- `src/Parsec.App.Tests/GuideBuilderTests.cs` — pure builder tests.
- `src/Parsec.App.Tests/GuideContentCoverageTests.cs` — content coverage tests.

Modified files (schema cleanup): `MandelboxState.cs`, `OrbitHybridState.cs`, `QuaternionJuliaState.cs`, `MoselyState.cs`, `AttractorState.cs`, `MandalayState.cs`, `KifsState.cs`, `MengerState.cs`, `MandelbulbState.cs`, `QJBoxState.cs`, `BiomorphState.cs`, `KleinianState.cs`, `PseudoKleinian4DState.cs`, `ApollonianState.cs`.

Modified files (feature): `MainWindow.axaml`, `MainWindow.axaml.cs`, `Parsec.sln`.

---

## Task 1: Create the xUnit test project

**Files:**
- Create: `src/Parsec.App.Tests/Parsec.App.Tests.csproj`
- Create: `src/Parsec.App.Tests/SmokeTests.cs`
- Modify: `Parsec.sln`

- [ ] **Step 1: Write the test project file**

Create `src/Parsec.App.Tests/Parsec.App.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Parsec.App\Parsec.App.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write a smoke test**

Create `src/Parsec.App.Tests/SmokeTests.cs`:

```csharp
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class SmokeTests
{
    [Fact]
    public void StateClassExposesSchema()
    {
        var schema = new MandelboxState().BuildSchema();
        Assert.NotEmpty(schema.Parameters);
    }
}
```

- [ ] **Step 3: Add the project to the solution**

Run: `dotnet sln Parsec.sln add src/Parsec.App.Tests/Parsec.App.Tests.csproj`
Expected: "Project ... added to the solution."

- [ ] **Step 4: Run the test**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj`
Expected: PASS (1 test passed). If Avalonia headless init errors occur, none should: the test only touches `MandelboxState`, which has no Avalonia dependency.

- [ ] **Step 5: Commit**

```bash
git add src/Parsec.App.Tests Parsec.sln
git commit -m "test: add xUnit test project for Parsec.App"
```

---

## Task 2: Fix Reset() bugs (B2, B3)

**Files:**
- Modify: `src/Parsec.App/QuaternionJuliaState.cs` (Reset)
- Modify: `src/Parsec.App/MoselyState.cs` (Reset)
- Test: `src/Parsec.App.Tests/SchemaAuditTests.cs`

- [ ] **Step 1: Write failing tests**

Create `src/Parsec.App.Tests/SchemaAuditTests.cs`:

```csharp
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class SchemaAuditTests
{
    private static ParamDescriptor Desc(ParamSchema s, string label)
        => s.Parameters.Single(p => p.Label == label);

    [Fact]
    public void QuaternionJulia_Reset_restores_stereo_params()
    {
        var s = new QuaternionJuliaState { StereoK = 2.5f, StereoR = 1.4f };
        s.Reset();
        Assert.Equal(1.0f, s.StereoK);
        Assert.Equal(0.8f, s.StereoR);
    }

    [Fact]
    public void Mosely_Reset_restores_wedge_and_fudge()
    {
        var s = new MoselyState { WedgeDeg = 120f, Fudge = 0.5f };
        s.Reset();
        Assert.Equal(360f, s.WedgeDeg);
        Assert.Equal(0.9f, s.Fudge);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: FAIL (StereoK is 1.0 only by luck of field init; the mutate-then-Reset asserts fail because Reset does not reset these — StereoK stays 2.5, WedgeDeg stays 120).

- [ ] **Step 3: Fix QuaternionJulia.Reset**

In `src/Parsec.App/QuaternionJuliaState.cs`, find in `Reset()`:

```csharp
        Fudge = 0.9f;
        Stereo = 0;
    }}
```

Replace with:

```csharp
        Fudge = 0.9f;
        Stereo = 0;
        StereoK = 1.0f;
        StereoR = 0.8f;
    }}
```

- [ ] **Step 4: Fix Mosely.Reset**

In `src/Parsec.App/MoselyState.cs`, find in `Reset()`:

```csharp
        TwistDeg = 0f;
    }}
```

Replace with:

```csharp
        TwistDeg = 0f;
        WedgeDeg = 360f;
        Fudge = 0.9f;
    }}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Parsec.App/QuaternionJuliaState.cs src/Parsec.App/MoselyState.cs src/Parsec.App.Tests/SchemaAuditTests.cs
git commit -m "fix(state): restore all defaults in QuaternionJulia and Mosely Reset"
```

---

## Task 3: Widen slider ranges (B1, R1, R3, R4, R7, R8)

**Files:**
- Modify: `OrbitHybridState.cs`, `MandelboxState.cs`, `MandalayState.cs`, `KifsState.cs`, `KleinianState.cs`, `PseudoKleinian4DState.cs`, `ApollonianState.cs`
- Test: `src/Parsec.App.Tests/SchemaAuditTests.cs`

- [ ] **Step 1: Add failing range tests**

Append to the `SchemaAuditTests` class in `src/Parsec.App.Tests/SchemaAuditTests.cs`:

```csharp
    [Fact]
    public void OrbitHybrid_bound_radius_max_reaches_default()
    {
        var s = new OrbitHybridState();
        var d = Desc(s.BuildSchema(), "Bound radius");
        Assert.True(d.Max >= 16.0, $"Max {d.Max} must reach default 16.0");
    }

    [Fact]
    public void Mandelbox_scale_max_allows_cityscape()
    {
        Assert.Equal(3.0, Desc(new MandelboxState().BuildSchema(), "Scale").Max, 3);
    }

    [Fact]
    public void Mandalay_scale_range_is_negative_only()
    {
        var d = Desc(new MandalayState().BuildSchema(), "Scale");
        Assert.Equal(-3.0, d.Min, 3);
        Assert.Equal(-0.5, d.Max, 3);
    }

    [Theory]
    [InlineData("Post-rot X")]
    [InlineData("Pre-rot X")]
    public void Kifs_rotation_range_is_plus_minus_90(string label)
    {
        var d = Desc(new KifsState().BuildSchema(), label);
        Assert.Equal(-90.0, d.Min, 3);
        Assert.Equal(90.0, d.Max, 3);
    }

    [Fact]
    public void Kleinian_and_pk4d_de_fudge_capped_at_one()
    {
        Assert.Equal(1.0, Desc(new KleinianState().BuildSchema(), "DE fudge").Max, 3);
        Assert.Equal(1.0, Desc(new PseudoKleinian4DState().BuildSchema(), "DE fudge").Max, 3);
    }

    [Fact]
    public void Apollonian_outer_radius_min_avoids_clipping()
    {
        Assert.Equal(0.95, Desc(new ApollonianState().BuildSchema(), "Outer radius x").Min, 3);
    }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: FAIL on the new range tests (current Maxes/Mins are the old values).

- [ ] **Step 3: Edit OrbitHybridState.cs (B1)**

Find: `Label = "Bound radius", Group = "Quality", Min = 2.0, Max = 10.0, Decimals = 1,`
Replace: `Label = "Bound radius", Group = "Quality", Min = 2.0, Max = 20.0, Decimals = 1,`

- [ ] **Step 4: Edit MandelboxState.cs (R1)**

Find: `Label = "Scale", Group = "Fold", Min = -3.0, Max = 2.0, Decimals = 2,`
Replace: `Label = "Scale", Group = "Fold", Min = -3.0, Max = 3.0, Decimals = 2,`

- [ ] **Step 5: Edit MandalayState.cs (R3)**

Find: `Label = "Scale", Group = "Form", Min = -3.0, Max = 3.0, Decimals = 2,`
Replace: `Label = "Scale", Group = "Form", Min = -3.0, Max = -0.5, Decimals = 2,`

- [ ] **Step 6: Edit KifsState.cs (R4)**

In `src/Parsec.App/KifsState.cs` replace all six occurrences of `Min = -45, Max = 45` with `Min = -90, Max = 90` (the Post-rot X/Y/Z and Pre-rot X/Y/Z descriptors; all six lines are identical so a replace-all on that substring is correct).

- [ ] **Step 7: Edit KleinianState.cs and PseudoKleinian4DState.cs (R7)**

In both files find: `Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 2.0, Decimals = 2,`
Replace: `Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 1.0, Decimals = 2,`

- [ ] **Step 8: Edit ApollonianState.cs (R8)**

Find: `Min = 0.85, Max = 1.5, Decimals = 3,`
Replace: `Min = 0.95, Max = 1.5, Decimals = 3,`

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: PASS (all range tests green).

- [ ] **Step 10: Commit**

```bash
git add src/Parsec.App/OrbitHybridState.cs src/Parsec.App/MandelboxState.cs src/Parsec.App/MandalayState.cs src/Parsec.App/KifsState.cs src/Parsec.App/KleinianState.cs src/Parsec.App/PseudoKleinian4DState.cs src/Parsec.App/ApollonianState.cs src/Parsec.App.Tests/SchemaAuditTests.cs
git commit -m "fix(state): widen/narrow slider ranges to cover proper viewing variety"
```

---

## Task 4: Fix defaults (R2, R5, R6) and their Reset values

**Files:**
- Modify: `AttractorState.cs`, `MengerState.cs`, `MandelbulbState.cs`, `QuaternionJuliaState.cs`, `QJBoxState.cs`, `BiomorphState.cs`
- Test: `src/Parsec.App.Tests/SchemaAuditTests.cs`

- [ ] **Step 1: Add failing default tests**

Append to `SchemaAuditTests`:

```csharp
    [Fact]
    public void Thomas_damping_default_is_chaotic_and_capped()
    {
        var fresh = new AttractorState();
        Assert.Equal(0.19f, fresh.B);
        var mutated = new AttractorState { B = 0.30f };
        mutated.Reset();
        Assert.Equal(0.19f, mutated.B);
        Assert.Equal(0.30, Desc(fresh.BuildSchema(), "Damping b").Max, 3);
    }

    [Fact]
    public void Menger_offset_z_default_is_canonical_sponge()
    {
        var fresh = new MengerState();
        Assert.Equal(1.0f, fresh.OffsetZ);
        var mutated = new MengerState { OffsetZ = 0.0f };
        mutated.Reset();
        Assert.Equal(1.0f, mutated.OffsetZ);
    }

    [Fact]
    public void Mandelbulb_defaults_bumped()
    {
        var fresh = new MandelbulbState();
        Assert.Equal(10, fresh.Iterations);
        Assert.Equal(8.0f, fresh.Bailout);
        var m = new MandelbulbState { Iterations = 4, Bailout = 4.0f };
        m.Reset();
        Assert.Equal(10, m.Iterations);
        Assert.Equal(8.0f, m.Bailout);
    }

    [Fact]
    public void Iteration_defaults_bumped()
    {
        Assert.Equal(12, new QuaternionJuliaState().Iterations);
        Assert.Equal(10, new QJBoxState().Iterations);
        Assert.Equal(24, new BiomorphState().Iterations);
    }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: FAIL on the four new tests.

- [ ] **Step 3: Edit AttractorState.cs (R2)**

Find field: `public float B = 0.208186f;`  →  `public float B = 0.19f;`
Find in `Reset()`: `B = 0.208186f;`  →  `B = 0.19f;`
Find slider: `Label = "Damping b", Group = "Attractor (Generate)", Min = 0.05, Max = 0.35, Decimals = 3,`  →  same line with `Max = 0.30`.

- [ ] **Step 4: Edit MengerState.cs (R5)**

Find field: `public float OffsetZ = 0.0f;`  →  `public float OffsetZ = 1.0f;`
Find in `Reset()`: `OffsetZ = 0.0f;`  →  `OffsetZ = 1.0f;`

- [ ] **Step 5: Edit MandelbulbState.cs (R6)**

Find field: `public int Iterations = 8;`  →  `public int Iterations = 10;`
Find field: `public float Bailout = 4.0f;`  →  `public float Bailout = 8.0f;`
Find in `Reset()`: `Iterations = 8;`  →  `Iterations = 10;`
Find in `Reset()`: `Bailout = 4.0f;`  →  `Bailout = 8.0f;`

- [ ] **Step 6: Edit QuaternionJuliaState.cs (R6)**

Find field: `public int Iterations = 10;`  →  `public int Iterations = 12;`
Find in `Reset()`: `Iterations = 10;`  →  `Iterations = 12;`

- [ ] **Step 7: Edit QJBoxState.cs (R6)**

Find field: `public int Iterations = 8;`  →  `public int Iterations = 10;`
Find in `Reset()`: `Iterations = 8;`  →  `Iterations = 10;`

- [ ] **Step 8: Edit BiomorphState.cs (R6)**

Find field: `public int Iterations = 16;`  →  `public int Iterations = 24;`
Find in `Reset()`: `Iterations = 16;`  →  `Iterations = 24;`

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "SchemaAuditTests"`
Expected: PASS (all SchemaAuditTests green).

- [ ] **Step 10: Commit**

```bash
git add src/Parsec.App/AttractorState.cs src/Parsec.App/MengerState.cs src/Parsec.App/MandelbulbState.cs src/Parsec.App/QuaternionJuliaState.cs src/Parsec.App/QJBoxState.cs src/Parsec.App/BiomorphState.cs src/Parsec.App.Tests/SchemaAuditTests.cs
git commit -m "fix(state): improve out-of-the-box defaults (Thomas, Menger, iteration counts)"
```

---

## Task 5: Guide data types

**Files:**
- Create: `src/Parsec.App/GuideTypes.cs`

- [ ] **Step 1: Write the types**

Create `src/Parsec.App/GuideTypes.cs`:

```csharp
using System.Collections.Generic;

namespace Parsec.App;

/// <summary>One renderable block of a fractal guide. The window maps each variant
/// to a styled, read-only text control.</summary>
public abstract record GuideBlock
{
    public sealed record Heading(string Text) : GuideBlock;
    public sealed record Paragraph(string Text) : GuideBlock;
    public sealed record SettingGroupHeading(string Group) : GuideBlock;
    public sealed record SettingDefinition(string Name, string Range, string Note) : GuideBlock;
}

/// <summary>Render-ready guide: a title plus an ordered list of blocks. Produced by
/// <see cref="FractalGuide.Build"/> from the live schema; consumed by GuideWindow.</summary>
public sealed record GuideDocument(string Title, IReadOnlyList<GuideBlock> Blocks);

/// <summary>Hand-written guide prose for one fractal (or one deep-zoom formula).
/// The settings list itself is auto-derived from the live schema; this only supplies
/// the per-setting NOTE text, keyed by the exact ParamDescriptor.Label.</summary>
public sealed record GuideContent
{
    public required string Title { get; init; }
    public required IReadOnlyList<string> WhatItIs { get; init; }
    public required IReadOnlyList<string> HowComputed { get; init; }
    public IReadOnlyList<string> Math { get; init; } = new List<string>();
    public required IReadOnlyList<string> BestResults { get; init; }
    public IReadOnlyDictionary<string, string> SettingNotes { get; init; }
        = new Dictionary<string, string>();
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Parsec.App/Parsec.App.csproj -c Debug`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Parsec.App/GuideTypes.cs
git commit -m "feat(guide): add guide data model types"
```

---

## Task 6: Pure document builder

**Files:**
- Create: `src/Parsec.App/FractalGuide.cs` (Build/BuildDocument/helpers + empty registry stubs)
- Test: `src/Parsec.App.Tests/GuideBuilderTests.cs`

- [ ] **Step 1: Write failing builder tests**

Create `src/Parsec.App.Tests/GuideBuilderTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class GuideBuilderTests
{
    private static ParamSchema TwoGroupSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor { Label = "Scale", Group = "Fold", Min = -3.0, Max = 3.0,
                Decimals = 2, Get = () => 0, Set = _ => { } },
            new ParamDescriptor { Label = "Iterations", Group = "Quality", Min = 4, Max = 500,
                Step = 1, Decimals = 0, Get = () => 0, Set = _ => { } },
            new ParamDescriptor { Label = "Gloss", Group = "Reflections", Min = 0, Max = 1,
                Decimals = 2, Get = () => 0, Set = _ => { } },
        },
    };

    private static GuideContent Content() => new()
    {
        Title = "Test Fractal",
        WhatItIs = new[] { "It is a test." },
        HowComputed = new[] { "Folded in a loop." },
        BestResults = new[] { "Turn the knobs." },
        SettingNotes = new Dictionary<string, string> { ["Scale"] = "Overall fold scale." },
    };

    [Fact]
    public void Document_title_comes_from_content()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        Assert.Equal("Test Fractal", doc.Title);
    }

    [Fact]
    public void Settings_emitted_in_schema_group_order_with_group_headings()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var groups = doc.Blocks.OfType<GuideBlock.SettingGroupHeading>().Select(g => g.Group).ToList();
        Assert.Equal(new[] { "Fold", "Quality", "Reflections" }, groups);
        Assert.Equal(3, doc.Blocks.OfType<GuideBlock.SettingDefinition>().Count());
    }

    [Fact]
    public void Range_string_uses_decimals_and_step()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var defs = doc.Blocks.OfType<GuideBlock.SettingDefinition>().ToDictionary(d => d.Name);
        Assert.Equal("range -3.00 .. 3.00", defs["Scale"].Range);
        Assert.Equal("range 4 .. 500, step 1", defs["Iterations"].Range);
    }

    [Fact]
    public void Note_lookup_prefers_content_then_shared_then_empty()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var defs = doc.Blocks.OfType<GuideBlock.SettingDefinition>().ToDictionary(d => d.Name);
        Assert.Equal("Overall fold scale.", defs["Scale"].Note);          // content-specific
        Assert.NotEqual("", defs["Gloss"].Note);                           // shared table
        Assert.Equal("", defs["Iterations"].Note);                        // graceful fallback
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "GuideBuilderTests"`
Expected: FAIL to compile (`FractalGuide` does not exist yet).

- [ ] **Step 3: Write FractalGuide with builder + shared notes (registry stubbed)**

Create `src/Parsec.App/FractalGuide.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Parsec.App;

/// <summary>
/// Builds a read-only <see cref="GuideDocument"/> for the active fractal. The
/// settings section is auto-derived from the live <see cref="ParamSchema"/> so it
/// never drifts from the real sliders; per-setting NOTE text and the prose come
/// from the hand-written <see cref="GuideContent"/> registry (see
/// GuideContent.Registry.cs).
/// </summary>
public static partial class FractalGuide
{
    /// <summary>Resolve content for the active fractal/formula and render it against
    /// the live schema.</summary>
    public static GuideDocument Build(FractalType type, int deepFormula, ParamSchema schema)
        => BuildDocument(Resolve(type, deepFormula), schema);

    /// <summary>Pure: turn explicit content + a schema into render-ready blocks.
    /// No Avalonia, no GL — this is the unit-tested seam.</summary>
    public static GuideDocument BuildDocument(GuideContent content, ParamSchema schema)
    {
        var blocks = new List<GuideBlock>();

        foreach (var p in content.WhatItIs) blocks.Add(new GuideBlock.Paragraph(p));

        blocks.Add(new GuideBlock.Heading("How Parsec computes it"));
        foreach (var p in content.HowComputed) blocks.Add(new GuideBlock.Paragraph(p));

        if (content.Math.Count > 0)
        {
            blocks.Add(new GuideBlock.Heading("The math"));
            foreach (var p in content.Math) blocks.Add(new GuideBlock.Paragraph(p));
        }

        blocks.Add(new GuideBlock.Heading("Settings"));
        foreach (var group in schema.Groups)
        {
            blocks.Add(new GuideBlock.SettingGroupHeading(group));
            foreach (var d in schema.InGroup(group))
                blocks.Add(new GuideBlock.SettingDefinition(d.Label, FormatRange(d), NoteFor(content, d.Label)));
        }

        blocks.Add(new GuideBlock.Heading("For best results"));
        foreach (var p in content.BestResults) blocks.Add(new GuideBlock.Paragraph(p));

        return new GuideDocument(content.Title, blocks);
    }

    internal static string FormatRange(ParamDescriptor d)
    {
        string fmt = "F" + d.Decimals;
        string lo = d.Min.ToString(fmt, CultureInfo.InvariantCulture);
        string hi = d.Max.ToString(fmt, CultureInfo.InvariantCulture);
        string range = $"range {lo} .. {hi}";
        if (d.Step > 0)
            range += $", step {d.Step.ToString(fmt, CultureInfo.InvariantCulture)}";
        return range;
    }

    internal static string NoteFor(GuideContent content, string label)
    {
        if (content.SettingNotes.TryGetValue(label, out var note)) return note;
        if (SharedSettingNotes.TryGetValue(label, out var shared)) return shared;
        return "";
    }

    /// <summary>Notes for the cross-fractal groups (Palette, Reflections, Light,
    /// Camera). Written once, reused for every fractal.</summary>
    public static readonly IReadOnlyDictionary<string, string> SharedSettingNotes =
        new Dictionary<string, string>
        {
            // Color: bands / phase / base / amplitude / trap mix
            ["Frequency"] = "How many color bands the cosine palette packs across the orbit-trap value. Higher = tighter stripes.",
            ["Trap scale"] = "Scales the orbit-trap distance before coloring. Shifts where bands land on the surface.",
            ["Phase R"] = "Red channel phase offset of the cosine palette. Rotates the red band position.",
            ["Phase G"] = "Green channel phase offset. Rotates the green band position.",
            ["Phase B"] = "Blue channel phase offset. Rotates the blue band position.",
            ["Base R"] = "Red midpoint of the palette (the color when the cosine is zero).",
            ["Base G"] = "Green midpoint of the palette.",
            ["Base B"] = "Blue midpoint of the palette.",
            ["Amp R"] = "Red swing around the base. Higher = more saturated red contrast.",
            ["Amp G"] = "Green swing around the base.",
            ["Amp B"] = "Blue swing around the base.",
            ["Mix origin"] = "Weight of the origin orbit-trap in the color (structural banding toward the center).",
            ["Mix axis"] = "Weight of the axis orbit-trap (banding along the fold axes).",
            ["Mix plane"] = "Weight of the plane orbit-trap (banding across cut planes).",
            ["Shell glaze"] = "Blends a thin bright shell over the surface for a glazed look.",
            // Reflections
            ["Reflection bounces (0=off)"] = "Reflection bounce depth. 0 keeps the fast single-bounce look; 1-3 add mirror reflections (3 is hero-quality and slowest).",
            ["Gloss"] = "Overall reflection strength. 0 = matte, 1 = full gloss.",
            ["Fresnel F0 (0.05 ceramic … 0.8 metal)"] = "Base reflectivity. ~0.05 reads as ceramic/glass (edge-weighted); ~0.8 reads as metal (reflective face-on).",
            // Light
            ["Light azimuth"] = "Horizontal angle of the key light, 0-360 degrees. Spins the light around the fractal.",
            ["Light elevation"] = "Vertical angle of the key light. +90 overhead, 0 on the horizon, -90 from below.",
            ["Light intensity"] = "Diffuse light strength. 1.0 is the standard look; 0 is flat ambient; >1 brightens.",
            // Camera (injected in FractalView)
            ["Cam X"] = "Camera world X position. Usually set by flying, but keyframeable for animation.",
            ["Cam Y"] = "Camera world Y position.",
            ["Cam Z"] = "Camera world Z position.",
            ["Cam Yaw"] = "Camera heading (left/right look angle), radians.",
            ["Cam Pitch"] = "Camera tilt (up/down look angle), radians, clamped near +/-90 degrees.",
            ["Cam Roll"] = "Camera roll (bank angle), radians.",
        };
}
```

Create a temporary stub so the partial compiles before Task 7 fills it. Create `src/Parsec.App/GuideContent.Registry.cs`:

```csharp
using System.Collections.Generic;

namespace Parsec.App;

public static partial class FractalGuide
{
    // Filled in Task 7. Temporary minimal stub so the builder compiles and tests run.
    public static GuideContent Resolve(FractalType type, int deepFormula) => new()
    {
        Title = type.ToString(),
        WhatItIs = new[] { "" },
        HowComputed = new[] { "" },
        BestResults = new[] { "" },
    };
}
```

- [ ] **Step 4: Run builder tests to verify they pass**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "GuideBuilderTests"`
Expected: PASS (4 tests). The `Gloss` shared-note assertion passes because "Gloss" is in `SharedSettingNotes`.

- [ ] **Step 5: Commit**

```bash
git add src/Parsec.App/FractalGuide.cs src/Parsec.App/GuideContent.Registry.cs src/Parsec.App.Tests/GuideBuilderTests.cs
git commit -m "feat(guide): pure document builder with shared setting notes"
```

---

## Task 7: Author the content registry

**Files:**
- Modify: `src/Parsec.App/GuideContent.Registry.cs` (replace the stub)
- Test: `src/Parsec.App.Tests/GuideContentCoverageTests.cs`

This task transcribes the committed research into `GuideContent`. The coverage test
is the safety net: it fails if any entry is missing prose or any fractal-specific
setting lacks a note.

- [ ] **Step 1: Write the coverage tests**

Create `src/Parsec.App.Tests/GuideContentCoverageTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class GuideContentCoverageTests
{
    // Every selectable non-deep FractalType mapped to a fresh state's schema, so we
    // can assert each fractal-specific setting label has a note. (Shared groups are
    // covered by SharedSettingNotes, tested separately.)
    private static readonly (FractalType Type, Func<ParamSchema> Schema)[] Fractals =
    {
        (FractalType.AmazingBox, () => new AmazingBoxState().BuildSchema()),
        (FractalType.Mandelbox, () => new MandelboxState().BuildSchema()),
        (FractalType.Kifs, () => new KifsState().BuildSchema()),
        (FractalType.Kleinian, () => new KleinianState().BuildSchema()),
        (FractalType.Attractor, () => new AttractorState().BuildSchema()),
        (FractalType.Mandelbulb, () => new MandelbulbState().BuildSchema()),
        (FractalType.QuaternionJulia, () => new QuaternionJuliaState().BuildSchema()),
        (FractalType.RotBox, () => new RotBoxState().BuildSchema()),
        (FractalType.Hybrid, () => new HybridState().BuildSchema()),
        (FractalType.QJBox, () => new QJBoxState().BuildSchema()),
        (FractalType.Menger, () => new MengerState().BuildSchema()),
        (FractalType.Bicomplex, () => new BicomplexState().BuildSchema()),
        (FractalType.Apollonian, () => new ApollonianState().BuildSchema()),
        (FractalType.Phoenix, () => new PhoenixState().BuildSchema()),
        (FractalType.Biomorph, () => new BiomorphState().BuildSchema()),
        (FractalType.Mosely, () => new MoselyState().BuildSchema()),
        (FractalType.PseudoKleinian4D, () => new PseudoKleinian4DState().BuildSchema()),
        (FractalType.RiemannSphere, () => new RiemannSphereState().BuildSchema()),
        (FractalType.Mandalay, () => new MandalayState().BuildSchema()),
        (FractalType.Anisotropic, () => new AnisotropicState().BuildSchema()),
        (FractalType.OrbitHybrid, () => new OrbitHybridState().BuildSchema()),
        (FractalType.BurningShip, () => new BurningShipState().BuildSchema()),
    };

    public static IEnumerable<object[]> FractalCases() => Fractals.Select(f => new object[] { f.Type });

    [Theory]
    [MemberData(nameof(FractalCases))]
    public void Every_fractal_has_nonempty_prose(FractalType type)
    {
        var c = FractalGuide.Resolve(type, 0);
        Assert.False(string.IsNullOrWhiteSpace(c.Title));
        Assert.NotEmpty(c.WhatItIs);
        Assert.All(c.WhatItIs, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.NotEmpty(c.HowComputed);
        Assert.All(c.HowComputed, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.NotEmpty(c.BestResults);
        Assert.All(c.BestResults, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    [Theory]
    [MemberData(nameof(FractalCases))]
    public void Every_fractal_specific_setting_has_a_note(FractalType type)
    {
        var schema = Fractals.Single(f => f.Type == type).Schema();
        var content = FractalGuide.Resolve(type, 0);
        foreach (var d in schema.Parameters)
            Assert.True(content.SettingNotes.ContainsKey(d.Label),
                $"{type}: missing note for setting '{d.Label}'");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_deep_formula_has_distinct_nonempty_prose(int formula)
    {
        var c = FractalGuide.Resolve(FractalType.DeepZoom, formula);
        Assert.False(string.IsNullOrWhiteSpace(c.Title));
        Assert.NotEmpty(c.WhatItIs);
        Assert.NotEmpty(c.BestResults);
    }

    [Fact]
    public void Deep_formulas_have_different_titles()
    {
        var titles = Enumerable.Range(0, 4)
            .Select(f => FractalGuide.Resolve(FractalType.DeepZoom, f).Title).ToList();
        Assert.Equal(4, titles.Distinct().Count());
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "GuideContentCoverageTests"`
Expected: FAIL (the stub returns empty prose and no notes).

- [ ] **Step 3: Replace the stub with the real registry**

Replace the entire contents of `src/Parsec.App/GuideContent.Registry.cs`. Structure:

```csharp
using System.Collections.Generic;

namespace Parsec.App;

public static partial class FractalGuide
{
    public static GuideContent Resolve(FractalType type, int deepFormula)
    {
        if (type == FractalType.DeepZoom)
            return DeepFormulas[System.Math.Clamp(deepFormula, 0, DeepFormulas.Count - 1)];
        return Registry[type];
    }

    private static readonly IReadOnlyList<GuideContent> DeepFormulas = new[]
    {
        Mandelbrot2D, Prospector2D, Julia2D, BurningShip2D,
    };

    private static readonly IReadOnlyDictionary<FractalType, GuideContent> Registry =
        new Dictionary<FractalType, GuideContent>
        {
            [FractalType.Mandelbulb] = Mandelbulb,
            // ... one entry per non-deep FractalType (see table below) ...
        };

    // ----- one static GuideContent property/field per entry, below -----

    private static GuideContent Mandelbulb => new()
    {
        Title = "Mandelbulb",
        WhatItIs = new[]
        {
            "The Mandelbulb is a 3D analogue of the Mandelbrot set: it raises a point in spherical coordinates to a power and adds the start point, iterating until the orbit escapes. The power-8 version is the canonical 'bulb' with its cauliflower lobes and filigree.",
        },
        HowComputed = new[]
        {
            "Parsec raymarches a distance estimate. Each step converts the running vector to spherical coordinates (r, theta, phi), raises r to the power and multiplies the angles by the power, converts back, and adds the start point. A running derivative gives the distance estimate that lets the ray step safely.",
        },
        Math = new[]
        {
            "Iteration: v -> v^n + c in spherical form, with theta,phi scaled by n. Escape when |v| exceeds the bailout radius. Power n = 8 is the standard; other integer and fractional powers give different lobe counts.",
        },
        BestResults = new[]
        {
            "Power 8 is the classic look; try 2-16 for different symmetries. Raise Iterations (default 10) toward 30-60 for crisp filigree in hero stills; lower it while flying for speed. A larger Bailout (default 8) sharpens the distance estimate and reduces mushy edges.",
        },
        SettingNotes = new Dictionary<string, string>
        {
            ["Power"] = "The exponent n in v -> v^n + c. 8 is canonical; lower powers give fewer lobes, higher powers more.",
            ["Iterations"] = "Escape-test iteration cap. Higher reveals finer filigree at the cost of framerate.",
            ["Bailout"] = "Escape radius. Larger values give a more accurate distance estimate and crisper edges.",
            ["DE fudge"] = "Safety factor on the marching step. Lower if you see overstepping artifacts; raise toward 1.0 for speed.",
        },
    };

    // ... remaining entries ...
}
```

Author every entry by transcribing from the research files. For each fractal, pull:
- `WhatItIs` from the file's "What it is" section (condensed to 1-2 short paragraphs).
- `HowComputed` from "How Parsec computes it" (condensed; drop the file:line citations from user-facing text).
- `Math` from "Canonical formula / math background" (1 short paragraph; may be omitted for the niche/custom ones).
- `SettingNotes`: one sentence per row of the "Settings audit" table — and there MUST be a key for every fractal-specific `ParamDescriptor.Label` in that state's `BuildSchema()` (the coverage test enforces this). Use the live app or the state file to list the exact labels.
- `BestResults` from "For best results", folding in the audit's guide-note items where relevant (R3 Mandalay: stay at negative Scale; R6: raise iterations for stills; R7: keep DE fudge modest on Kleinian/PK4D; R8: keep Apollonian outer radius >= 0.95).

Honesty notes to include verbatim in the relevant entries:
- Prospector (`Prospector2D`): state it is a custom Parsec formula with no standard published definition; describe only what the shader does.
- 3D Burning Ship (`BurningShip`) and 2D Burning Ship (`BurningShip2D`): a community triplex/abs-fold extension; note Power lives low (2-3) for the 3D one.
- Bicomplex (`Bicomplex`): the mul/add params are an artist variant; muls = 1 recovers the true bicomplex set.

Entry-to-source mapping (one GuideContent each; check off as authored):

- [ ] Mandelbox, AmazingBox, RotBox, Menger -> `fractals-1-box.md`
- [ ] Mandelbulb, QuaternionJulia, Bicomplex, QJBox -> `fractals-2-bulb.md`
- [ ] Kifs, Kleinian, PseudoKleinian4D, OrbitHybrid -> `fractals-3-kifs.md`
- [ ] Apollonian, Phoenix, Biomorph, Mosely -> `fractals-4-exoticA.md`
- [ ] RiemannSphere, Mandalay, Anisotropic, Hybrid, Attractor -> `fractals-5-exoticB.md`
- [ ] BurningShip (3D) + DeepZoom formulas Mandelbrot2D/Prospector2D/Julia2D/BurningShip2D -> `fractals-6-deepzoom.md`

Note for the Attractor entry: its settings are generation parameters (Damping b, Dt, etc.); write notes accordingly and mention animation is disabled for it.

- [ ] **Step 4: Run coverage tests to verify they pass**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj --filter "GuideContentCoverageTests"`
Expected: PASS. If a "missing note for setting 'X'" failure appears, add that label to the named fractal's `SettingNotes` (the schema label is the exact key).

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj`
Expected: PASS (smoke + schema + builder + coverage all green).

- [ ] **Step 6: Commit**

```bash
git add src/Parsec.App/GuideContent.Registry.cs src/Parsec.App.Tests/GuideContentCoverageTests.cs
git commit -m "feat(guide): author per-fractal guide content registry"
```

---

## Task 8: Guide window renderer

**Files:**
- Create: `src/Parsec.App/GuideWindow.cs`

This is an Avalonia view, verified by build + manual run (no unit test: instantiating
a Window needs an app lifetime).

- [ ] **Step 1: Write the window**

Create `src/Parsec.App/GuideWindow.cs`:

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Parsec.App;

/// <summary>
/// A non-modal, read-only companion window that renders a <see cref="GuideDocument"/>
/// for the active fractal. Built in code to match the ParameterPanel pattern (a
/// UserControl/Window wrapping a ScrollViewer scrolls reliably). Content is
/// TextBlock/SelectableTextBlock only — no TextBox — so it can be read and copied
/// but never edited. Escape closes it.
/// </summary>
public sealed class GuideWindow : Window
{
    private static readonly IBrush Bg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26));
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0xb0));
    private static readonly IBrush Body = new SolidColorBrush(Color.FromRgb(0xd0, 0xd0, 0xd8));
    private static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(0xa0, 0xc0, 0xe0));

    private readonly StackPanel _root;

    public GuideWindow()
    {
        Width = 480;
        Height = 720;
        Background = Bg;
        Title = "Guide";

        _root = new StackPanel { Margin = new Thickness(16), Spacing = 6 };
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _root,
        };

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    /// <summary>Rebuild the window body for a new document (used on open and on
    /// fractal/formula switch while open).</summary>
    public void Populate(GuideDocument doc)
    {
        Title = $"Guide — {doc.Title}";
        _root.Children.Clear();

        _root.Children.Add(new SelectableTextBlock
        {
            Text = doc.Title,
            Foreground = Body,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case GuideBlock.Heading h:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = h.Text.ToUpperInvariant(),
                        Foreground = Accent,
                        FontSize = 13,
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 14, 0, 2),
                    });
                    break;

                case GuideBlock.Paragraph p:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = p.Text,
                        Foreground = Body,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4),
                    });
                    break;

                case GuideBlock.SettingGroupHeading g:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = g.Group.ToUpperInvariant(),
                        Foreground = Dim,
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 10, 0, 2),
                    });
                    break;

                case GuideBlock.SettingDefinition d:
                    _root.Children.Add(BuildDefinition(d));
                    break;
            }
        }
    }

    private Control BuildDefinition(GuideBlock.SettingDefinition d)
    {
        var box = new StackPanel { Spacing = 1, Margin = new Thickness(0, 2, 0, 4) };

        var header = new WrapPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new SelectableTextBlock
        {
            Text = d.Name + "  ",
            Foreground = Body,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
        });
        header.Children.Add(new SelectableTextBlock
        {
            Text = d.Range,
            Foreground = Dim,
            FontSize = 11,
            FontFamily = new FontFamily("monospace"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        box.Children.Add(header);

        if (!string.IsNullOrEmpty(d.Note))
            box.Children.Add(new SelectableTextBlock
            {
                Text = d.Note,
                Foreground = Body,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });

        return box;
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Parsec.App/Parsec.App.csproj -c Debug`
Expected: Build succeeded. (If `SelectableTextBlock` is not found, confirm the Avalonia version exposes it under `Avalonia.Controls`; it does in Avalonia 11.)

- [ ] **Step 3: Commit**

```bash
git add src/Parsec.App/GuideWindow.cs
git commit -m "feat(guide): read-only guide window renderer"
```

---

## Task 9: Wire the Guide button into MainWindow

**Files:**
- Modify: `src/Parsec.App/MainWindow.axaml`
- Modify: `src/Parsec.App/MainWindow.axaml.cs`

- [ ] **Step 1: Add the button to the XAML**

In `src/Parsec.App/MainWindow.axaml`, find the fractal selector close tag and the FORMULA label:

```xml
        </ComboBox>
        <TextBlock Name="FormulaLabel" Text="FORMULA" Foreground="#9a9ab0" FontSize="11"
                   FontWeight="SemiBold" Margin="0,8,0,0" IsVisible="False" />
```

Replace with (inserting the Guide button between them):

```xml
        </ComboBox>
        <Button Name="GuideButton" HorizontalAlignment="Stretch"
                HorizontalContentAlignment="Center" Margin="0,8,0,0"
                Content="Guide" />
        <TextBlock Name="FormulaLabel" Text="FORMULA" Foreground="#9a9ab0" FontSize="11"
                   FontWeight="SemiBold" Margin="0,8,0,0" IsVisible="False" />
```

- [ ] **Step 2: Add the window field and handler in code-behind**

In `src/Parsec.App/MainWindow.axaml.cs`, add a field next to the other private fields (after `private Button? _generateButton;`):

```csharp
    private GuideWindow? _guideWindow;
```

In the constructor, after the `_generateButton` wiring block (the `if (_generateButton != null) _generateButton.Click += OnGenerateClick;` block), add:

```csharp
        var guideButton = this.FindControl<Button>("GuideButton");
        if (guideButton != null)
            guideButton.Click += OnGuideClick;
```

Add these methods to the class (near `OnFractalChanged`):

```csharp
    // ----------------------------------------------------------- guide window
    private void OnGuideClick(object? sender, RoutedEventArgs e) => OpenOrRefreshGuide();

    private void OpenOrRefreshGuide()
    {
        if (_view == null || _activeSchema == null) return;
        var doc = FractalGuide.Build(_view.ActiveType, _view.DeepFormula, _activeSchema);

        if (_guideWindow == null)
        {
            _guideWindow = new GuideWindow();
            _guideWindow.Closed += (_, _) => _guideWindow = null;
            _guideWindow.Populate(doc);
            _guideWindow.Show();            // non-modal companion window
        }
        else
        {
            _guideWindow.Populate(doc);
            _guideWindow.Activate();        // bring forward
        }
    }

    private void RefreshGuideIfOpen()
    {
        if (_guideWindow != null && _view != null && _activeSchema != null)
            _guideWindow.Populate(
                FractalGuide.Build(_view.ActiveType, _view.DeepFormula, _activeSchema));
    }
```

- [ ] **Step 3: Refresh the guide when the fractal changes**

In `OnFractalChanged`, the last line is `RebuildForActiveFractal();`. Immediately after it add:

```csharp
        RefreshGuideIfOpen();   // keep an open guide tracking the viewed fractal
```

- [ ] **Step 4: Refresh the guide when the deep formula changes**

In the constructor, the FORMULA selector handler currently is:

```csharp
        if (formulaSelector != null)
            formulaSelector.SelectionChanged += (_, _) =>
            {
                // Dropdown index == formula int (Mandelbrot 0, Prospector 1,
                // Julia 2, Burning Ship 3).
                if (_view != null && formulaSelector.SelectedIndex >= 0)
                    _view.SetDeepFormula(formulaSelector.SelectedIndex);
            };
```

Replace the inner body with one that also refreshes the guide:

```csharp
        if (formulaSelector != null)
            formulaSelector.SelectionChanged += (_, _) =>
            {
                // Dropdown index == formula int (Mandelbrot 0, Prospector 1,
                // Julia 2, Burning Ship 3).
                if (_view != null && formulaSelector.SelectedIndex >= 0)
                {
                    _view.SetDeepFormula(formulaSelector.SelectedIndex);
                    RefreshGuideIfOpen();
                }
            };
```

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build src/Parsec.App/Parsec.App.csproj -c Debug`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/Parsec.App/MainWindow.axaml src/Parsec.App/MainWindow.axaml.cs
git commit -m "feat(guide): add Guide button that opens the per-fractal guide window"
```

---

## Task 10: Full verification

**Files:** none (verification + final commit if needed)

- [ ] **Step 1: Build the whole solution**

Run: `dotnet build Parsec.sln -c Debug`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test src/Parsec.App.Tests/Parsec.App.Tests.csproj`
Expected: All tests pass (smoke, schema audit, builder, content coverage).

- [ ] **Step 3: Manual run checklist**

Run: `dotnet run --project src/Parsec.App/Parsec.App.csproj -c Release`
Verify by hand:
- Click "Guide": a separate window opens with the current fractal's title, what-it-is, how-computed, settings (each with a range), and best-results.
- The main window is still interactive while the guide is open (non-modal): move a slider, fly the camera.
- Switch the fractal in the dropdown: the open guide updates to the new fractal.
- Select Deep Zoom 2D, change the FORMULA dropdown: the guide updates per formula (Mandelbrot/Prospector/Julia/Burning Ship).
- Try to edit guide text: it selects/copies but cannot be edited.
- Press Escape (and separately the window close button): the guide closes. Reopen works.
- Spot-check a schema fix in the panel: Mandelbox Scale slider now reaches 3.0; Reset to Defaults on Mandelbulb shows Iterations 10 / Bailout 8.

- [ ] **Step 4: Final commit (if any manual fixes were needed)**

```bash
git add -A
git commit -m "chore(guide): manual-run fixes and polish"
```

(If no fixes were needed, skip this commit.)

---

## Self-Review notes (author)

- Spec coverage: Part 1 (data model -> Task 5; builder -> Task 6; content -> Task 7;
  window -> Task 8; wiring + auto-refresh + non-modal -> Task 9; tests -> Tasks
  1,2,3,4,6,7). Part 2 schema cleanup: B1/R1/R3/R4/R7/R8 -> Task 3; B2/B3 -> Task 2;
  R2/R5/R6 -> Task 4. Read-only guarantee -> Task 8 (no TextBox). Deep-zoom-by-formula
  -> Task 7 Resolve + Task 9 refresh.
- Type consistency: `GuideContent`, `GuideDocument`, `GuideBlock.{Heading,Paragraph,
  SettingGroupHeading,SettingDefinition}`, `FractalGuide.{Build,BuildDocument,Resolve,
  FormatRange,NoteFor,SharedSettingNotes}` are used identically across Tasks 5-9.
- Partial class: `FractalGuide` is declared `partial` in both FractalGuide.cs and
  GuideContent.Registry.cs; `Resolve` lives only in the registry file (builder file
  does not redefine it).
```

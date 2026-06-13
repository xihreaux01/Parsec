# Per-Fractal Guide Window + Schema Cleanup — Design Spec

Date: 2026-06-13
Status: Approved for planning

## Summary

Add a "Guide" button to Parsec Hyperdrive that opens a non-modal, read-only
companion window describing the fractal currently being viewed: what it is, how
Parsec computes it, a brief mathematical note, a definition of every setting (with
its live min/max range), and tuning tips for best results. The guide's setting
list is auto-derived from the live parameter schema so it never drifts from the
real sliders; the prose is hand-written, sourced from the research in
`docs/superpowers/research/`.

Bundled with the feature is a schema cleanup pass: three confirmed correctness bugs
and eight range/default coverage fixes surfaced by the audit. The guide shows the
corrected ranges automatically because it reads the live schema, so the schema
edits land first.

## Goals

- One click opens a per-fractal guide for the active fractal (and, for Deep Zoom
  2D, the active formula).
- The guide is scrollable and read-only (selectable for copy, never editable) and
  closes when the user is done.
- Setting definitions stay in sync with the actual sliders automatically.
- Content is accurate: calculation claims anchored to code, parameter advice from
  cited research.
- Defaults are good out-of-the-box viewing values and slider ranges cover the
  proper viewing variety (the audit fixes).

## Non-goals (YAGNI)

No markdown library, no external content files, no print/export, no in-guide
search, no clickable links from guide into the panel, no per-fractal preset
thumbnails. Content lives in code; formatting is native Avalonia controls.

---

## Part 1 — Guide feature

### Architecture overview

```
MainWindow (Guide button)
   -> builds GuideDocument via FractalGuide.Build(activeType, deepFormula, activeSchema)
   -> opens / refreshes a single GuideWindow (non-modal)

FractalGuide (static)
   - registry: FractalType -> GuideContent (prose + per-setting notes)
   - deep-zoom resolved by formula index
   - shared note table for Palette / Reflections / Light / Camera
   - Build(...) : pure function -> GuideDocument  (unit-testable, no GL/window)

GuideWindow (Avalonia Window)
   - renders a GuideDocument into formatted, read-only SelectableTextBlocks
```

### Data model

`GuideContent` (one per fractal, plain data):
- `string Title`
- `string[] WhatItIs` (paragraphs)
- `string[] HowComputed` (paragraphs)
- `string[] Math` (paragraphs; may be empty)
- `string[] BestResults` (paragraphs / bullet lines)
- `IReadOnlyDictionary<string,string> SettingNotes` keyed by exact
  `ParamDescriptor.Label`

`GuideDocument` (render-ready, produced by `Build`):
- `string Title`
- ordered `IReadOnlyList<GuideBlock>` where `GuideBlock` is one of:
  - `Heading(string text)`
  - `Paragraph(string text)`
  - `SettingGroupHeading(string group)`
  - `SettingDefinition(string name, string range, string note)`

`Build(FractalType type, int deepFormula, ParamSchema schema)`:
1. Resolve `GuideContent` (deep zoom -> by `deepFormula`; otherwise by `type`).
2. Emit Title, then What it is, How Parsec computes it, Math, then a SETTINGS
   section: iterate `schema.Groups` in order; for each group emit a
   `SettingGroupHeading`, then for each `ParamDescriptor` in the group emit a
   `SettingDefinition` with:
   - `name` = descriptor `Label`
   - `range` = formatted from `Min`/`Max`/`Step`/`Decimals`
     (e.g. `range -3.00 .. 3.00`, with `step 1` appended when `Step > 0`)
   - `note` = lookup order: `content.SettingNotes[label]` ->
     `FractalGuide.SharedSettingNotes[label]` -> empty string (graceful
     fallback: the setting still appears with its name and range).
3. Emit a FOR BEST RESULTS section from `content.BestResults`.

`Build` is pure (no Avalonia, no GL): it takes the schema and returns data. This
is the unit-test seam.

### Content sourcing

Prose is hand-written from the six research files and the consolidated audit in
`docs/superpowers/research/`. Per fractal:
- What it is and the math note: from each family file's "What it is" and
  "Canonical formula / math background" sections.
- How Parsec computes it: from the "How Parsec computes it" sections (already
  anchored to shader/DE code).
- Setting notes: one concise sentence per fractal-specific parameter, from the
  per-parameter audit tables.
- Best results: from each file's "For best results" plus the audit's guide-note
  items (R3 Mandalay negative scale, R6 raise iterations for stills, R7 keep DE
  fudge modest, R8 Apollonian safe radius band).

Honesty notes baked into content: Prospector is a custom/undocumented formula
(described by behavior only); 3D Burning Ship is a community triplex extension,
not canonical; Bicomplex mul/add are an artist variant.

Shared note table (`SharedSettingNotes`) covers the labels in Palette
(`Frequency`, `Trap scale`, `Phase R/G/B`, `Base R/G/B`, `Amp R/G/B`, `Mix
origin/axis/plane`, `Shell glaze`), Reflections (`Reflection bounces (0=off)`,
`Gloss`, `Fresnel F0 ...`), Light (`Light azimuth/elevation/intensity`), and
Camera (`Cam X/Y/Z`, `Cam Yaw/Pitch/Roll`). Written once, reused everywhere.

Coverage requirement: the registry holds 26 `GuideContent` entries — the 22
non-deep-zoom `FractalType` values plus the four deep-zoom formulas (Deep Zoom is
resolved by formula, not by a single entry). Each has non-empty Title, WhatItIs,
HowComputed, and BestResults. Any setting label without a note degrades gracefully
(name + range only), but every fractal-specific label should have a note at ship.

### GuideWindow

- An Avalonia `Window`, ~480x720, dark theme matching the panel (`#222226`
  background), title `Guide — {GuideDocument.Title}`.
- Content: a `ScrollViewer` (vertical auto, horizontal disabled) wrapping a
  `StackPanel`, mirroring the `ParameterPanel` UserControl pattern that is known to
  scroll correctly.
- Rendering of blocks:
  - `Heading`: bold, uppercase-ish, dim accent color, larger size.
  - `SettingGroupHeading`: same style as the panel's group labels (`#9a9ab0`,
    semibold, 11px).
  - `SettingDefinition`: setting name in bold with a dim monospace range suffix on
    the same line, note wrapped below.
  - `Paragraph`: wrapped body text, comfortable line height.
- Read-only guarantee: only `TextBlock` / `SelectableTextBlock` are used. There is
  no `TextBox` anywhere in the window, so content cannot be edited; selection is
  allowed so users can copy.
- Closing: window chrome close button, plus Escape key closes the window.
- `Populate(GuideDocument doc)` rebuilds the content in place for refresh.

### Wiring in MainWindow

- Add a `Guide` button in `MainWindow.axaml`, placed directly under the
  `FractalSelector` ComboBox (before the FORMULA label).
- `MainWindow` holds a single `GuideWindow? _guideWindow`.
- On click: build a `GuideDocument` from `_view.ActiveType`, `_view.DeepFormula`,
  and the live `_activeSchema`. If `_guideWindow` is null or closed, create it,
  `Populate`, subscribe to its `Closed` to null the field, and `Show()`
  (non-modal). If it is already open, `Populate` with the fresh document and
  `Activate()` to bring it forward.
- In `OnFractalChanged` and the FORMULA `SelectionChanged` handler: if
  `_guideWindow` is open, rebuild and `Populate` it so it tracks the fractal the
  user is actually viewing.
- The build uses `_activeSchema`, which already excludes nothing the user sees:
  for 3D fractals it is fractal params + Palette + Reflections + Light + Camera;
  for Deep Zoom it is kappa + Palette. The guide reflects exactly those.

### Testing

Add a minimal xUnit project `src/Parsec.App.Tests` (net8.0, references
`Parsec.App`). Tests target the pure builder and registry only (no GL, no
windowing):
- Every non-deep-zoom `FractalType` resolves to a `GuideContent` with non-empty
  Title, WhatItIs, HowComputed, BestResults.
- Each of the four deep-zoom formulas resolves to distinct non-empty content.
- For a representative fractal's schema, `Build` emits a SettingDefinition for
  every `ParamDescriptor`, in schema order, grouped correctly.
- Every fractal-specific setting label has a note (no fallback-empty notes for
  fractal params; shared params may rely on the shared table).
- Range formatting matches `Min`/`Max`/`Step`/`Decimals` (spot cases).

---

## Part 2 — Schema cleanup

All values verified against source and confirmed in-range. Format: file — change.

### Confirmed bugs

1. Orbit Hybrid bound radius unreachable default.
   `OrbitHybridState.cs:112` — `Bound radius` slider `Max = 10.0` -> `Max = 20.0`
   so the default/Reset value `16.0` (`:41,129`) is reachable.

2. Quaternion Julia Reset incomplete.
   `QuaternionJuliaState.cs` `Reset()` (`:95-105`) — add
   `StereoK = 1.0f; StereoR = 0.8f;` to match the field defaults (`:27-28`).

3. Mosely Reset incomplete.
   `MoselyState.cs` `Reset()` (`:70-72`) — add `WedgeDeg = 360f; Fudge = 0.9f;`
   to match the field defaults (`:24,26`).

### Range / default coverage

R1. `MandelboxState.cs:53` — `Scale` slider `Max = 2.0` -> `Max = 3.0` (admit the
   canonical scale-3 cityscape and 2 < s <= 3 forms).

R2. `AttractorState.cs` — `Damping b` default `0.208186f` -> `0.19f` at the field
   (`:24`) and in `Reset()` (`:127`); slider `Max = 0.35` -> `Max = 0.30` (`:77`)
   to keep it in the chaotic regime (b < ~0.208) and out of the limit-cycle band.

R3. `MandalayState.cs:62` — `Scale` slider range `Min = -3.0, Max = 3.0` ->
   `Min = -3.0, Max = -0.5` (the bounded set only exists at negative scale; default
   `-2.0` stays valid).

R4. `KifsState.cs:58-74` — all six rotation sliders (Post-rot X/Y/Z, Pre-rot
   X/Y/Z) `Min = -45, Max = 45` -> `Min = -90, Max = 90` for the full curl range.

R5. `MengerState.cs:19` — `OffsetZ` field default `0.0f` -> `1.0f` (and in
   `Reset()`), giving the canonical sponge offset (1,1,1).

R6. Render-cost defaults (trades a little interactive framerate for crisper
   out-of-the-box detail; flagged for review):
   - `MandelbulbState.cs` — `Bailout` `4.0f` -> `8.0f` (field `:14` + Reset `:48`);
     `Iterations` `8` -> `10` (field `:12` + Reset `:46`).
   - `QuaternionJuliaState.cs` — `Iterations` `10` -> `12` (field + Reset).
   - `QJBoxState.cs` — `Iterations` `8` -> `10` (field + Reset).
   - `BiomorphState.cs` — `Iterations` `16` -> `24` (field + Reset).

R7. `KleinianState.cs:65` and `PseudoKleinian4DState.cs:121` — `DE fudge` slider
   `Max = 2.0` -> `Max = 1.0` (defaults 0.7 / 0.6 stay valid; keeps users out of
   broken-DE territory).

R8. `ApollonianState.cs:54` — `Outer radius x` slider `Min = 0.85` -> `Min = 0.95`
   (below ~0.92 the bounding sphere clips the gasket's outer lobes; default 1.0
   stays valid).

### Consistency requirement

Where a default field value changes (R2, R5, R6), the matching assignment in that
state's `Reset()` must change too, so `Reset to defaults` restores the new value.
This is the same class of issue as bugs 2 and 3.

---

## File change list

New:
- `src/Parsec.App/FractalGuide.cs` — content registry + `GuideContent` /
  `GuideDocument` / `GuideBlock` types + pure `Build`.
- `src/Parsec.App/GuideWindow.cs` — read-only window renderer.
- `src/Parsec.App.Tests/Parsec.App.Tests.csproj` + test file(s).

Modified (feature):
- `src/Parsec.App/MainWindow.axaml` — add Guide button.
- `src/Parsec.App/MainWindow.axaml.cs` — button handler, single-window tracking,
  refresh on fractal/formula change.
- `Parsec.sln` — add the test project.

Modified (schema cleanup):
- `OrbitHybridState.cs`, `QuaternionJuliaState.cs`, `MoselyState.cs`,
  `MandelboxState.cs`, `AttractorState.cs`, `MandalayState.cs`, `KifsState.cs`,
  `MengerState.cs`, `MandelbulbState.cs`, `QJBoxState.cs`, `BiomorphState.cs`,
  `KleinianState.cs`, `PseudoKleinian4DState.cs`, `ApollonianState.cs`.

## Risks and tradeoffs

- R6 raises interactive render cost on four fractals. The Bailout bump is nearly
  free; iteration bumps are modest but real. Reviewer may veto individual bumps.
- Writing accurate prose for 22 + 4 entries is the bulk of the effort; the research
  files are the source so this is transcription/condensation, not new research.
- Auto-refresh on fractal switch must rebuild from the same `_activeSchema` the
  panel uses, to avoid showing stale ranges. Build is cheap, so always rebuild.

## Verification

- Build the app (`dotnet run --project src/Parsec.App/Parsec.App.csproj -c
  Release`), open the guide on several fractals incl. Deep Zoom with each formula,
  confirm scroll, read-only, Escape-close, and auto-refresh on switch.
- `dotnet test` green for the builder/registry tests.
- Spot-check three edited schemas in the running app: Reset restores new defaults;
  the new slider ranges are present; the guide shows the corrected ranges.

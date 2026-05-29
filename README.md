# Parsec

A modular compositional system for defining and rendering iterated function systems (IFS).

## Project layout

```
Parsec/
├── Parsec.sln
└── src/
    ├── Parsec.Core/        Pure math: AffineMap2D, IFS2D, IFSNode2D, Polygon2D
    │                         (Transforms/, Ifs/, Geometry/ subfolders)
    ├── Parsec.Rendering/   IIFSRenderer2D + SkiaSharp-backed implementations
    └── Parsec.Cli/         Console runner with command-line example dispatch
```

`Parsec.Core` has no dependencies. `Parsec.Rendering` depends on Core and SkiaSharp.
`Parsec.Cli` depends on both.

## Building and running

```bash
dotnet build
dotnet run --project src/Parsec.Cli -- <example>
```

PNGs are written to `<exe-dir>/outputs/<example>.png` (typically
`src/Parsec.Cli/bin/Debug/net9.0/outputs/`).

### Commands

| Command                                  | Effect                            |
| ---------------------------------------- | --------------------------------- |
| `parsec list`                            | List available examples           |
| `parsec <name>`                          | Render one example                |
| `parsec all`                             | Render every example              |
| `parsec help`                            | Usage                             |

### Examples available

- `diamond` — Reference image: rotated-square ∪ two corner squares, depth 7, outlines.
- `diamond-construction` — Same IFS at depth 2 with fill + outlines, showing construction.
- `carpet` — Sierpiński carpet, depth 5.
- `triangle` — Sierpiński triangle, depth 7.

## Design notes

### Layered architecture

- **`IFS2D`** is a pure mathematical object — a set of `IFSNode2D`s, each carrying
  an affine transform plus optional metadata (weight, post-transform, color, label).
  It knows nothing about how it will be rendered.
- **`IIFSRenderer2D`** is the swappable rendering layer. Today the only
  implementation is `DeterministicSubdivisionRenderer`; planned future siblings
  include density-accumulation (fractal-flame-style) and chaos-game renderers.
- **Examples** in the CLI compose IFSes with renderers; the same IFS can in
  principle be fed to any renderer.

### Composition operator

`IFS2D.Union(a, b)` (also `a | b`) concatenates node lists. Weights are
preserved as-is; any normalization happens at the renderer. This lets
sub-IFSes be named, reused, and recombined without lossy intermediate
normalization.

### Transform composition order

`a.Then(b).Apply(p) == b.Apply(a.Apply(p))`. I.e., `Then` reads left-to-right
in application order. `AffineMap2D.Compose(a, b, c, ...)` is the variadic form.

### Contractive vs non-contractive systems

`IFS2D.IsContractive` reports whether every node's primary transform has
spectral norm < 1. The deterministic subdivision renderer doesn't require this,
but interesting things happen when it's false — see the `diamond` example,
where the diamond map is a pure rotation.

## Planned next steps

1. **Density accumulation renderer** — proper handling of overlapping leaves;
   the right answer for high-depth visualizations of non-trivial IFSes.
2. **Chaos game renderer** — stochastic point-spray; lets `Weight` and `Color`
   on nodes become first-class.
3. **GPU compute backend** — the architecture is positioned for this; the
   `AffineMap2D` field layout already matches a GPU `float3x2`.
4. **3D** — `IFS3D`, `AffineMap3D`, distance-estimator and point-cloud renderers.

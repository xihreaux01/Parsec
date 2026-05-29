using Parsec.Core.Ifs;
using SkiaSharp;

namespace Parsec.Rendering;

/// <summary>
/// A 2D IFS renderer. Pairs an <see cref="IFS2D"/> with renderer-specific
/// inputs (view bounds, image size, style) and produces an SKBitmap.
/// </summary>
/// <remarks>
/// <para>
/// Each implementation defines its own configuration record; the interface
/// itself just exposes <see cref="Render"/>. This is intentional — different
/// renderers need wildly different inputs (a chaos game wants a point count;
/// deterministic subdivision wants a base shape and depth; density
/// accumulation wants both plus a tone mapping curve) and trying to unify
/// them at the interface level produces a useless lowest-common-denominator.
/// </para>
/// </remarks>
public interface IIFSRenderer2D
{
    SKBitmap Render();
}

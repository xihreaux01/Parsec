using Parsec.Core.Transforms;

namespace Parsec.Core.Ifs;

/// <summary>
/// A single node in an iterated function system: a primary affine transform plus
/// optional metadata that some renderers use (weight, post-transform, color, label).
/// </summary>
/// <param name="Transform">
/// The primary affine map applied to points/shapes by this node.
/// </param>
/// <param name="Weight">
/// Relative probability for chaos-game-style stochastic renderers. Ignored by
/// deterministic renderers. Weights are not normalized at construction — the
/// renderer normalizes (or doesn't) according to its semantics.
/// </param>
/// <param name="PostTransform">
/// Optional second affine applied after <see cref="Transform"/>. Fractal-flame
/// renderers use this; deterministic subdivision composes it into the
/// accumulated transform.
/// </param>
/// <param name="Color">
/// Optional color coordinate (RGB, components in [0,1]) for renderers that
/// support per-node color (flame-style palette blending, density visualization).
/// </param>
/// <param name="Label">
/// Optional human-readable identity for the node. Survives <see cref="IFS2D.Union"/>
/// and is useful for introspection and authoring tools.
/// </param>
public sealed record IFSNode2D(
    AffineMap2D Transform,
    float Weight = 1f,
    AffineMap2D? PostTransform = null,
    (float R, float G, float B)? Color = null,
    string? Label = null);

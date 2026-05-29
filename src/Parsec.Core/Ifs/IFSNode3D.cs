using Parsec.Core.Transforms;

namespace Parsec.Core.Ifs;

/// <summary>
/// A single node in a 3D iterated function system: a primary affine transform
/// plus optional metadata. Mirrors <see cref="IFSNode2D"/> structurally.
/// </summary>
/// <param name="Transform">
/// The primary affine map applied to points/shapes by this node.
/// </param>
/// <param name="Weight">
/// Relative probability for chaos-game-style renderers. Ignored by deterministic
/// renderers (distance estimation, subdivision).
/// </param>
/// <param name="PostTransform">
/// Optional second affine applied after <see cref="Transform"/>.
/// </param>
/// <param name="Color">
/// Optional RGB color coordinate (components in [0,1]) for renderers that
/// support per-node color.
/// </param>
/// <param name="Label">
/// Optional human-readable identity for introspection and authoring tools.
/// </param>
public sealed record IFSNode3D(
    AffineMap3D Transform,
    float Weight = 1f,
    AffineMap3D? PostTransform = null,
    (float R, float G, float B)? Color = null,
    string? Label = null);

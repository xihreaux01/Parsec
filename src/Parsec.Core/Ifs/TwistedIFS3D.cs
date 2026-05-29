using System.Numerics;
using Parsec.Core.Transforms;

namespace Parsec.Core.Ifs;

/// <summary>
/// Twisted variants of classic IFS attractors: the standard contractive maps
/// with a rotation inserted around each map's fixed point. Each map remains
/// affine (a fixed rotation is just another linear factor), so these render
/// in the existing pipeline without any nonlinear-map machinery — yet the
/// per-map rotation compounds across recursion levels, producing a helical,
/// twisted attractor.
/// </summary>
/// <remarks>
/// This is the "linear bridge" case toward full nonlinear IFS: a fixed
/// rotation per map is affine, but the visual is already strongly non-trivial
/// because the rotation accumulates differently at every depth. Position-
/// dependent twist (a true nonlinear map) is a later step; this captures most
/// of the visual character with none of the DE complications.
/// </remarks>
public static class TwistedIFS3D
{
    /// <summary>
    /// Sierpiński tetrahedron with a per-vertex twist. Each of the four
    /// scale-toward-vertex maps gets a rotation by <paramref name="twistRadians"/>
    /// around an axis through its fixed vertex.
    /// </summary>
    /// <param name="twistRadians">
    /// Rotation angle applied within each map. 0 reproduces the plain
    /// Sierpiński tetrahedron. Around 0.3–0.8 rad gives a pronounced but
    /// still legible twist; larger values scramble the structure more.
    /// </param>
    /// <param name="axisMode">
    /// How to choose each map's rotation axis. <see cref="TwistAxisMode.CentroidToVertex"/>
    /// rotates around the axis from the tetrahedron centroid to each vertex
    /// (preserves tetrahedral symmetry — a coherent "outward twist").
    /// <see cref="TwistAxisMode.GlobalY"/> rotates every map around the world
    /// Y axis (a more sheared, less symmetric look).
    /// </param>
    public static IFS3D SierpinskiTetrahedron(
        float twistRadians = 0.5f,
        TwistAxisMode axisMode = TwistAxisMode.CentroidToVertex)
    {
        // Same four vertices as CanonicalIFS3D.SierpinskiTetrahedron:
        // alternating corners of the unit cube.
        Vector3[] verts =
        {
            new(0, 0, 0),
            new(1, 1, 0),
            new(1, 0, 1),
            new(0, 1, 1),
        };
        Vector3 centroid = new(0.5f, 0.5f, 0.5f);

        var nodes = new IFSNode3D[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 v = verts[i];

            // Base map: scale by 1/2 toward vertex v (fixed point at v).
            var baseMap = AffineMap3D.ScaleToCell(0.5f, 0.5f * v);

            // Rotation axis for this map.
            Vector3 axis = axisMode switch
            {
                TwistAxisMode.GlobalY => Vector3.UnitY,
                TwistAxisMode.CentroidToVertex => SafeNormalize(v - centroid),
                _ => Vector3.UnitY,
            };

            // Rotate around the vertex v: translate v to origin, rotate, translate back.
            var rotateAboutVertex =
                AffineMap3D.Translation(-v)
                    .Then(AffineMap3D.RotationAxis(axis, twistRadians))
                    .Then(AffineMap3D.Translation(v));

            // Full map: base scale-toward-vertex, then twist about the vertex.
            var twisted = baseMap.Then(rotateAboutVertex);

            nodes[i] = new IFSNode3D(Transform: twisted, Label: $"twist-tet-{i}");
        }
        return IFS3D.FromNodes(nodes);
    }

    private static Vector3 SafeNormalize(Vector3 v)
    {
        float len = v.Length();
        return len > 1e-8f ? v / len : Vector3.UnitY;
    }
}

/// <summary>
/// How to choose the rotation axis for a twisted IFS map.
/// </summary>
public enum TwistAxisMode
{
    /// <summary>Axis runs from the figure's centroid to the map's fixed point.</summary>
    CentroidToVertex,
    /// <summary>All maps rotate around the world Y axis.</summary>
    GlobalY,
}

using System.Numerics;
using Parsec.Core.Transforms;

namespace Parsec.Core.Ifs;

/// <summary>
/// Factory methods for canonical 3D IFSes. Useful as test cases and as
/// starting points for variations.
/// </summary>
public static class CanonicalIFS3D
{
    /// <summary>
    /// The Sierpiński tetrahedron: four half-scale maps placing copies at the
    /// vertices of a regular tetrahedron inscribed in the unit cube.
    /// </summary>
    /// <remarks>
    /// Uses the four "alternating cube corners" tetrahedron: vertices at
    /// (0,0,0), (1,1,0), (1,0,1), (0,1,1). The attractor lives in the
    /// unit cube [0,1]^3 and is self-similar with similarity ratio 1/2.
    /// </remarks>
    public static IFS3D SierpinskiTetrahedron()
    {
        Vector3[] vertices =
        [
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 1, 1),
        ];

        string[] labels = ["v0", "v1", "v2", "v3"];

        var nodes = new IFSNode3D[4];
        for (int i = 0; i < 4; i++)
        {
            // Half-scale toward the vertex: p -> (p + v) / 2 = 0.5 * p + 0.5 * v
            nodes[i] = new IFSNode3D(
                Transform: AffineMap3D.ScaleToCell(0.5f, vertices[i] * 0.5f),
                Label: labels[i]);
        }
        return IFS3D.FromNodes(nodes);
    }

    /// <summary>
    /// The Menger sponge: 20 third-scale maps placing copies at the corners
    /// and edge midpoints of a 3x3x3 grid (skipping face centers and the
    /// volumetric center — 27 - 6 - 1 = 20 cells).
    /// </summary>
    /// <remarks>
    /// The skipped cells are: the central cell (1,1,1), and the six
    /// face-center cells (one per face: (0,1,1), (2,1,1), (1,0,1), (1,2,1),
    /// (1,1,0), (1,1,2)). The remaining 20 cells form the sponge.
    /// </remarks>
    public static IFS3D MengerSponge()
    {
        const float s = 1f / 3f;
        var nodes = new List<IFSNode3D>(20);
        for (int z = 0; z < 3; z++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    // Skip if two or more coordinates are 1 (the center axis crossings).
                    int center = (x == 1 ? 1 : 0) + (y == 1 ? 1 : 0) + (z == 1 ? 1 : 0);
                    if (center >= 2) continue;

                    nodes.Add(new IFSNode3D(
                        Transform: AffineMap3D.ScaleToCell(s, new Vector3(x * s, y * s, z * s)),
                        Label: $"cell-{x}-{y}-{z}"));
                }
            }
        }
        return IFS3D.FromNodes(nodes.ToArray());
    }

    /// <summary>
    /// A 3D Sierpiński carpet (Cantor dust × Cantor dust × Cantor dust, in 3D
    /// terms — really a Sierpiński-2D-style omission applied per slice). Each
    /// of 8 corner cells of a 2x2x2 grid is half-scaled.
    /// </summary>
    /// <remarks>
    /// This isn't the classic Menger sponge — it's the simpler octree
    /// equivalent of a Sierpiński carpet. Useful as an intermediate test
    /// between the tetrahedron (4 maps) and the sponge (20 maps).
    /// </remarks>
    public static IFS3D OctreeCorners()
    {
        var nodes = new List<IFSNode3D>(8);
        for (int z = 0; z < 2; z++)
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
        {
            nodes.Add(new IFSNode3D(
                Transform: AffineMap3D.ScaleToCell(0.5f, new Vector3(x * 0.5f, y * 0.5f, z * 0.5f)),
                Label: $"corner-{x}-{y}-{z}"));
        }
        return IFS3D.FromNodes(nodes.ToArray());
    }
}

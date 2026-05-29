using System.Collections.Immutable;
using System.Numerics;

namespace Parsec.Core.Geometry;

/// <summary>
/// A simple closed polygon in 2D, represented as a sequence of vertices in
/// counter-clockwise order. Used as a seed shape by renderers that need one
/// (e.g., deterministic subdivision draws the seed at each leaf).
/// </summary>
public sealed record Polygon2D(ImmutableArray<Vector2> Vertices)
{
    public int Count => Vertices.Length;

    public static Polygon2D FromVertices(params Vector2[] vertices) =>
        new(vertices.ToImmutableArray());

    /// <summary>
    /// The unit square with corners at (0,0), (1,0), (1,1), (0,1).
    /// </summary>
    public static Polygon2D UnitSquare { get; } = FromVertices(
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1));

    /// <summary>
    /// A regular n-gon centered at <paramref name="center"/> with circumradius
    /// <paramref name="radius"/>. First vertex points "up" (toward +Y).
    /// </summary>
    public static Polygon2D Regular(int sides, Vector2 center, float radius)
    {
        if (sides < 3) throw new ArgumentOutOfRangeException(nameof(sides), "need at least 3 sides");

        var verts = ImmutableArray.CreateBuilder<Vector2>(sides);
        for (int i = 0; i < sides; i++)
        {
            float theta = 2f * MathF.PI * i / sides - MathF.PI / 2f;
            verts.Add(new Vector2(center.X + radius * MathF.Cos(theta),
                                  center.Y + radius * MathF.Sin(theta)));
        }
        return new Polygon2D(verts.ToImmutable());
    }

    /// <summary>
    /// Equilateral triangle inscribed in the unit square (top-vertex at (0.5, 1),
    /// base on y=0). Useful as a Sierpiński-triangle seed.
    /// </summary>
    public static Polygon2D UnitTriangle { get; } = FromVertices(
        new Vector2(0.5f, 1f),
        new Vector2(0f, 0f),
        new Vector2(1f, 0f));
}

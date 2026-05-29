using System.Numerics;
using Parsec.Core.Transforms;

namespace Parsec.Core.Ifs;

/// <summary>
/// Factory methods for "fractal of a knot" IFSes — a non-classical
/// construction where each map contracts everything toward a sample point on
/// a parametric closed curve.
/// </summary>
/// <remarks>
/// <para>
/// Given a closed curve C(t) parameterized over [0, 2π), sample N points
/// p_i = C(2πi/N), and define N maps T_i(x) = s*(x - p_i) + p_i = s*x + (1-s)*p_i,
/// each a similarity contracting toward p_i with ratio s.
/// </para>
/// <para>
/// The attractor depends qualitatively on s:
/// <list type="bullet">
/// <item><b>Small s</b> (~0.1–0.2): The contracted copies are small relative to
///   the sample spacing, so the attractor is a string of disjoint fractal
///   "beads" — one near each sample point — clearly tracing the curve.</item>
/// <item><b>Medium s</b> (~0.3–0.4): The contracted copies overlap a little.
///   The beads merge into a connected fractal "rope" along the curve.</item>
/// <item><b>Large s</b> (~0.5+): Heavy overlap. The attractor fills a fuzzy
///   tube around (or even beyond) the curve, dominated by the convex-combination
///   structure of the contracted maps rather than the curve's topology.</item>
/// </list>
/// </para>
/// <para>
/// This is not a standard "named" IFS construction — it's a pure Hutchinson
/// IFS built from a curve, producing attractors that inherit the curve's
/// topology while having locally fractal structure. To my knowledge it does
/// not have an established name in the literature.
/// </para>
/// </remarks>
public static class KnotIFS
{
    /// <summary>
    /// Build an IFS from N samples of a closed parametric curve, with each map
    /// being a similarity of contraction <paramref name="contraction"/>
    /// fixing the corresponding sample point.
    /// </summary>
    /// <param name="curve">Parametric curve C: [0, 2π) → R^3.</param>
    /// <param name="sampleCount">Number of sample points (and resulting IFS maps).</param>
    /// <param name="contraction">
    /// Contraction ratio s in (0, 1). Lower values give discrete beads;
    /// higher values give continuous fractal tubes.
    /// </param>
    /// <param name="labelPrefix">Prefix for node labels, e.g. "trefoil".</param>
    public static IFS3D FromCurve(
        Func<float, Vector3> curve,
        int sampleCount,
        float contraction,
        string labelPrefix = "knot")
    {
        if (sampleCount < 2)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Need at least 2 sample points.");
        if (contraction <= 0f || contraction >= 1f)
            throw new ArgumentOutOfRangeException(nameof(contraction), "Contraction must be in (0, 1).");

        var nodes = new IFSNode3D[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = MathF.PI * 2f * i / sampleCount;
            Vector3 p = curve(t);
            // T_i(x) = s*x + (1-s)*p_i
            var transform = AffineMap3D.ScaleToCell(contraction, (1f - contraction) * p);
            nodes[i] = new IFSNode3D(Transform: transform, Label: $"{labelPrefix}-{i}");
        }
        return IFS3D.FromNodes(nodes);
    }

    /// <summary>
    /// Trefoil knot — the simplest non-trivial knot, with 3-fold rotational
    /// symmetry around the z-axis.
    /// </summary>
    /// <remarks>
    /// Uses the standard parameterization:
    /// x(t) = sin(t) + 2 sin(2t), y(t) = cos(t) - 2 cos(2t), z(t) = -sin(3t).
    /// </remarks>
    public static IFS3D TrefoilKnot(int sampleCount = 24, float contraction = 0.20f)
    {
        return FromCurve(Trefoil, sampleCount, contraction, "trefoil");
    }

    /// <summary>
    /// Figure-eight knot — the next-simplest knot after the trefoil. 2-fold symmetry.
    /// </summary>
    public static IFS3D FigureEightKnot(int sampleCount = 32, float contraction = 0.15f)
    {
        return FromCurve(FigureEight, sampleCount, contraction, "fig8");
    }

    /// <summary>
    /// (p, q)-torus knot. The trefoil is the (2, 3) torus knot;
    /// (3, 2) gives the same knot rotated. (2, 5), (3, 5), etc. give richer knots.
    /// </summary>
    public static IFS3D TorusKnot(
        int p, int q,
        int sampleCount = 32,
        float contraction = 0.15f,
        float majorRadius = 2f,
        float minorRadius = 1f)
    {
        Vector3 Curve(float t)
        {
            float r = majorRadius + minorRadius * MathF.Cos(q * t);
            return new Vector3(
                r * MathF.Cos(p * t),
                r * MathF.Sin(p * t),
                minorRadius * MathF.Sin(q * t));
        }
        return FromCurve(Curve, sampleCount, contraction, $"torus-{p}-{q}");
    }

    private static Vector3 Trefoil(float t) => new(
        MathF.Sin(t) + 2f * MathF.Sin(2f * t),
        MathF.Cos(t) - 2f * MathF.Cos(2f * t),
        -MathF.Sin(3f * t));

    private static Vector3 FigureEight(float t) => new(
        (2f + MathF.Cos(2f * t)) * MathF.Cos(3f * t),
        (2f + MathF.Cos(2f * t)) * MathF.Sin(3f * t),
        MathF.Sin(4f * t));
}

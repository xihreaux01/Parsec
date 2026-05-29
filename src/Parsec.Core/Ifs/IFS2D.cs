using System.Collections.Immutable;

namespace Parsec.Core.Ifs;

/// <summary>
/// A 2D iterated function system: an unordered set of <see cref="IFSNode2D"/>
/// representing the maps whose Hutchinson union the attractor is the fixed point of.
/// </summary>
/// <remarks>
/// <para>
/// The IFS is a pure mathematical object — it knows nothing about how it will be
/// rendered. The same IFS can feed into any number of renderers (deterministic
/// subdivision, chaos game, density accumulation, distance estimation), each of
/// which may interpret the optional node metadata differently.
/// </para>
/// <para>
/// <see cref="Union"/> is concatenation of node lists. Weights are preserved
/// as-is; any renormalization happens at the renderer.
/// </para>
/// </remarks>
public sealed record IFS2D(ImmutableArray<IFSNode2D> Nodes)
{
    public static IFS2D FromNodes(params IFSNode2D[] nodes) =>
        new(nodes.ToImmutableArray());

    public static IFS2D Union(params IFS2D[] systems)
    {
        if (systems.Length == 0)
            return new IFS2D(ImmutableArray<IFSNode2D>.Empty);
        if (systems.Length == 1)
            return systems[0];

        var builder = ImmutableArray.CreateBuilder<IFSNode2D>();
        foreach (var s in systems)
            builder.AddRange(s.Nodes);
        return new IFS2D(builder.ToImmutable());
    }

    /// <summary>
    /// Convenience: <c>a | b</c> is <see cref="Union"/>.
    /// </summary>
    public static IFS2D operator |(IFS2D a, IFS2D b) => Union(a, b);

    /// <summary>
    /// Total raw weight across all nodes (not normalized).
    /// </summary>
    public float TotalWeight
    {
        get
        {
            float sum = 0f;
            foreach (var n in Nodes) sum += n.Weight;
            return sum;
        }
    }

    /// <summary>
    /// True iff every node's primary transform is a contraction (spectral norm &lt; 1).
    /// A standard contractive IFS has a unique compact attractor by Banach's theorem;
    /// non-contractive systems can still produce interesting structure but the
    /// attractor may be unbounded or degenerate.
    /// </summary>
    public bool IsContractive
    {
        get
        {
            foreach (var n in Nodes)
                if (!n.Transform.IsContractive) return false;
            return true;
        }
    }
}

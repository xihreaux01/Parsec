using System.Numerics;

namespace Parsec.Core.Attractors;

/// <summary>
/// Uniform spatial hash over an attractor trajectory, so a raymarcher's
/// distance-to-nearest-segment query only tests segments in the 3x3x3 cell
/// neighborhood of the query point instead of all N. Ported from Meadow's Unity
/// BuildSpatialHash and validated in Python (the hash cut per-query work ~1000x).
///
/// Layout produced (all flat arrays, ready for GPU upload):
///   - Points:        xyz = position, w = progress (one Vector4 per point)
///   - CellOffsets:   per cell, the start index into SortedIndices (prefix sum)
///   - CellCounts:    per cell, how many point-indices it owns
///   - SortedIndices: point indices grouped by cell
/// A segment i connects Points[i] and Points[i+1]; it is registered in the cell
/// of BOTH endpoints (so a query cell finds segments that pass through it even
/// when only one endpoint lands inside).
/// </summary>
public sealed class AttractorHash
{
    public int GridSize { get; }
    public Vector3 BoundsMin { get; }
    public Vector3 BoundsMax { get; }

    public Vector4[] Points { get; }       // xyz + progress
    public int[] CellOffsets { get; }      // length GridSize^3
    public int[] CellCounts { get; }       // length GridSize^3
    public int[] SortedIndices { get; }    // length = number of (point,cell) entries

    private AttractorHash(int gridSize, Vector3 lo, Vector3 hi,
        Vector4[] points, int[] offsets, int[] counts, int[] sorted)
    {
        GridSize = gridSize;
        BoundsMin = lo;
        BoundsMax = hi;
        Points = points;
        CellOffsets = offsets;
        CellCounts = counts;
        SortedIndices = sorted;
    }

    public static AttractorHash Build(
        IReadOnlyList<ThomasAttractor.TrajectoryPoint> trajectory, int gridSize = 64)
    {
        int n = trajectory.Count;
        var points = new Vector4[n];
        var lo = new Vector3(float.MaxValue);
        var hi = new Vector3(float.MinValue);
        for (int i = 0; i < n; i++)
        {
            var pos = trajectory[i].Position;
            points[i] = new Vector4(pos, trajectory[i].Progress);
            lo = Vector3.Min(lo, pos);
            hi = Vector3.Max(hi, pos);
        }

        // Pad the bounds slightly so points on the boundary bin cleanly.
        var pad = (hi - lo) * 0.05f + new Vector3(1e-4f);
        lo -= pad;
        hi += pad;
        var extent = hi - lo;

        int totalCells = gridSize * gridSize * gridSize;
        var counts = new int[totalCells];

        (int x, int y, int z) CellXyz(Vector3 p)
        {
            var u = (p - lo) / extent;
            return (Math.Clamp((int)(u.X * gridSize), 0, gridSize - 1),
                    Math.Clamp((int)(u.Y * gridSize), 0, gridSize - 1),
                    Math.Clamp((int)(u.Z * gridSize), 0, gridSize - 1));
        }

        // Register each SEGMENT in every cell its endpoints' bounding block
        // spans -- not just the two endpoint cells. A segment is a line that can
        // pass THROUGH a cell neither endpoint lands in; if we only bin
        // endpoints, a ray querying such a pass-through cell misses the segment,
        // oversteps, and bites a notch out of the tube. Segments are one RK4
        // step long, so the spanned block is normally 1-2 cells per axis -- the
        // conservative block is cheap and closes the gap exactly. We build a
        // list of (cell, pointIndex) entries, where pointIndex i denotes the
        // segment i..i+1.
        var entryCell = new List<int>(n * 2);
        var entryPoint = new List<int>(n * 2);
        for (int i = 0; i < n - 1; i++)
        {
            var (ax, ay, az) = CellXyz(new Vector3(points[i].X, points[i].Y, points[i].Z));
            var (bx, by, bz) = CellXyz(new Vector3(points[i + 1].X, points[i + 1].Y, points[i + 1].Z));
            int x0 = Math.Min(ax, bx), x1 = Math.Max(ax, bx);
            int y0 = Math.Min(ay, by), y1 = Math.Max(ay, by);
            int z0 = Math.Min(az, bz), z1 = Math.Max(az, bz);
            for (int z = z0; z <= z1; z++)
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int c = x + y * gridSize + z * gridSize * gridSize;
                entryCell.Add(c);
                entryPoint.Add(i);
                counts[c]++;
            }
        }

        // Prefix-sum offsets.
        var offsets = new int[totalCells];
        offsets[0] = 0;
        for (int i = 1; i < totalCells; i++)
            offsets[i] = offsets[i - 1] + counts[i - 1];

        // Scatter segment indices into cell-grouped order.
        var sorted = new int[entryCell.Count];
        var cursor = (int[])offsets.Clone();
        for (int e = 0; e < entryCell.Count; e++)
        {
            int c = entryCell[e];
            sorted[cursor[c]++] = entryPoint[e];
        }

        return new AttractorHash(gridSize, lo, hi, points, offsets, counts, sorted);
    }
}

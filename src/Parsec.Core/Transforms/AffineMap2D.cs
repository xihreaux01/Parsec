using System.Numerics;
using System.Runtime.CompilerServices;

namespace Parsec.Core.Transforms;

/// <summary>
/// A 2D affine transformation: <c>p' = M*p + t</c> where M is a 2x2 linear matrix and t a 2D translation.
/// </summary>
/// <remarks>
/// <para>
/// Stored as six floats laid out so that composition and application reduce to a few
/// scalar multiply-adds, and so that the field layout matches a GPU <c>float3x2</c>
/// when we move rendering to compute shaders later.
/// </para>
/// <para>
/// Matrix form (column vector convention):
/// <code>
///   | A  B  Tx |
///   | C  D  Ty |
///   | 0  0   1 |
/// </code>
/// So <c>p' = (A*p.x + B*p.y + Tx, C*p.x + D*p.y + Ty)</c>.
/// </para>
/// <para>
/// Composition order: <c>a.Then(b)</c> means "apply a, then b". This matches the
/// natural reading order at call sites and the Python spike's <c>.then(...)</c> chaining.
/// In matrix terms, <c>a.Then(b)</c> equals <c>b * a</c>.
/// </para>
/// </remarks>
public readonly struct AffineMap2D : IEquatable<AffineMap2D>
{
    public readonly float A, B, Tx;
    public readonly float C, D, Ty;

    public AffineMap2D(float a, float b, float c, float d, float tx, float ty)
    {
        A = a; B = b; Tx = tx;
        C = c; D = d; Ty = ty;
    }

    // ----- Factory helpers -----

    public static AffineMap2D Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public static AffineMap2D Translation(float tx, float ty) => new(1, 0, 0, 1, tx, ty);
    public static AffineMap2D Translation(Vector2 t) => new(1, 0, 0, 1, t.X, t.Y);

    public static AffineMap2D Scale(float s) => new(s, 0, 0, s, 0, 0);
    public static AffineMap2D Scale(float sx, float sy) => new(sx, 0, 0, sy, 0, 0);

    public static AffineMap2D Rotation(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new AffineMap2D(c, -s, s, c, 0, 0);
    }

    /// <summary>
    /// Shear: <c>(x, y) -> (x + shx*y, y + shy*x)</c>.
    /// </summary>
    public static AffineMap2D Shear(float shx, float shy) => new(1, shx, shy, 1, 0, 0);

    /// <summary>
    /// Rotate by <paramref name="radians"/> and uniformly scale by <paramref name="scale"/>,
    /// about <paramref name="center"/>. Useful for placing rotated cells (diamonds, spirals).
    /// </summary>
    public static AffineMap2D RotateScaleAt(float radians, float scale, Vector2 center)
    {
        // Translate center to origin, rotate+scale, translate back.
        return Compose(
            Translation(-center.X, -center.Y),
            Rotation(radians),
            Scale(scale),
            Translation(center.X, center.Y));
    }

    /// <summary>
    /// Scale uniformly by <paramref name="scale"/>, then translate so that the
    /// transformed unit square's lower-left corner lands at <paramref name="offset"/>.
    /// Useful for placing a sub-IFS into a grid cell.
    /// </summary>
    public static AffineMap2D ScaleToCell(float scale, Vector2 offset)
    {
        return new AffineMap2D(scale, 0, 0, scale, offset.X, offset.Y);
    }

    // ----- Composition -----

    /// <summary>
    /// <c>this.Then(next).Apply(p) == next.Apply(this.Apply(p))</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AffineMap2D Then(AffineMap2D next)
    {
        // result = next * this
        // (next applied AFTER this)
        return new AffineMap2D(
            a:  next.A * A  + next.B * C,
            b:  next.A * B  + next.B * D,
            c:  next.C * A  + next.D * C,
            d:  next.C * B  + next.D * D,
            tx: next.A * Tx + next.B * Ty + next.Tx,
            ty: next.C * Tx + next.D * Ty + next.Ty);
    }

    /// <summary>
    /// Compose left-to-right: <c>Compose(a, b, c).Apply(p) == c.Apply(b.Apply(a.Apply(p)))</c>.
    /// </summary>
    public static AffineMap2D Compose(params AffineMap2D[] maps)
    {
        var result = Identity;
        for (int i = 0; i < maps.Length; i++)
            result = result.Then(maps[i]);
        return result;
    }

    // ----- Application -----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Apply(Vector2 p) => new(A * p.X + B * p.Y + Tx, C * p.X + D * p.Y + Ty);

    /// <summary>
    /// Applies the transform to an array of points, writing into a destination span.
    /// Destination may alias the source.
    /// </summary>
    public void ApplyPolygon(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("destination too small", nameof(destination));

        for (int i = 0; i < source.Length; i++)
        {
            var p = source[i];
            destination[i] = new Vector2(A * p.X + B * p.Y + Tx, C * p.X + D * p.Y + Ty);
        }
    }

    public Vector2[] ApplyPolygon(ReadOnlySpan<Vector2> source)
    {
        var result = new Vector2[source.Length];
        ApplyPolygon(source, result);
        return result;
    }

    // ----- Inverse / determinant (useful for some renderers) -----

    public float Determinant => A * D - B * C;

    public bool TryInvert(out AffineMap2D inverse)
    {
        float det = Determinant;
        if (MathF.Abs(det) < float.Epsilon)
        {
            inverse = default;
            return false;
        }
        float invDet = 1f / det;
        float a =  D * invDet;
        float b = -B * invDet;
        float c = -C * invDet;
        float d =  A * invDet;
        float tx = -(a * Tx + b * Ty);
        float ty = -(c * Tx + d * Ty);
        inverse = new AffineMap2D(a, b, c, d, tx, ty);
        return true;
    }

    /// <summary>
    /// Largest singular value — the worst-case stretching factor.
    /// A map is contractive iff this is &lt; 1.
    /// </summary>
    public float SpectralNorm
    {
        get
        {
            // For 2x2 M, sigma_max = sqrt(largest eigenvalue of M^T M).
            // M^T M = | A^2 + C^2     A*B + C*D   |
            //        | A*B + C*D     B^2 + D^2   |
            // Eigenvalues: trace/2 +/- sqrt((trace/2)^2 - det).
            float m00 = A * A + C * C;
            float m11 = B * B + D * D;
            float m01 = A * B + C * D;
            float trace = m00 + m11;
            float det = m00 * m11 - m01 * m01;
            float disc = MathF.Max(0f, trace * trace * 0.25f - det);
            return MathF.Sqrt(trace * 0.5f + MathF.Sqrt(disc));
        }
    }

    public bool IsContractive => SpectralNorm < 1f;

    // ----- Equality -----

    public bool Equals(AffineMap2D other) =>
        A == other.A && B == other.B && Tx == other.Tx &&
        C == other.C && D == other.D && Ty == other.Ty;

    public override bool Equals(object? obj) => obj is AffineMap2D m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(A, B, C, D, Tx, Ty);

    public static bool operator ==(AffineMap2D a, AffineMap2D b) => a.Equals(b);
    public static bool operator !=(AffineMap2D a, AffineMap2D b) => !a.Equals(b);

    public override string ToString() =>
        $"AffineMap2D[[{A:0.###}, {B:0.###}, {Tx:0.###}], [{C:0.###}, {D:0.###}, {Ty:0.###}]]";
}

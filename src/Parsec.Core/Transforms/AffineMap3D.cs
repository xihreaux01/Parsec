using System.Numerics;
using System.Runtime.CompilerServices;

namespace Parsec.Core.Transforms;

/// <summary>
/// A 3D affine transformation: <c>p' = M*p + t</c> where M is a 3x3 linear matrix
/// and t a 3D translation.
/// </summary>
/// <remarks>
/// <para>
/// Stored as twelve floats laid out so that composition and application are
/// straight scalar multiply-adds, and so that the field layout matches a GPU
/// <c>float4x3</c> (twelve floats, 3 rows of 4 floats each row-major with the
/// last column being translation) when we move to compute shaders later.
/// </para>
/// <para>
/// Matrix form (column vector convention):
/// <code>
///   | M00 M01 M02 Tx |
///   | M10 M11 M12 Ty |
///   | M20 M21 M22 Tz |
///   |  0   0   0  1  |
/// </code>
/// So <c>p' = (M00*p.x + M01*p.y + M02*p.z + Tx, ...)</c>.
/// </para>
/// <para>
/// Composition order matches <see cref="AffineMap2D"/>: <c>a.Then(b)</c> means
/// "apply a, then b". In matrix terms, <c>a.Then(b) == b * a</c>.
/// </para>
/// </remarks>
public readonly struct AffineMap3D : IEquatable<AffineMap3D>
{
    public readonly float M00, M01, M02, Tx;
    public readonly float M10, M11, M12, Ty;
    public readonly float M20, M21, M22, Tz;

    public AffineMap3D(
        float m00, float m01, float m02, float tx,
        float m10, float m11, float m12, float ty,
        float m20, float m21, float m22, float tz)
    {
        M00 = m00; M01 = m01; M02 = m02; Tx = tx;
        M10 = m10; M11 = m11; M12 = m12; Ty = ty;
        M20 = m20; M21 = m21; M22 = m22; Tz = tz;
    }

    // ----- Factory helpers -----

    public static AffineMap3D Identity { get; } = new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0);

    public static AffineMap3D Translation(float tx, float ty, float tz) => new(
        1, 0, 0, tx,
        0, 1, 0, ty,
        0, 0, 1, tz);

    public static AffineMap3D Translation(Vector3 t) => Translation(t.X, t.Y, t.Z);

    public static AffineMap3D Scale(float s) => new(
        s, 0, 0, 0,
        0, s, 0, 0,
        0, 0, s, 0);

    public static AffineMap3D Scale(float sx, float sy, float sz) => new(
        sx, 0,  0,  0,
        0,  sy, 0,  0,
        0,  0,  sz, 0);

    /// <summary>
    /// Rotation about the X axis. Right-handed: a positive angle rotates +Y toward +Z.
    /// </summary>
    public static AffineMap3D RotationX(float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        return new AffineMap3D(
            1, 0,  0, 0,
            0, c, -s, 0,
            0, s,  c, 0);
    }

    /// <summary>
    /// Rotation about the Y axis. Right-handed: a positive angle rotates +Z toward +X.
    /// </summary>
    public static AffineMap3D RotationY(float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        return new AffineMap3D(
             c, 0, s, 0,
             0, 1, 0, 0,
            -s, 0, c, 0);
    }

    /// <summary>
    /// Rotation about the Z axis. Right-handed: a positive angle rotates +X toward +Y.
    /// </summary>
    public static AffineMap3D RotationZ(float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        return new AffineMap3D(
            c, -s, 0, 0,
            s,  c, 0, 0,
            0,  0, 1, 0);
    }

    /// <summary>
    /// Rotation about an arbitrary axis (must be unit length).
    /// Right-handed (Rodrigues' formula).
    /// </summary>
    public static AffineMap3D RotationAxis(Vector3 unitAxis, float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        float t = 1f - c;
        float x = unitAxis.X, y = unitAxis.Y, z = unitAxis.Z;
        return new AffineMap3D(
            t*x*x + c,    t*x*y - s*z, t*x*z + s*y, 0,
            t*x*y + s*z,  t*y*y + c,   t*y*z - s*x, 0,
            t*x*z - s*y,  t*y*z + s*x, t*z*z + c,   0);
    }

    /// <summary>
    /// Scale uniformly by <paramref name="scale"/>, then translate so that the
    /// transformed unit cube's lower corner lands at <paramref name="offset"/>.
    /// Useful for placing a sub-IFS into a grid cell of the unit cube.
    /// </summary>
    public static AffineMap3D ScaleToCell(float scale, Vector3 offset) => new(
        scale, 0,     0,     offset.X,
        0,     scale, 0,     offset.Y,
        0,     0,     scale, offset.Z);

    // ----- Composition -----

    /// <summary>
    /// <c>this.Then(next).Apply(p) == next.Apply(this.Apply(p))</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AffineMap3D Then(AffineMap3D next)
    {
        // result = next * this (next applied AFTER this).
        // Compute next.M * this.M for the linear part, and next.M * this.t + next.t for translation.
        return new AffineMap3D(
            // Row 0
            next.M00*M00 + next.M01*M10 + next.M02*M20,
            next.M00*M01 + next.M01*M11 + next.M02*M21,
            next.M00*M02 + next.M01*M12 + next.M02*M22,
            next.M00*Tx  + next.M01*Ty  + next.M02*Tz + next.Tx,
            // Row 1
            next.M10*M00 + next.M11*M10 + next.M12*M20,
            next.M10*M01 + next.M11*M11 + next.M12*M21,
            next.M10*M02 + next.M11*M12 + next.M12*M22,
            next.M10*Tx  + next.M11*Ty  + next.M12*Tz + next.Ty,
            // Row 2
            next.M20*M00 + next.M21*M10 + next.M22*M20,
            next.M20*M01 + next.M21*M11 + next.M22*M21,
            next.M20*M02 + next.M21*M12 + next.M22*M22,
            next.M20*Tx  + next.M21*Ty  + next.M22*Tz + next.Tz);
    }

    /// <summary>
    /// Compose left-to-right: <c>Compose(a, b, c).Apply(p) == c.Apply(b.Apply(a.Apply(p)))</c>.
    /// </summary>
    public static AffineMap3D Compose(params AffineMap3D[] maps)
    {
        var result = Identity;
        for (int i = 0; i < maps.Length; i++)
            result = result.Then(maps[i]);
        return result;
    }

    // ----- Application -----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Apply(Vector3 p) => new(
        M00 * p.X + M01 * p.Y + M02 * p.Z + Tx,
        M10 * p.X + M11 * p.Y + M12 * p.Z + Ty,
        M20 * p.X + M21 * p.Y + M22 * p.Z + Tz);

    /// <summary>
    /// Applies the transform to an array of points, writing into a destination span.
    /// Destination may alias the source.
    /// </summary>
    public void ApplyMany(ReadOnlySpan<Vector3> source, Span<Vector3> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("destination too small", nameof(destination));
        for (int i = 0; i < source.Length; i++)
            destination[i] = Apply(source[i]);
    }

    // ----- Inverse / determinant -----

    public float Determinant =>
        M00 * (M11 * M22 - M12 * M21)
      - M01 * (M10 * M22 - M12 * M20)
      + M02 * (M10 * M21 - M11 * M20);

    /// <summary>
    /// Computes the inverse, returning false if the linear part is singular.
    /// </summary>
    public bool TryInvert(out AffineMap3D inverse)
    {
        // Cofactor expansion for the 3x3 linear inverse.
        float c00 =  (M11 * M22 - M12 * M21);
        float c01 = -(M10 * M22 - M12 * M20);
        float c02 =  (M10 * M21 - M11 * M20);

        float det = M00 * c00 + M01 * c01 + M02 * c02;
        if (MathF.Abs(det) < float.Epsilon)
        {
            inverse = default;
            return false;
        }
        float invDet = 1f / det;

        float c10 = -(M01 * M22 - M02 * M21);
        float c11 =  (M00 * M22 - M02 * M20);
        float c12 = -(M00 * M21 - M01 * M20);
        float c20 =  (M01 * M12 - M02 * M11);
        float c21 = -(M00 * M12 - M02 * M10);
        float c22 =  (M00 * M11 - M01 * M10);

        // Inverse linear part is the transpose-of-cofactor times invDet.
        float i00 = c00 * invDet, i01 = c10 * invDet, i02 = c20 * invDet;
        float i10 = c01 * invDet, i11 = c11 * invDet, i12 = c21 * invDet;
        float i20 = c02 * invDet, i21 = c12 * invDet, i22 = c22 * invDet;

        // Inverse translation: -M^-1 * t
        float itx = -(i00 * Tx + i01 * Ty + i02 * Tz);
        float ity = -(i10 * Tx + i11 * Ty + i12 * Tz);
        float itz = -(i20 * Tx + i21 * Ty + i22 * Tz);

        inverse = new AffineMap3D(
            i00, i01, i02, itx,
            i10, i11, i12, ity,
            i20, i21, i22, itz);
        return true;
    }

    /// <summary>
    /// Largest singular value of the linear part — the worst-case stretching factor.
    /// A map is contractive iff this is &lt; 1.
    /// </summary>
    /// <remarks>
    /// Uses power iteration on M^T M. Converges quickly (typically &lt; 20 iterations)
    /// for non-degenerate matrices. For exact closed-form, we'd need cubic eigenvalue
    /// solving; power iteration is simpler and good enough for the contraction check.
    /// </remarks>
    public float SpectralNorm
    {
        get
        {
            // Build A = M^T * M (symmetric positive semi-definite).
            float a00 = M00 * M00 + M10 * M10 + M20 * M20;
            float a11 = M01 * M01 + M11 * M11 + M21 * M21;
            float a22 = M02 * M02 + M12 * M12 + M22 * M22;
            float a01 = M00 * M01 + M10 * M11 + M20 * M21;
            float a02 = M00 * M02 + M10 * M12 + M20 * M22;
            float a12 = M01 * M02 + M11 * M12 + M21 * M22;

            // Power iteration for the largest eigenvalue of A.
            // Start with a generic vector to avoid landing in a null eigenvector by accident.
            float vx = 0.577f, vy = 0.577f, vz = 0.577f;
            float lambda = 0f;
            for (int i = 0; i < 30; i++)
            {
                // w = A * v
                float wx = a00 * vx + a01 * vy + a02 * vz;
                float wy = a01 * vx + a11 * vy + a12 * vz;
                float wz = a02 * vx + a12 * vy + a22 * vz;

                float norm = MathF.Sqrt(wx * wx + wy * wy + wz * wz);
                if (norm < 1e-20f) return 0f;
                float inv = 1f / norm;
                vx = wx * inv; vy = wy * inv; vz = wz * inv;

                float prev = lambda;
                lambda = norm;
                if (MathF.Abs(lambda - prev) < 1e-7f * lambda) break;
            }
            return MathF.Sqrt(MathF.Max(0f, lambda));
        }
    }

    public bool IsContractive => SpectralNorm < 1f;

    // ----- Equality -----

    public bool Equals(AffineMap3D other) =>
        M00 == other.M00 && M01 == other.M01 && M02 == other.M02 && Tx == other.Tx &&
        M10 == other.M10 && M11 == other.M11 && M12 == other.M12 && Ty == other.Ty &&
        M20 == other.M20 && M21 == other.M21 && M22 == other.M22 && Tz == other.Tz;

    public override bool Equals(object? obj) => obj is AffineMap3D m && Equals(m);

    public override int GetHashCode()
    {
        // HashCode.Combine takes at most 8 args; mix the twelve fields in two passes.
        int h1 = HashCode.Combine(M00, M01, M02, Tx, M10, M11, M12, Ty);
        int h2 = HashCode.Combine(M20, M21, M22, Tz);
        return HashCode.Combine(h1, h2);
    }

    public static bool operator ==(AffineMap3D a, AffineMap3D b) => a.Equals(b);
    public static bool operator !=(AffineMap3D a, AffineMap3D b) => !a.Equals(b);

    public override string ToString() =>
        $"AffineMap3D[[{M00:0.###}, {M01:0.###}, {M02:0.###}, {Tx:0.###}], " +
        $"[{M10:0.###}, {M11:0.###}, {M12:0.###}, {Ty:0.###}], " +
        $"[{M20:0.###}, {M21:0.###}, {M22:0.###}, {Tz:0.###}]]";
}

#version 430 core

// ===========================================================================
// Deep-zoom Mandelbrot -- perturbation + rebasing delta pass (fp64).
//
// One CPU-computed high-precision reference orbit Z_n (see ReferenceOrbit.cs)
// lives in an SSBO as dvec2. Every pixel is a cheap fp64 offset dc = c - C and
// iterates the perturbation
//     dz_{n+1} = 2*Z_n*dz_n + dz_n^2 + dc
// with rebasing (Zhuoran): when the full value |Z_m + dz| drops below |dz|, or
// the reference index runs to its end, fold the full value back into dz and
// restart the reference index at 0.
//
// This is a STANDALONE compute shader -- it does NOT compose with
// raymarch_main.glsl. It is the only genuinely new GPU piece of the 2D
// deep-zoom subsystem. Validated (identical loop + smooth output) against an
// mpmath oracle in the Python prototype: escape iters match the oracle, smooth
// mu matches to ~1e-12 away from knife-edge pixels.
//
// Output: one float per pixel = smooth iteration count, or -1.0 for in-set.
// SSAA + palette coloring are added at the pipeline level (next milestone);
// `jitter` is the per-sample sub-pixel offset that pass will drive.
//
// SSBO contract (deep-zoom pipeline, independent of the raymarch bindings):
//   binding 1 = RefOrbit  (dvec2 Z[refCount], from ReferenceOrbit.ToInterleaved)
//   binding 2 = MuOut      (float mu[width*height])
//   binding 4 = DeepParams (the struct below; mind std430 dvec2 16B alignment)
// ===========================================================================

layout(local_size_x = 8, local_size_y = 8) in;

layout(std430, binding = 1) readonly buffer RefOrbit {
    dvec2 Zref[];
};

layout(std430, binding = 2) writeonly buffer MuOut {
    float mu[];
};

layout(std430, binding = 4) readonly buffer DeepParams {
    int   width;
    int   height;
    int   rowOffset;     // tiled dispatch (TDR-safe), like the raymarch pipeline
    int   rowCount;
    int   refCount;      // valid entries in Zref
    int   maxIter;
    int   _pad0;
    int   _pad1;         // pad to a 16-byte boundary before the doubles
    dvec2 refDc;         // dc of the reference point vs the view center (0 if ref==center)
    dvec2 pixelDx;       // dc increment per +1 pixel in X  = (spacing, 0)
    dvec2 pixelDy;       // dc increment per +1 pixel in Y  = (0, -spacing)  (Y flipped)
    dvec2 jitter;        // sub-pixel offset in pixels (x,y), for SSAA
    double escapeR2;     // escape radius squared (>=4.0; larger = smoother coloring)
    double _pad2;
};

void main()
{
    uint gx = gl_GlobalInvocationID.x;
    uint gy = gl_GlobalInvocationID.y;
    if (gx >= uint(width) || gy >= uint(rowCount)) return;
    int  py = rowOffset + int(gy);
    if (py >= height) return;
    int  outIdx = py * width + int(gx);

    // Per-pixel dc = c - C, built in fp64 from the affine pixel->complex map.
    double fx = double(gx) - 0.5LF * double(width)  + jitter.x;
    double fy = double(py) - 0.5LF * double(height) + jitter.y;
    dvec2  dc = refDc + fx * pixelDx + fy * pixelDy;

    // Perturbation + rebasing.
    dvec2  dz  = dvec2(0.0LF);
    int    m   = 0;            // reference index
    int    esc = maxIter;
    double z2  = 0.0LF;        // |z|^2 at escape (for smooth coloring)

    for (int n = 0; n < maxIter; n++)
    {
        dvec2  Zr  = Zref[m];
        dvec2  z   = Zr + dz;                 // full value Z_n
        double az2 = z.x * z.x + z.y * z.y;
        if (az2 > escapeR2) { esc = n; z2 = az2; break; }

        // Rebase: full value smaller than the delta, or reference exhausted.
        double adz2 = dz.x * dz.x + dz.y * dz.y;
        if (az2 < adz2 || m >= refCount - 1)
        {
            dz = z;
            m  = 0;
            Zr = Zref[0];
        }

        // dz_{n+1} = 2*Zr*dz + dz^2 + dc   (complex arithmetic)
        dvec2 twoZrDz = dvec2(2.0LF * (Zr.x * dz.x - Zr.y * dz.y),
                              2.0LF * (Zr.x * dz.y + Zr.y * dz.x));
        dvec2 dzSq    = dvec2(dz.x * dz.x - dz.y * dz.y,
                              2.0LF * dz.x * dz.y);
        dz = twoZrDz + dzSq + dc;
        m++;
    }

    if (esc >= maxIter)
    {
        mu[outIdx] = -1.0;                    // in-set sentinel
    }
    else
    {
        // smooth iteration: (n+1) - log2(log|z|).  |z| = sqrt(z2).
        float lz = log(float(sqrt(z2)));      // log|z|  (>0 for |z|>1)
        mu[outIdx] = float(esc) + 1.0 - log2(max(lz, 1e-20));
    }
}

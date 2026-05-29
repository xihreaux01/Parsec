// =============================================================================
// Parsec strange-attractor tube distance estimator - core
// =============================================================================
//
// Includable chunk (no #version, no main). Concatenated after a #version line
// and before raymarch_main.glsl, like the other DE cores.
//
// UNLIKE the fold/inversion fractals, the attractor is not a closed-form field:
// it is a TRAJECTORY (a polyline of integrated ODE points) rendered as a glowing
// tube. The DE is "distance to the nearest trajectory segment, minus the tube
// radius". A brute-force nearest-segment search is O(N) over hundreds of
// thousands of points; instead we read a precomputed uniform SPATIAL HASH and
// only test segments in the 3x3x3 cell neighborhood of the query point. This
// approach + the step-clamping rule below were validated in Python (thomas_tube)
// before this port.
//
// This core uses its OWN SSBOs at bindings 6/7/8 (the shared raymarch shading
// uses only 1/4/5), so it slots into the shared pipeline as just another core
// providing estimate()/gTrap/attractorBoundingSphere()/deFudge(). Orbit-trap
// colour therefore works automatically.
//
// PARAMETER REUSE (shared FoldParams buffer, binding 1):
//   boxParams.x = tube radius
//   boxParams.y = grid size (N, as float; the grid is N^3)
//   rot.w       = DE fudge
//   boundSphere = bounding sphere for the fast-skip
//   juliaC.xyz  = bounds min,  surfParams.xyz = bounds max  (cloud AABB)

layout(std430, binding = 1) readonly buffer FoldParams {
    int   iterations;       // unused
    int   mode;             // unused
    int   juliaMode;        // unused
    int   pad0;

    vec4  boxParams;        // (tubeRadius, gridSize, _, _)
    vec4  surfParams;       // (boundsMax.xyz, _)
    vec4  juliaC;           // (boundsMin.xyz, _)
    vec4  rot;              // (_, _, _, fudge)
    vec4  boundSphere;      // (cx, cy, cz, r)
} fp;

// Trajectory points: xyz = position, w = progress along the orbit.
layout(std430, binding = 6) readonly buffer Trajectory {
    vec4 points[];
} traj;

// Spatial hash: two ints per cell, packed as (offset, count) pairs.
layout(std430, binding = 7) readonly buffer SpatialHash {
    int cells[];            // cells[2*c] = offset, cells[2*c+1] = count
} hash;

// Point indices grouped by cell (indexed by offset..offset+count).
layout(std430, binding = 8) readonly buffer SortedIndices {
    int idx[];
} sorted;

vec4 gTrap;

// --- helpers ------------------------------------------------------------------

ivec3 cellCoord(vec3 p, vec3 lo, vec3 ext, int n) {
    vec3 u = (p - lo) / ext;
    return clamp(ivec3(u * float(n)), ivec3(0), ivec3(n - 1));
}

float distToSegment(vec3 p, vec3 a, vec3 b) {
    vec3 ab = b - a;
    float t = clamp(dot(p - a, ab) / max(dot(ab, ab), 1e-12), 0.0, 1.0);
    return length(p - (a + t * ab));
}

// --- the DE -------------------------------------------------------------------

float estimate(vec3 p) {
    gTrap = vec4(1e20);

    float tubeR = fp.boxParams.x;
    int   n     = int(fp.boxParams.y);
    vec3  lo    = fp.juliaC.xyz;
    vec3  hi    = fp.surfParams.xyz;
    vec3  ext   = hi - lo;
    float minCell = min(ext.x, min(ext.y, ext.z)) / float(n);

    // If outside the cloud AABB by more than a cell, return distance to the box
    // (a safe large step; the hash neighbourhood is irrelevant out here).
    vec3 dBox = max(max(lo - p, p - hi), vec3(0.0));
    float outside = length(dBox);
    if (outside > minCell) {
        return outside;
    }

    int nPts = traj.points.length();
    ivec3 c = cellCoord(p, lo, ext, n);

    float best = 1e9;
    bool found = false;

    for (int dx = -1; dx <= 1; dx++)
    for (int dy = -1; dy <= 1; dy++)
    for (int dz = -1; dz <= 1; dz++) {
        ivec3 cc = c + ivec3(dx, dy, dz);
        if (cc.x < 0 || cc.y < 0 || cc.z < 0 ||
            cc.x >= n || cc.y >= n || cc.z >= n) continue;
        int cellIndex = cc.x + cc.y * n + cc.z * n * n;
        int offset = hash.cells[2 * cellIndex];
        int count  = hash.cells[2 * cellIndex + 1];
        for (int k = 0; k < count; k++) {
            int i = sorted.idx[offset + k];
            // Segment i connects point i and i+1 (skip the last point, which
            // has no following segment).
            if (i >= nPts - 1) continue;
            vec3 a = traj.points[i].xyz;
            vec3 b = traj.points[i + 1].xyz;
            float d = distToSegment(p, a, b);
            if (d < best) {
                best = d;
                found = true;
                // Orbit-trap accumulation on the tube: track closeness to the
                // origin, axes, and unit shell so the shared palette colours it.
                gTrap.x = min(gTrap.x, length(p));
                gTrap.y = min(gTrap.y, abs(p.x));
                gTrap.z = min(gTrap.z, length(p.xy));
                gTrap.w = min(gTrap.w, abs(length(p) - 1.0));
            }
        }
    }

    if (!found) {
        // Empty 3x3x3 neighbourhood: nearest segment is >= ~1 cell away. A
        // half-cell step is comfortably safe and kills the last grazing-angle
        // specks (where a full-cell step was marginally too generous on steeply
        // tangent rays). Costs more steps, but the field is cheap and fast here.
        return 0.5 * minCell;
    }
    // SAFETY CLAMP (see note): cap to half the neighbourhood's safe radius so
    // even steeply grazing rays can't skip the thin near-surface shell. Inert
    // near the surface, where best-tubeR is small and detail stays exact.
    return min(best - tubeR, 0.5 * minCell);
}

vec4 attractorBoundingSphere() {
    return fp.boundSphere;
}

float deFudge() {
    return fp.rot.w;
}

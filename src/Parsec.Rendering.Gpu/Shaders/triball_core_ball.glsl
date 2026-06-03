// Parsec mined fold core: sibling_solid  scale=-1.69 boundRadius~4.3  (genome: ([('abs',), ('box', 1.71), ('ball', 0.61, 1.49)], -1.69))
// Mandelbox-family. Linear DE: dr -> |scale|*(fold factors)*dr + 1, DE = |z|/dr,
// freezing each point on bailout. z0 = c = sample point (Mandelbrot convention).
//   boxParams = (scale, bailout, _, _) ; rot.w = DE fudge ; boundSphere = skip sphere
layout(std430, binding = 1) readonly buffer FoldParams {
    int iterations; int mode; int juliaMode; int pad0;
    vec4 boxParams; vec4 surfParams; vec4 juliaC; vec4 rot; vec4 boundSphere;
} fp;

vec4 gTrap;

float estimate(vec3 p) {
    float scale   = fp.boxParams.x;
    float bailout = fp.boxParams.y;
    vec3  z = p;
    vec3  c = p;
    float dr = 1.0;
    float r2, t;
    gTrap = vec4(1e20);

    for (int i = 0; i < fp.iterations; i++) {
        if (length(z) > bailout) break;          // freeze escaped points
        z = abs(z);                                           // abs fold
        z = clamp(z, -1.7100, 1.7100) * 2.0 - z;   // box fold
        r2 = dot(z, z);                                       // ball fold 0.61/1.49
        if (r2 < 0.37210)      { t = 2.22010/0.37210; z *= t; dr *= t; }
        else if (r2 < 2.22010) { t = 2.22010/r2;        z *= t; dr *= t; }
        z = scale * z + c;                        // affine
        dr = dr * abs(scale) + 1.0;
        gTrap.x = min(gTrap.x, length(z));
        gTrap.y = min(gTrap.y, abs(z.x));
        gTrap.z = min(gTrap.z, length(z.xy));
        gTrap.w = min(gTrap.w, abs(length(z) - 1.0));
    }
    return length(z) / max(dr, 1e-9);             // linear Mandelbox/KIFS DE
}

vec4 attractorBoundingSphere() { return fp.boundSphere; }
float deFudge()                { return fp.rot.w; }

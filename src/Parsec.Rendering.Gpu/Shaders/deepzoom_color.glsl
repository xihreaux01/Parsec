#version 430 core

// ===========================================================================
// Deep-zoom coloring + SSAA accumulate. Reads the per-pixel smooth iteration
// (mu) produced by deepzoom_delta.glsl, maps it through the SAME cosine palette
// as the raymarcher (trapAlbedo's cosPalette), and ADDS the resulting colour
// into the vec4 accumulator. Run once per AA sample (the delta pass re-fills mu
// with a fresh sub-pixel jitter each time); finalize then divides by the sample
// count. Colouring per-sample-then-averaging keeps in-set/out-set edges clean.
//
// Deep-zoom SSBO bindings:
//   2 = Mu        (float, from delta pass)      5 = Accum (vec4, accumulate)
//   6 = ColorParams (palette + bg)
// ===========================================================================

layout(local_size_x = 8, local_size_y = 8) in;

layout(std430, binding = 2) readonly buffer MuIn   { float mu[]; };
layout(std430, binding = 5)          buffer Accum  { vec4  colors[]; };

layout(std430, binding = 6) readonly buffer ColorParams {
    int   width;
    int   height;
    int   _p0;
    int   _p1;
    vec4  palBase;    // rgb = a (offset),     a = frequency
    vec4  palAmp;     // rgb = b (amplitude)
    vec4  palPhase;   // rgb = d (phase)
    vec4  bg;         // rgb = in-set / background colour
    float palScale;   // mu -> palette parameter scale
    float _p2;
    float _p3;
    float _p4;
} cp;

vec3 cosPalette(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
    return a + b * cos(6.28318530718 * (c * t + d));
}

void main() {
    uint gx = gl_GlobalInvocationID.x;
    uint gy = gl_GlobalInvocationID.y;
    if (gx >= uint(cp.width) || gy >= uint(cp.height)) return;
    int idx = int(gy) * cp.width + int(gx);

    float m = mu[idx];
    vec3 col;
    if (m < 0.0) {
        col = cp.bg.rgb;                       // in-set sentinel
    } else {
        float t = fract(m * cp.palScale);
        col = cosPalette(t, cp.palBase.rgb, cp.palAmp.rgb,
                          vec3(cp.palBase.a), cp.palPhase.rgb);
    }
    colors[idx] += vec4(col, 1.0);
}

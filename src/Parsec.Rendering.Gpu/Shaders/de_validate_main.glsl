// DE validation entry point. Concatenated after de_core.glsl by the loader.
// Adds the points input buffer (binding 2) and distances output (binding 3).

layout(local_size_x = 64) in;

layout(std430, binding = 2) readonly buffer Points {
    vec4 points[];
} pts;

layout(std430, binding = 3) writeonly buffer Output {
    float distances[];
} outBuf;

void main() {
    uint idx = gl_GlobalInvocationID.x;
    if (idx >= uint(q.pointCount)) return;
    vec3 p = pts.points[idx].xyz;
    outBuf.distances[idx] = estimate(p);
}

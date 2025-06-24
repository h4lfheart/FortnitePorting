#version 460 core

out vec4 FragColor;

in OUT_IN_VARIABLES {
    vec3 nearPoint;
    vec3 farPoint;
    vec3 cameraPos;
    mat4 proj;
    mat4 view;
    float near;
    float far;
} inVar;


uniform float u_GridScale1;
uniform float u_GridScale2;
uniform vec3 u_GridColor1;
uniform vec3 u_GridColor2;
uniform float u_FadeStart;
uniform float u_FadeEnd;

vec4 grid(vec3 fragPos, float scale, vec3 color, float baseOpacity) {
    vec2 coord = fragPos.xz * scale;
    vec2 derivative = fwidth(coord);
    vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
    float line = min(grid.x, grid.y);
    float opacity = (1.0 - min(line, 1.0)) * baseOpacity;

    // Axis highlighting
    float minimumX = min(derivative.x, 1.0) * 0.1;
    float minimumZ = min(derivative.y, 1.0) * 0.1;
    return vec4(color, opacity);
}

float computeDepth(vec3 pos) {
    vec4 clip_space_pos = inVar.proj * inVar.view * vec4(pos, 1.0);
    return (clip_space_pos.z / clip_space_pos.w) * 0.5 + 0.5;
}

float computeLinearDepth(vec3 pos) {
    vec4 clip_space_pos = inVar.proj * inVar.view * vec4(pos, 1.0);
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w) * 2.0 - 1.0;
    float linearDepth = (2.0 * inVar.near * inVar.far) / (inVar.far + inVar.near - clip_space_depth * (inVar.far - inVar.near));
    return linearDepth / inVar.far;
}

void main() {
    float t = -inVar.nearPoint.y / (inVar.farPoint.y - inVar.nearPoint.y);

    if (t < 0.0) {
        discard;
    }

    vec3 fragPos3D = inVar.nearPoint + t * (inVar.farPoint - inVar.nearPoint);

    float depth = computeDepth(fragPos3D);

    if (depth >= 1.0) {
        discard;
    }

    gl_FragDepth = depth;

    float distanceToCamera = length(fragPos3D - inVar.cameraPos);
    float fadeFactor = 1.0 - smoothstep(u_FadeStart, u_FadeEnd, distanceToCamera);

    if (fadeFactor <= 0.0) {
        discard;
    }

    vec4 grid1 = grid(fragPos3D, u_GridScale1, u_GridColor1, 0.8 * fadeFactor);
    vec4 grid2 = grid(fragPos3D, u_GridScale2, u_GridColor2, 0.6 * fadeFactor);

    vec4 combinedGrid = grid1 + grid2 * (1.0 - grid1.a);

    combinedGrid.a *= fadeFactor;

    if (combinedGrid.a < 0.01) {
        discard;
    }

    FragColor = combinedGrid;
}
#version 460 core

layout (location = 0) in vec3 vPos;

out OUT_IN_VARIABLES {
    vec3 nearPoint;
    vec3 farPoint;
    vec3 cameraPos;
    mat4 proj;
    mat4 view;
    float near;
    float far;
} outVar;

uniform mat4 u_Proj;
uniform mat4 u_View;
uniform float u_Near;
uniform float u_Far;
uniform vec3 u_CameraPos;

vec3 UnprojectPoint(vec2 xy, float z) {
    mat4 viewInv = inverse(u_View);
    mat4 projInv = inverse(u_Proj);
    vec4 unprojectedPoint = viewInv * projInv * vec4(xy, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

void main()
{
    outVar.near = u_Near;
    outVar.far = u_Far;
    outVar.proj = u_Proj;
    outVar.view = u_View;
    outVar.cameraPos = u_CameraPos;
    outVar.nearPoint = UnprojectPoint(vPos.xy, 0.0);
    outVar.farPoint = UnprojectPoint(vPos.xy, 1.0);
    gl_Position = vec4(vPos, 1.0);
}
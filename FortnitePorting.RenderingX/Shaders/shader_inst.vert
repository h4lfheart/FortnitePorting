#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aTangent;
layout (location = 3) in vec2 aTexCoord;
layout (location = 4) in float aMaterialLayer;

layout(std430, binding = 0) buffer InstanceMatrices
{
    mat4 matrices[];
};

out vec3 fPosition;
out vec3 fNormal;
out vec3 fTangent;
out vec2 fTexCoord;
out float fMaterialLayer;
out float fMirrorFlip;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vec4 finalPos = vec4(aPosition, 1.0);

    mat4 instanceMatrix = matrices[gl_InstanceID];

    mat3 normalMatrix = mat3(transpose(inverse(instanceMatrix)));

    float det = determinant(mat3(instanceMatrix));
    fMirrorFlip = (det < 0.0) ? -1.0 : 1.0;

    fNormal = normalize(aNormal * normalMatrix);
    fTangent = normalize(aTangent * normalMatrix);

    fPosition = vec3(finalPos * instanceMatrix);
    fTexCoord = aTexCoord;
    fMaterialLayer = aMaterialLayer;

    gl_Position = finalPos * instanceMatrix * uView * uProjection;
}
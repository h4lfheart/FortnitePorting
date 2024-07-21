#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in float aMaterialLayer;

out vec3 fPosition;
out vec2 fTexCoord;
out vec3 fNormal;
out vec3 fTangent;
out float fMaterialLayer;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vec4 finalPos = vec4(aPosition, 1.0);
    vec4 finalNormal = vec4(aNormal, 1.0);
    vec4 finalTangent = vec4(aTangent, 1.0);

    fPosition = vec3(finalPos * uTransform);
    fNormal = vec3(finalNormal * transpose(inverse(uTransform)));
    fTangent = vec3(finalTangent * transpose(inverse(uTransform)));
    fTexCoord = aTexCoord;
    fMaterialLayer = aMaterialLayer;

    gl_Position = finalPos * uTransform * uView * uProjection;
}
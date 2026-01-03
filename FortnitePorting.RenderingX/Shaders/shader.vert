#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aTangent;
layout (location = 3) in vec2 aTexCoord;
layout (location = 4) in float aMaterialLayer;


out vec3 fPosition;
out vec3 fNormal;
out vec3 fTangent;
out vec2 fTexCoord;
out float fMaterialLayer;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vec4 finalPos = vec4(aPosition, 1.0);
    vec3 transformedNormal = normalize((vec4(aNormal, 0.0) * transpose(inverse(uTransform))).xyz);
    vec3 transformedTangent = normalize((vec4(aTangent, 0.0) * transpose(inverse(uTransform))).xyz);

    fPosition = vec3(finalPos * uTransform);
    fNormal = normalize(transformedNormal);
    fTangent  = normalize(transformedTangent);
    fTexCoord = aTexCoord;
    fMaterialLayer = aMaterialLayer;

    gl_Position = finalPos * uTransform * uView * uProjection;
}
#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;

out vec3 fPosition;
out vec2 fTexCoord;
out vec3 fNormal;
out vec3 fTangent;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * uTransform * uView * uProjection;
    fTexCoord = aTexCoord;
    fNormal = aNormal;
    fTangent = aTangent;
    fPosition = vec3(vec4(aPosition, 1.0) * uTransform);
}
#version 460 core
layout (location = 0) in vec3 aPosition;

out vec3 texCoords;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    vec4 pos = vec4(aPosition, 1.0) * uView * uProjection;
    gl_Position = pos.xyww;

    texCoords = vec3(aPosition.x, -aPosition.y, aPosition.z);
}
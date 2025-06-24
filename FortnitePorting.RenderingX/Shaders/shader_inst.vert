#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

layout (location = 2) in vec4 aInstanceMatrix0;
layout (location = 3) in vec4 aInstanceMatrix1;
layout (location = 4) in vec4 aInstanceMatrix2;
layout (location = 5) in vec4 aInstanceMatrix3;

out vec3 fPosition;
out vec3 fNormal;
out vec3 fDirection;
out vec3 fColor;

uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uDirection;

void main()
{
    vec4 finalPos = vec4(aPosition, 1.0);

    mat4 instanceMatrix = mat4(
        aInstanceMatrix0,
        aInstanceMatrix1,
        aInstanceMatrix2,
        aInstanceMatrix3
    );

    fNormal = normalize((vec4(aNormal, 0.0) * transpose(inverse(instanceMatrix))).xyz);

    fPosition = vec3(finalPos * instanceMatrix);
    fDirection = uDirection;

    int instanceId = gl_BaseInstance + gl_InstanceID;
    fColor = mix(vec3(0.25), vec3(1.0), vec3(
        float((instanceId * 97u) % 255u) / 255.0,
        float((instanceId * 59u) % 255u) / 255.0,
        float((instanceId * 31u) % 255u) / 255.0
    ));

    gl_Position = finalPos * instanceMatrix * uView * uProjection;
}
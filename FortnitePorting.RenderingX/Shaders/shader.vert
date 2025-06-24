#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

out vec3 fPosition;
out vec3 fNormal;
out vec3 fDirection;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uDirection;

void main()
{
    vec4 finalPos = vec4(aPosition, 1.0);
    vec3 transformedNormal = normalize((vec4(aNormal, 0.0) * transpose(inverse(uTransform))).xyz);
    float det = determinant(mat3(uTransform));

    fPosition = vec3(finalPos * uTransform);
    fNormal = normalize(det < 0 ? -transformedNormal : transformedNormal);
    fDirection = uDirection;

    gl_Position = finalPos * uTransform * uView * uProjection;
}
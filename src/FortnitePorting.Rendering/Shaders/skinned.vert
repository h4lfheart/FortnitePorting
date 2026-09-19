#version 460 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec3 aTangent;
layout (location = 3) in vec2 aTexCoord;
layout (location = 4) in float aMaterialLayer;
layout (location = 5) in vec4 aBoneIndices0;
layout (location = 6) in vec4 aBoneIndices1;
layout (location = 7) in vec4 aBoneWeights0;
layout (location = 8) in vec4 aBoneWeights1;

out vec3 fPosition;
out vec3 fNormal;
out vec3 fTangent;
out vec2 fTexCoord;
out float fMaterialLayer;
out float fMirrorFlip;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;

layout(std430, binding = 0) readonly buffer BoneBuffer {
    mat4 uBones[];
};

mat4 skinMatrix()
{
    float totalWeight =
        aBoneWeights0[0] + aBoneWeights0[1] + aBoneWeights0[2] + aBoneWeights0[3] +
        aBoneWeights1[0] + aBoneWeights1[1] + aBoneWeights1[2] + aBoneWeights1[3];

    if (totalWeight <= 0.0)
        return mat4(1.0);

    mat4 result = mat4(0.0);
    for (int i = 0; i < 4; i++)
    {
        float w0 = aBoneWeights0[i];
        if (w0 > 0.0)
            result += uBones[int(aBoneIndices0[i])] * w0;

        float w1 = aBoneWeights1[i];
        if (w1 > 0.0)
            result += uBones[int(aBoneIndices1[i])] * w1;
    }
    return result;
}

void main()
{
    mat4 skin = skinMatrix();

    vec4 finalPos = vec4(aPosition, 1.0) * skin;
    vec3 skinnedNormal = normalize((vec4(aNormal, 0.0) * skin).xyz);
    vec3 skinnedTangent = normalize((vec4(aTangent, 0.0) * skin).xyz);

    vec3 transformedNormal = normalize((vec4(skinnedNormal, 0.0) * transpose(inverse(uTransform))).xyz);
    vec3 transformedTangent = normalize((vec4(skinnedTangent, 0.0) * transpose(inverse(uTransform))).xyz);

    float det = determinant(mat3(uTransform));
    fMirrorFlip = (det < 0.0) ? -1.0 : 1.0;

    fPosition = vec3(finalPos * uTransform);
    fNormal = normalize(transformedNormal);
    fTangent = normalize(transformedTangent);
    fTexCoord = aTexCoord;
    fMaterialLayer = aMaterialLayer;

    gl_Position = finalPos * uTransform * uView * uProjection;
}

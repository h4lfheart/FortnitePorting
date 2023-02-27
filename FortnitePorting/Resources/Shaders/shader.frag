#version 460 core
#define PI 3.1415926535897932384626433832795
out vec4 FragColor;

in vec3 fPosition;
in vec2 fTexCoord;
in vec3 fNormal;
in vec3 fTangent;

uniform sampler2D diffuseTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;
uniform sampler2D maskTex;

vec3 samplerToColor(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}

vec3 calcNormals()
{
    vec3 normalTexture = samplerToColor(normalTex).rgb;
    vec3 normal = normalTexture * 2.0 - 1.0;

    vec3 tangentVec = normalize(fTangent);
    vec3 normalVec  = normalize(fNormal);
    vec3 binormalVec  = -normalize(cross(normalVec, tangentVec));
    mat3 combined = mat3(tangentVec, binormalVec, normalVec);

    return normalize(combined * normal);
}

vec3 calcLight()
{
    vec3 diffuse = samplerToColor(diffuseTex);
    return diffuse;
}

void main()
{
    vec3 result = calcLight();
    FragColor = vec4(result, 1.0);
}
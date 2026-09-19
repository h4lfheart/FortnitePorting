#version 460 core
out vec4 FragColor;

in vec3 fPosition;
in vec3 fNormal;
in vec3 fTangent;
in vec2 fTexCoord;
in float fMaterialLayer;
in float fMirrorFlip;

uniform bool useLayers;
uniform int maxLayer;

uniform sampler2D diffuse0;
uniform sampler2D diffuse1;
uniform sampler2D diffuse2;
uniform sampler2D diffuse3;

uniform sampler2D normal0;
uniform sampler2D normal1;
uniform sampler2D normal2;
uniform sampler2D normal3;

uniform sampler2D specular0;
uniform sampler2D specular1;
uniform sampler2D specular2;
uniform sampler2D specular3;

uniform vec3 fCameraDirection;
uniform vec3 fCameraPosition;

const float PI = 3.14159265359;

vec3 sampleTexture(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}

vec3 getDiffuse(int layer) {
    switch (layer) {
        case 0: return sampleTexture(diffuse0);
        case 1: return sampleTexture(diffuse1);
        case 2: return sampleTexture(diffuse2);
        case 3: return sampleTexture(diffuse3);
        default: return vec3(0.0);
    }
}

vec3 getNormal(int layer) {
    switch (layer) {
        case 0: return sampleTexture(normal0);
        case 1: return sampleTexture(normal1);
        case 2: return sampleTexture(normal2);
        case 3: return sampleTexture(normal3);
        default: return vec3(0.0);
    }
}

vec3 getSpecular(int layer) {
    switch (layer) {
        case 0: return sampleTexture(specular0);
        case 1: return sampleTexture(specular1);
        case 2: return sampleTexture(specular2);
        case 3: return sampleTexture(specular3);
        default: return vec3(0.0);
    }
}

vec3 calculateNormals(vec3 normalMap)
{
    normalMap.y = 1 - normalMap.y;
    normalMap = normalize(normalMap * 2.0 - 1.0);

    vec3 N = normalize(fNormal);
    vec3 T = normalize(fTangent);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T) * fMirrorFlip;

    mat3 TBN = mat3(T, B, N);
    return normalize(TBN * normalMap);
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

void main()
{
    int layer = useLayers ? int(fMaterialLayer) : 0;

    if (layer >= maxLayer) {
        layer = 0;
    }

    vec3 albedo = pow(getDiffuse(layer), vec3(2.2));
    vec3 normals = calculateNormals(getNormal(layer));
    vec3 specularMasks = getSpecular(layer);

    float specularStrength = specularMasks.r;
    float metallic = specularMasks.g;
    float roughness = specularMasks.b;
    roughness = clamp(roughness, 0.04, 1.0);

    vec3 N = normals;
    vec3 V = normalize(fCameraPosition - fPosition);

    vec3 V_horizontal = normalize(vec3(V.x, 0.0, V.z));
    vec3 cameraRight = normalize(cross(V_horizontal, vec3(0.0, 1.0, 0.0)));
    vec3 cameraUp = vec3(0.0, 1.0, 0.0);

    vec3 L1 = normalize(V_horizontal + cameraRight * 0.8 + cameraUp * 1.2);
    vec3 L2 = normalize(V_horizontal - cameraRight * 0.5 + cameraUp * 0.3);

    vec3 H1 = normalize(V + L1);
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    float NDF1 = DistributionGGX(N, H1, roughness);
    float G1 = GeometrySmith(N, V, L1, roughness);
    vec3 F1 = fresnelSchlick(max(dot(H1, V), 0.0), F0);

    vec3 kS1 = F1;
    vec3 kD1 = vec3(1.0) - kS1;
    kD1 *= 1.0 - metallic;

    vec3 numerator1 = NDF1 * G1 * F1;
    float denominator1 = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L1), 0.0) + 0.0001;
    vec3 specular1 = numerator1 / denominator1;

    float NdotL1 = max(dot(N, L1), 0.0);
    vec3 Lo1 = (kD1 * albedo / PI + specular1) * vec3(2.5) * NdotL1;

    float NdotL2 = max(dot(N, L2), 0.0);
    vec3 Lo2 = (albedo / PI) * vec3(0.4) * NdotL2;

    vec3 ambient = vec3(0.02) * albedo;

    vec3 color = ambient + Lo1 + Lo2;
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0/2.2));

    FragColor = vec4(color, 1.0);
    
}
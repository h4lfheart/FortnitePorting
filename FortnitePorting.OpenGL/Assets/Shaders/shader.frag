#version 460 core
#define PI 3.1415926535897932384626433832795
out vec4 FragColor;

in vec3 fPosition;
in vec2 fTexCoord;
in vec3 fNormal;
in vec3 fTangent;
in vec2 fExtraUV;

struct Parameters
{
    bool useLayers;
    
    sampler2D diffuse[6];
    sampler2D normal[6];
    sampler2D specular[6];
    sampler2D mask[6];
    sampler2D opacityMask[6];
};

uniform Parameters parameters;


uniform vec3 viewVector;

vec3 samplerToColor(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}

vec3 calcNormals(int layer)
{
    vec3 normal = samplerToColor(parameters.normal[layer]).rgb * 2.0 - 1.0;

    vec3 t = normalize(fTangent);
    vec3 n = normalize(fNormal);
    vec3 b = -normalize(cross(n, t));
    mat3 tbn = mat3(t, b, n);
    
    return normalize(tbn * normal);
}

vec3 blendSoftLight(vec3 base, vec3 blend)
{
    return mix(
        sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend),
        2.0 * base * blend + base * base * (1.0 - 2.0 * blend),
        step(base, vec3(0.5))
    );
}

float distributionGGX(float roughness, float nDotH)
{
    float numerator = pow(roughness, 2);
    float denominator = PI * pow((pow(nDotH, 2) * (numerator - 1)+1), 2);
    return (numerator * numerator) / denominator;
}

vec4 calcLight(int layer)
{
    vec3 normals = calcNormals(layer);
    
    // diffuse
    vec3 mask = samplerToColor(parameters.mask[layer]);
    float ambientOcclusion = mask.r;
    float cavity = mask.g;
    float skinMask = mask.b;
    
    vec3 diffuse = samplerToColor(parameters.diffuse[layer]);
    diffuse = mix(diffuse, diffuse * ambientOcclusion, 0.5);
    diffuse = mix(diffuse, blendSoftLight(diffuse, vec3(cavity)), 0.5);
    
    // direct light
    vec3 lightDirection = vec3(0, 1, 0.28);
    vec3 lightColor = vec3(0.854934, 0.613244, 0.475582) * 1.5;
    float directLight = dot(normals, lightDirection);
    vec3 directLightColor = max(directLight * lightColor, 0.0);

    // specular
    vec3 specularMasks = samplerToColor(parameters.specular[layer]);
    float specular = specularMasks.r;
    float metallic = specularMasks.g;
    float roughness = specularMasks.b;

    vec3 l = normalize(lightDirection);
    vec3 v = normalize(viewVector);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(normals, h), 0.0);
    float hDotV = max(dot(h, v), 0.0);
    float nDotL = max(dot(normals, l), 0.0);
    float nDotV = max(dot(normals, v), 0.0);

    float specularLight = distributionGGX(roughness, nDotH);
    vec3 specularColor = specularLight * lightColor * specular;

    vec3 ambientLightColor = vec3(0.227945, 0.40773, 0.462354);
    
    vec3 result = diffuse;
    result *= directLightColor + ambientLightColor + specularColor;
    result = mix(result, diffuse + vec3(1, 0, 0), skinMask * 0.15); // fake subsurface lol
    
    float alpha = 1.0;
    alpha = samplerToColor(parameters.opacityMask[layer]).r;
    if (alpha < 0.2) discard;
    
    return vec4(result, 1.0);
}

void main()
{
    float layerMask = fExtraUV.r;
    
    vec4 outColor = calcLight(0);
    if (parameters.useLayers)
    {
        outColor = mix(outColor, calcLight(1), layerMask > 1);
        outColor = mix(outColor, calcLight(2), layerMask > 2);
        outColor = mix(outColor, calcLight(3), layerMask > 3);
        outColor = mix(outColor, calcLight(4), layerMask > 4);
        outColor = mix(outColor, calcLight(5), layerMask > 5);
    }
    
    FragColor = outColor;
}
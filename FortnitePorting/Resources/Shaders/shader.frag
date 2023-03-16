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
uniform samplerCube environmentTex;
uniform vec3 viewVector;
uniform int isGlass;

vec3 samplerToColor(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}


vec3 calcNormals()
{
    vec3 normalTexture = samplerToColor(normalTex).rgb;
    vec3 normal = normalTexture * 2.0 - 1.0;

    vec3 tangentVec = normalize(fTangent);
    vec3 normalVec = normalize(fNormal);
    vec3 binormalVec = -normalize(cross(normalVec, tangentVec));
    mat3 combined = mat3(tangentVec, binormalVec, normalVec);
    

    return normalize(combined * normal);
}

vec3 irradiance(vec3 normals)
{
    vec3 irradiance = vec3(0.0);

    vec3 up    = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(up, normals));
    up         = normalize(cross(normals, right));

    float sampleDelta = 0.25;
    float nrSamples = 0.0;
    for(float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for(float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            // spherical to cartesian (in tangent space)
            vec3 tangentSample = vec3(sin(theta) * cos(phi),  sin(theta) * sin(phi), cos(theta));
            // tangent space to world
            vec3 sampleVec = tangentSample.x * right + tangentSample.y * up + tangentSample.z * normals;

            irradiance += texture(environmentTex, sampleVec).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    return PI * irradiance * (1.0 / float(nrSamples));
}

vec3 calcReflection(vec3 normals)
{
    vec3 I = normalize(viewVector);
    vec3 R = reflect(I, normalize(normals));
    return texture(environmentTex, R).rgb;
}

vec3 fresnelSchlick(vec3 diffuseColor, float metallic, float hDotV)
{
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, diffuseColor, metallic);
    return F0 + (1.0 - F0) * pow(clamp(1.0 - hDotV, 0, 1), 5);
}

float distributionGGX(float roughness, float nDotH)
{
    float numerator = pow(roughness, 2);
    float denominator = PI * pow((pow(nDotH, 2) * (numerator - 1)+1), 2);
    return (numerator * numerator) / denominator;
}

vec3 blendSoftLight(vec3 base, vec3 blend) 
{
    return mix(
    sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend),
    2.0 * base * blend + base * base * (1.0 - 2.0 * blend),
    step(base, vec3(0.5))
    );
}

vec3 calcLight()
{
    // textures
    vec3 diffuse = samplerToColor(diffuseTex);
    
    vec3 normal = calcNormals();
    
    vec3 specularMasks = samplerToColor(specularTex);
    float specular = specularMasks.r;
    float metallic = specularMasks.g;
    float roughness = specularMasks.b;
    
    vec3 mask = samplerToColor(maskTex);
    float ambientOcclusion = mask.r;
    float cavity = mask.g;
    float skinMask = mask.b;

    vec3 environment = calcReflection(normal);
    
    // Mask Stuff
    diffuse = mix(diffuse, diffuse * ambientOcclusion, 0.25);
    diffuse = mix(diffuse, blendSoftLight(diffuse, vec3(cavity)), 0.25);
    
    // light
    vec3 lightDirection = vec3(0.323, 0.456, 0.006);
    vec3 lightColor = vec3(0.939, 0.831, 0.610) * 3;
    
    // ambient
    vec3 ambientColor = irradiance(normal);
    
    // diffuse light
    float diffuseLight = max(dot(normal, lightDirection), 0);
    vec3 diffuseColor = diffuseLight * lightColor;

    // specular light
    vec3 l = normalize(lightDirection);
    vec3 v = normalize(viewVector);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(normal, h), 0.0);
    float hDotV = max(dot(h, v), 0.0);
    float nDotL = max(dot(normal, l), 0.0);
    float nDotV = max(dot(normal, v), 0.0);

    float specularLight = distributionGGX(roughness, nDotH);
    vec3 specularColor = specularLight * lightColor * specular;
    
    vec3 result = diffuse;
    result *= diffuseColor + specularColor + ambientColor;
    result = mix(result, diffuse, skinMask * 0.25);
    
    return result;
}

void main()
{
    if (isGlass == 0)
    {
        FragColor = vec4(calcLight(), 1.0);
    }
    else 
    {
        FragColor = vec4(calcReflection(calcNormals()), 0.5);
    }
}
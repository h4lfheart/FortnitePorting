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

vec3 calcLight()
{
    vec3 diffuse = samplerToColor(diffuseTex);
    vec3 normal = calcNormals();
    vec3 mask = samplerToColor(maskTex);


    //vec3 ambientColor = irradiance(normal);
    
    vec3 result = mix(diffuse, diffuse*mask.r, 0.5f);
    //result *= ambientColor;
    
    return result;
}

void main()
{
    vec3 result = calcLight();
    FragColor = vec4(result, 1.0);
}
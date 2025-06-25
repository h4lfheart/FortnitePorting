#version 460 core
out vec4 FragColor;

struct Parameters
{
    bool useLayers;

    sampler2D diffuse[4];
    sampler2D normal[4];
    sampler2D specular[4];
};

uniform Parameters parameters;

uniform vec3 fCameraDirection;
uniform vec3 fCameraPosition;

in vec3 fPosition;
in vec3 fNormal;
in vec3 fTangent;
in vec2 fTexCoord;
in float fMaterialLayer;

vec3 samplerToColor(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}

vec3 calcNormals(int layer)
{
    vec3 normalMap = texture(parameters.normal[layer], fTexCoord).rgb;
    
    float temp = normalMap.r;
    normalMap.r = normalMap.b;
    normalMap.b = temp;
    
    normalMap.y = 1 - normalMap.y;
    
    normalMap = normalize(normalMap * 2.0 - 1.0);

    vec3 N = normalize(fNormal);
    vec3 T = normalize(fTangent);
    vec3 B = normalize(cross(fNormal, fTangent));

    mat3 TBN = mat3(T, B, N);
    
    return normalize(TBN * normalMap);
}

void main()
{
    int layer = parameters.useLayers ? int(fMaterialLayer) : 0;
    
    
    vec3 lightDir = normalize(vec3(fCameraPosition - fPosition) * 250);
    
    vec3 normals = calcNormals(layer);
    
    float diffuseLight = max(dot(normals, lightDir), 0.0);
    
    vec3 baseColor = samplerToColor(parameters.diffuse[layer]);
    vec3 diffuse = baseColor * diffuseLight;
    
    vec3 specularMasks = samplerToColor(parameters.specular[layer]);

    float shininess = pow(2.0, (1.0 - specularMasks.b) * 10.0);
    
    vec3 viewDir = normalize(fCameraPosition - fPosition);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    float specLight = pow(max(0.0, dot(halfwayDir, normals)), shininess);
    
    vec3 specular = specularMasks.r * baseColor * specLight;
    
    vec3 ambientColor = vec3(0.2, 0.2, 0.2);
    vec3 ambient = baseColor * ambientColor;

    vec3 finalColor = diffuse + specular + ambient;

    FragColor = vec4(vec3(finalColor), 1.0);
}
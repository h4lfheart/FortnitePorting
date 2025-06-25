#version 460 core
out vec4 FragColor;

uniform bool useLayers;

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

in vec3 fPosition;
in vec3 fNormal;
in vec3 fTangent;
in vec2 fTexCoord;
in float fMaterialLayer;

vec3 sampleTexture(sampler2D tex)
{
    return texture(tex, fTexCoord).rgb;
}

vec3 getDiffuse(int layer) {

    switch (layer) {
        case 0:
            return sampleTexture(diffuse0);
        case 1:
            return sampleTexture(diffuse1);
        case 2:
            return sampleTexture(diffuse2);
        case 3:
            return sampleTexture(diffuse3);
        default:
            return vec3(0.0);
    }
}

vec3 getNormal(int layer) {

    switch (layer) {
        case 0:
            return sampleTexture(normal0);
        case 1:
            return sampleTexture(normal1);
        case 2:
            return sampleTexture(normal2);
        case 3:
            return sampleTexture(normal3);
        default:
            return vec3(0.0);
    }
}

vec3 getSpecular(int layer) {
    switch (layer) {
        case 0:
            return sampleTexture(specular0);
        case 1:
            return sampleTexture(specular1);
        case 2:
            return sampleTexture(specular2);
        case 3:
            return sampleTexture(specular3);
        default:
            return vec3(0.0);
    }
}

vec3 calculateNormals(vec3 normalMap)
{
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
    int layer = useLayers ? int(fMaterialLayer) : 0;
    
    vec3 lightDir = normalize(vec3(fCameraPosition - fPosition) * 250);
    
    vec3 normals = calculateNormals(getNormal(layer));
    
    float diffuseLight = max(dot(normals, lightDir), 0.0);
    
    vec3 baseColor = getDiffuse(layer);
    vec3 diffuse = baseColor * diffuseLight;
    
    vec3 specularMasks = getSpecular(layer);

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
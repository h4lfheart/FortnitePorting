#version 460 core
out vec4 FragColor;

in vec3 fPosition;
in vec3 fNormal;

uniform vec3 fCameraDirection;
uniform vec3 fCameraPosition;

void main()
{
    vec3 lightDir = normalize(vec3(fCameraPosition - fPosition) * 250);
    float diffuseLight = max(dot(fNormal, lightDir), 0.0);
    
    FragColor = vec4(vec3(diffuseLight), 1);
}
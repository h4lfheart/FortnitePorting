#version 460 core
out vec4 FragColor;

in vec3 fPosition;
in vec3 fNormal;
in vec3 fDirection;

void main()
{
    vec3 lightDir = -fDirection;
    
    vec3 N = normalize(fNormal);
    vec3 L = normalize(lightDir);
    
    float cosTheta = max(dot(N, L), 0.0);

    vec3 baseColor = vec3(0.7, 0.7, 0.7);
    vec3 diffuse = baseColor * cosTheta;
    
    vec3 ambientColor = vec3(0.2, 0.2, 0.2);
    vec3 ambient = baseColor * ambientColor;

    vec3 finalColor = diffuse + ambient;

    FragColor = vec4(finalColor, 1.0);
}
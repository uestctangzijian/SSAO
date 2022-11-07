#version 330 core
out float FragColor;

in vec2 TexCoords;

uniform sampler2D ssaoInput;

uniform int noiseSize;
uniform bool blurEnable;

void main()
{
    if (!blurEnable)
    {
        FragColor = texture(ssaoInput, TexCoords).r;
        return;
    }
    vec2 texelSize = 1.0 / vec2(textureSize(ssaoInput, 0));
    float result = 0.0;
    for (int x = - noiseSize/2; x < noiseSize/2; x++)
    {
        for (int y = -noiseSize/2; y < noiseSize/2; y++)
        {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(ssaoInput, TexCoords + offset).r;
        }
    }
    FragColor = result / (noiseSize * noiseSize);
}
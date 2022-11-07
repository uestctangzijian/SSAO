#version 330 core
out float FragColor;

in vec2 TexCoords;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D texNoise;

uniform vec3 samples[64];

uniform int SCR_WIDTH;
uniform int SCR_HEIGHT;

// parameters (you'd probably want to use them as uniforms to more easily tweak the effect)
uniform int kernelSize;
uniform float radius;
uniform float bias;
uniform int power;
uniform int noiseSize;
uniform bool rangeCheckEnable;

uniform mat4 projection;

void main()
{
    // tile noise texture over screen based on screen dimensions divided by noise size
    vec2 noiseScale = vec2(SCR_WIDTH/noiseSize, SCR_HEIGHT/noiseSize); 

    vec3 fragPos = texture(gPosition, TexCoords).xyz; // ���������ͼ�ռ��λ��
    vec3 normal = normalize(texture(gNormal, TexCoords).rgb); // ���������ͼ�ռ�ķ�����
    vec3 randomVec = normalize(texture(texNoise, TexCoords * noiseScale).xyz); // �����ת���� ������ռ�
    // ����TBN������󣬴����߿ռ� �� ��ͼ�ռ�
    // �����https://learnopengl.com/Advanced-Lighting/Normal-Mapping
    // Gram-Schmidt������
    vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
    vec3 bitangent = cross(normal, tangent);
    mat3 TBN = mat3(tangent, bitangent, normal);
   
    float occlusion = 0.0;
    for(int i = 0; i < kernelSize; ++i)
    {
        vec3 samplePos = TBN * samples[i]; // ����������߿ռ�䵽��ͼ�ռ䣬��ʱ�������λ��
        samplePos = fragPos + samplePos * radius; // ������ ����ͼ�ռ� �ľ���λ��
        
        vec4 offset = vec4(samplePos, 1.0);
        offset = projection * offset; // ������ ����ͼ�ռ�ͶӰ�����ÿռ�[-1, 1]
        offset.xyz /= offset.w; // ͸�ӳ���
        offset.xyz = offset.xyz * 0.5 + 0.5; // ������ ת����NDC
        
        float sampleDepth = texture(gPosition, offset.xy).z; // ��ȡ�������xy��z-buffer�ж�Ӧ�����ֵ
        
        // �������ͼ�ռ��� �����������Ƿ��� �����Ĳ����뾶�ڣ�����ǲŶ��ڱ������й���
        float rangeCheck = rangeCheckEnable ? smoothstep(0.0, 1.0, radius / abs(fragPos.z - sampleDepth)) : 1.0f;
        // �ж������������Ƿ���ڸõ�����ͼ�ռ����ȣ�bias �Ǳ��룬�������Ӿ��ϵ���SSAO��Ч��
        occlusion += (sampleDepth >= samplePos.z + bias ? 1.0 : 0.0) * rangeCheck;           
    }
    occlusion = 1.0 - (occlusion / kernelSize);
    
    FragColor = pow(occlusion, power);
}
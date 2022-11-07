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

    vec3 fragPos = texture(gPosition, TexCoords).xyz; // 计算点在视图空间的位置
    vec3 normal = normalize(texture(gNormal, TexCoords).rgb); // 计算点在视图空间的法向量
    vec3 randomVec = normalize(texture(texNoise, TexCoords * noiseScale).xyz); // 随机旋转向量 在切向空间
    // 构造TBN变基矩阵，从切线空间 到 视图空间
    // 详见：https://learnopengl.com/Advanced-Lighting/Normal-Mapping
    // Gram-Schmidt正交化
    vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
    vec3 bitangent = cross(normal, tangent);
    mat3 TBN = mat3(tangent, bitangent, normal);
   
    float occlusion = 0.0;
    for(int i = 0; i < kernelSize; ++i)
    {
        vec3 samplePos = TBN * samples[i]; // 样本点从切线空间变到视图空间，此时还是相对位置
        samplePos = fragPos + samplePos * radius; // 样本点 在视图空间 的绝对位置
        
        vec4 offset = vec4(samplePos, 1.0);
        offset = projection * offset; // 样本点 从视图空间投影到剪裁空间[-1, 1]
        offset.xyz /= offset.w; // 透视除法
        offset.xyz = offset.xyz * 0.5 + 0.5; // 样本点 转换成NDC
        
        float sampleDepth = texture(gPosition, offset.xy).z; // 获取样本点的xy在z-buffer中对应的深度值
        
        // 检查在视图空间中 样本点的深度是否在 计算点的采样半径内，如果是才对遮蔽因子有贡献
        float rangeCheck = rangeCheckEnable ? smoothstep(0.0, 1.0, radius / abs(fragPos.z - sampleDepth)) : 1.0f;
        // 判断样本点的深度是否大于该点在视图空间的深度，bias 非必须，可以在视觉上调整SSAO的效果
        occlusion += (sampleDepth >= samplePos.z + bias ? 1.0 : 0.0) * rangeCheck;           
    }
    occlusion = 1.0 - (occlusion / kernelSize);
    
    FragColor = pow(occlusion, power);
}
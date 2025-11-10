sampler screenTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float opacity;
float intensity;
float2 screenPosition;
float2 screenSize;
float2 focusPosition;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(screenTexture, coords);
    
    // More intense effects based on intensity parameter
    float2 center = float2(0.5, 0.5);
    float dist = distance(coords, center);
    
    // Pulsing vignette - more intense
    float pulse = sin(globalTime * 3.0) * 0.5 + 0.5;
    float vignette = 1.0 - smoothstep(0.2, 1.4, dist * (1.0 + intensity * 0.3));
    vignette = lerp(0.5, 1.0, vignette * (0.5 + pulse * 0.5) * intensity);
    
    // Extreme chromatic aberration
    float aberrationStrength = 0.006 * opacity * intensity * (1.0 + pulse * 0.8);
    float2 direction = normalize(coords - center);
    
    float r = tex2D(screenTexture, coords + direction * aberrationStrength * 1.5).r;
    float g = tex2D(screenTexture, coords).g;
    float b = tex2D(screenTexture, coords - direction * aberrationStrength * 1.5).b;
    
    color = float4(r, g, b, color.a);
    
    // Phase-shifting color tint
    float3 color1 = float3(0.6, 0.1, 0.9); // Purple
    float3 color2 = float3(0.1, 0.9, 0.9); // Cyan
    float3 color3 = float3(0.9, 0.1, 0.3); // Red
    
    float colorPhase = frac(globalTime * 0.3 + intensity * 0.2);
    float3 chaosColor = lerp(color1, color2, smoothstep(0.0, 0.5, colorPhase));
    chaosColor = lerp(chaosColor, color3, smoothstep(0.5, 1.0, colorPhase));
    
    color.rgb = lerp(color.rgb, color.rgb * chaosColor, 0.25 * opacity * intensity);
    
    // Intense scanlines
    float scanline = sin(coords.y * screenSize.y * 3.0 + globalTime * 10.0 * intensity) * 0.5 + 0.5;
    color.rgb *= 1.0 - scanline * 0.1 * opacity * intensity;
    
    // Wave distortion
    float2 waveOffset = float2(
        sin(coords.y * 20.0 + globalTime * 5.0) * 0.01,
        cos(coords.x * 20.0 + globalTime * 5.0) * 0.01
    ) * opacity * intensity;
    
    // Noise distortion - more chaotic
    float2 noiseCoords = coords * 4.0 + float2(globalTime * 0.2, globalTime * 0.15);
    float noise = tex2D(noiseTexture, noiseCoords).r;
    
    float2 distortion = (noise - 0.5) * 0.02 * opacity * pulse * intensity;
    distortion += waveOffset;
    
    float4 distortedColor = tex2D(screenTexture, coords + distortion);
    color = lerp(color, distortedColor, 0.5 * opacity * intensity);
    
    // Apply vignette
    color.rgb *= vignette;
    
    // Intense edge glow with color cycling
    float edgeGlow = pow(dist, 1.5) * pulse * intensity;
    color.rgb += chaosColor * edgeGlow * 0.2 * opacity;
    
    // Add screen-space chaos lines
    float chaosLine = frac(sin(dot(coords, float2(12.9898 + globalTime, 78.233))) * 43758.5453);
    color.rgb += chaosLine * 0.05 * pulse * opacity * intensity;
    
    // Conditional pixelation effect at high intensity
    float pixelationAmount = saturate((intensity - 1.5) / 0.5);
    if (pixelationAmount > 0.0)
    {
        float pixelSize = 4.0 * pixelationAmount;
        float2 pixelCoords = floor(coords * screenSize / pixelSize) * pixelSize / screenSize;
        float4 pixelatedColor = tex2D(screenTexture, pixelCoords);
        color = lerp(color, pixelatedColor, 0.3 * pixelationAmount);
    }
    
    return color * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
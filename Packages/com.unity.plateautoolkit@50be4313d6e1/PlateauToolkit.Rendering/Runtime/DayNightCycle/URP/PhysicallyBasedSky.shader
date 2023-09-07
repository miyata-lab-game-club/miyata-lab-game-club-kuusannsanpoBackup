Shader "Skybox/URPPhysicallyBasedSky"
{
    Properties
    {
        _Brightness ("Exposure", range(0, 30)) = 20 
        _Samples ("Scattering Samples", Range(2, 64)) = 16
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off
        Tags { "Queue" = "Background" "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }


        Pass
        {
            PackageRequirements
            {
                "com.unity.render-pipelines.universal": "10.0"
            }
            HLSLPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION; // clipspace
                float3 worldPos : TEXCOORD1;
            };

            float _Brightness;
            float _SkyMultiplier;
            float _Cloud;
            uint _Samples;

            static const float EARTH_RADIUS = 6371000;
            static const float3 EARTH_CENTER = float3(0, -6371000, 0);
            static const float ATMO_HEIGHT = 100000;
            static const float MIE = 0.000003996;
            static const float3 RAYLEIGH = float3(0.000005802, 0.000013558, 0.0000331);
            static const float3 OZONE = float3(0.00000065, 0.000001881, 0.000000085);

            float2 EarthIntersect (float3 rayPos, float3 rayDir, float3 center, float radius)
            {
                rayPos -= center;
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(rayPos, rayDir);
                float c = dot(rayPos, rayPos) - (radius * radius);
                float d = b * b - 4 * a * c;
                if (d < 0)
                {
                    return -1;
                }
                else
                {
                    d = sqrt(d);
                    return float2(-b - d, -b + d) / (2 * a);
                }
            }

            float3 Density (float h)
            {
                float rayleigh = exp(-max(0, h / (ATMO_HEIGHT * 0.08)));
                float mie = exp(-max(0, h / (ATMO_HEIGHT * 0.012)));
                float ozone = max(0, 1 - abs(h - 25000.0) / 15000.0);

                return float3(rayleigh, mie, ozone);
            }

            float3 ViewDepth (float3 rayPos, float3 rayDir)
            {
                float2 intersection = EarthIntersect(rayPos, rayDir, EARTH_CENTER, EARTH_RADIUS + ATMO_HEIGHT);
                float  rayDist    = intersection.y;
                float  stepSize     = rayDist / (_Samples / 8);
                float3 vDepth = 0;

                for (int i = 0; i < (_Samples / 8); i++)
                {
                    float3 pos = rayPos + rayDir * (i + 0.5) * stepSize;
                    float  h   = distance(pos, EARTH_CENTER) - EARTH_RADIUS;
                    vDepth += Density(h) * stepSize;
                }

                return vDepth;
            }

            float3 Scatter (float3 vDepth)
            {
                return exp(-(vDepth.x * RAYLEIGH + vDepth.y * MIE * 1.1 + vDepth.z * OZONE)); 
            }

            float3 RayMarch (float3 rayPos, float3 rayDir, float rayDist)
            {
                Light light = GetMainLight();
                float rayHeight = distance(rayPos, EARTH_CENTER) - EARTH_RADIUS;
                float sampleDistributionExponent = 1 + saturate(1 - rayHeight / ATMO_HEIGHT) * 8;
                float2 intersection = EarthIntersect(rayPos, rayDir, EARTH_CENTER, EARTH_RADIUS + ATMO_HEIGHT);

                rayDist = min(rayDist, intersection.y);

                if (intersection.x > 0)
                {
                    rayPos += rayDir * intersection.x;
                    rayDist -= intersection.x;
                }
                
                float RdotL = dot(rayDir, light.direction);
                float3 vDepth = 0;
                float3 rayleigh = 0;
                float3 mie = 0;
                float rayDelta = 0;

                for (int i = 0; i < _Samples; i++)
                {
                    float  rayLen = pow(abs((float)i / _Samples), sampleDistributionExponent) * rayDist;
                    float  stepSize = (rayLen - rayDelta);

                    float3 pos = rayPos + rayDir * rayLen;
                    float  h   = distance(pos, EARTH_CENTER) - EARTH_RADIUS; 
                    float3 d  = Density(h);
                    vDepth += d * stepSize;
                    float3 vTransmit = Scatter(vDepth);
                    float3 vDepthLight  = ViewDepth(pos, light.direction);
                    float3 lTransmit = Scatter(vDepthLight);
                    float pRayleigh = 3 * (1 + RdotL*RdotL) / (16 * 3.1415);
                    float k = 1.55 * 0.85 - 0.55 * 0.85 * 0.85 *0.85;
                    float kRdotL = k*RdotL;
                    float pMie = (1 - k*k) / ((4 * 3.1415) * (1-kRdotL) * (1-kRdotL));

                    rayleigh += vTransmit * lTransmit * pRayleigh * d.x * stepSize;
                    mie      += vTransmit * lTransmit * pMie * d.y * stepSize;
                    rayDelta = rayLen;
                }

                float3 multiplier = (_Brightness * _SkyMultiplier);
                multiplier *= light.color;
                return (rayleigh * RAYLEIGH + mie * MIE) * multiplier; 
            }

            v2f vert (appdata v)        
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            { 
                float3 scattering = 0;
                float3 rayPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.worldPos);
                float scaleFactor = 100000 / max(_ProjectionParams.z, 0.00001);
                float rayDist = distance(rayPos, i.worldPos) * scaleFactor;
                
                scattering = RayMarch(rayPos, rayDir, rayDist);
                float3 monochrome = dot(scattering, float3(0.299, 0.587, 0.114));
                scattering = lerp(scattering, monochrome, _Cloud);

                return float4(scattering, 1); 
            }
            ENDHLSL
        }
    }
}
Shader "ScreenTransition/DoorSurface"
{
    Properties { _BaseColor ("Color", Color) = (0.15,0.2,0.19,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz); o.uv = v.uv; return o;
            }
            half4 Frag(Varyings v) : SV_Target
            {
                half3 normal = normalize(v.normalWS);
                half3 lightDirection = normalize(half3(-0.4,0.7,-1));
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(v.positionWS);
                half shade = 0.22 + 0.78 * saturate(dot(normal, lightDirection));
                half highlight = pow(saturate(dot(normal, normalize(lightDirection + viewDirection))), 48);
                half rim = pow(1 - saturate(dot(normal, viewDirection)), 3);
                half grain = 0.97 + 0.03 * sin(v.uv.y * 950);
                return half4(_BaseColor.rgb * shade * grain + highlight * 0.4 + rim * 0.06, 1);
            }
            ENDHLSL
        }
    }
}

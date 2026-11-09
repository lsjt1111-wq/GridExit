Shader "ScreenTransition/ChestSurface"
{
    Properties
    {
        _BaseMap ("Chest texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 normal = normalize(input.normalWS) * IS_FRONT_VFACE(face, 1, -1);
                half shade = 0.45 + 0.55 * saturate(dot(normal, normalize(half3(-0.4,0.8,-0.6))));
                return half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb * shade, 1);
            }
            ENDHLSL
        }
    }
}

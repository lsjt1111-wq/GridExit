Shader "GridTutorial/HitFlash"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.25
        [HDR] _FlashColor("Flash Color", Color) = (1,1,1,1)
        _FlashAmount("Flash Amount", Range(0,1)) = 0
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _FlashColor;
                half _NormalStrength, _Metallic, _Smoothness, _FlashAmount;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fog : TEXCOORD4;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.tangentWS = half4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv), _NormalStrength);
                half3 n = normalize(i.normalWS);
                half3 t = normalize(i.tangentWS.xyz);
                half3 b = cross(n, t) * i.tangentWS.w;
                InputData input = (InputData)0;
                input.positionWS = i.positionWS;
                input.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(t,b,n)));
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI = SampleSH(input.normalWS);
                input.vertexLighting = VertexLighting(i.positionWS, input.normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask = half4(1,1,1,1);
                half flash = saturate(_FlashAmount);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb, _FlashColor.rgb, flash);
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = normalTS;
                surface.occlusion = 1;
                surface.alpha = 1;
                surface.emission = _FlashColor.rgb * flash * 2;
                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, i.fog);
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack Off
}

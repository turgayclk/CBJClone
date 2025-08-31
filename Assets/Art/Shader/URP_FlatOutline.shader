Shader "Custom/URP_FlatOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-1" }
        LOD 100

        Pass
        {
            Name "FlatOutline"
            Cull Off
            ZWrite Off   // blo�un i�ine g�m�lmesin
            ZTest Always // hep g�r�ns�n

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _OutlineColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

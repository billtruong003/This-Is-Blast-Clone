Shader "Hidden/ImageToVoxel/VoxelVertexColor"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 color      : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(float3(0.3, 1.0, 0.2));
                float ndl = saturate(dot(normalize(i.worldNormal), lightDir));
                float lighting = lerp(0.35, 1.0, ndl);
                return float4(i.color.rgb * lighting, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Particles/Standard Unlit"
}

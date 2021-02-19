// quick vertex extrude shader test for nasadem data

Shader "Unlit/Nasadem/HeightmapDecode"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // nasadem.xyz
            // The API returns a PNG image, where each pixel encodes the height at that point. The formula to convert into meters is:
            // let h = 256 * r + g - 32768;
            // where r is the pixel value in the red channel, and g that of the green channel

            float Decode(float2 tex)
            {
                return 256 * (tex.r*256) + (tex.g*256) - 32768;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // read texture
                float2 tex = tex2Dlod(_MainTex, float4(v.uv.xy, 0, 0)).rg;

                // extrude
                float height = Decode(tex);
                v.vertex.y += height;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

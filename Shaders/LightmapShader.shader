Shader "Custom/LightmapShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Lightmap ("Lightmap", 2D) = "" {}
        _EmissionMap ("Emission Texture", 2D) = "" {}
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1.0
        _Brightness("Brightness", Range(0, 10)) = 4.0
    }
    SubShader
    {     
        Pass 
        { 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog       
            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _Lightmap;
            float4 _Lightmap_ST;

            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            uniform float _EmissionIntensity;

            uniform float _Brightness;


            // Vertex Shader
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.lightmapUV = TRANSFORM_TEX(v.lightmapUV, _Lightmap);

                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }
            //


            // Fragment Shader
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 lightmap = tex2D(_Lightmap, i.lightmapUV);
                fixed4 emissionTex = tex2D(_EmissionMap, i.uv)  * _EmissionIntensity;
                float brightness = _Brightness;

                // Add color
                col.rgb *= _Color;

                // Add emission map
                col.rgb += emissionTex.rgb * col.rgb;

                // Add lightmap
                col.rgb = col.rgb * DecodeLightmap(lightmap);

                // Add color brightness
                col.rgb += col.rgb * brightness;
            
              
                UNITY_APPLY_FOG(i.fogCoord, col);
               
                return col;
            }       
            //

            ENDCG
        }
    }
    Fallback "Diffuse"
}

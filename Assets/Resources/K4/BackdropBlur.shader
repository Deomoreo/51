Shader "Hidden/Project51/K4/BackdropBlur"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Direction;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 stepUV = _MainTex_TexelSize.xy * _Direction.xy;
                fixed4 color = tex2D(_MainTex, i.uv) * 0.2270270270;
                color += tex2D(_MainTex, i.uv + stepUV * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, i.uv - stepUV * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, i.uv + stepUV * 3.2307692308) * 0.0702702703;
                color += tex2D(_MainTex, i.uv - stepUV * 3.2307692308) * 0.0702702703;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}

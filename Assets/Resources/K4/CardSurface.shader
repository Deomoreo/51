Shader "Project51/K4/CardSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _K4Sweep ("Sweep", Float) = -1
        _K4Dissolve ("Dissolve", Range(0,1)) = 0
        _K4Holographic ("Holographic", Range(0,1)) = 0
        _K4Bounds ("Local Bounds", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _K4Sweep, _K4Dissolve, _K4Holographic;
            float4 _K4Bounds;
            struct card_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            card_v2f vert(appdata_t input)
            {
                v2f sprite = SpriteVert(input);
                card_v2f output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = sprite.vertex;
                output.color = sprite.color;
                output.texcoord = sprite.texcoord;
                // Local coordinates keep the decoration independent of atlas packing and UV rotation.
                output.localUV = (input.vertex.xy - _K4Bounds.xy) * _K4Bounds.zw;
                return output;
            }
            fixed4 frag(card_v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                float2 uv = input.localUV;
                float stripe = uv.x + uv.y * 0.32;
                float sweep = (1 - smoothstep(0.0, 0.12, abs(stripe - lerp(-0.2, 1.6, _K4Sweep)))) * step(0, _K4Sweep);
                float noise = sin(uv.x * 31 + sin(uv.y * 27)) * 0.045;
                float threshold = uv.y * 0.88 + 0.06 + noise;
                float coverage = 1 - smoothstep(threshold - 0.035, threshold + 0.035, _K4Dissolve);
                // Smooth edge must never trim the original sprite at rest or survive full dissolve.
                coverage = _K4Dissolve <= 0 ? 1 : (_K4Dissolve >= 1 ? 0 : coverage);
                float edge = (1 - smoothstep(0, 0.055, abs(threshold - _K4Dissolve))) * step(0.001, _K4Dissolve);
                color.rgb += sweep * 0.16 + edge * float3(0.45, 0.16, 0.025);
                color.rgb += _K4Holographic * 0.08 * (0.5 + 0.5 * cos(6.28318 * (stripe + _Time.y * 0.12 + float3(0, 0.33, 0.67))));
                color.a *= coverage;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}

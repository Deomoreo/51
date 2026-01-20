Shader "Sprites/HoloFoil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FoilTex ("Foil Texture (RGBA)", 2D) = "white" {}
        _FoilScale ("Foil Scale", Float) = 3
        _FoilIntensity ("Foil Intensity", Range(0,2)) = 0.45
        _FoilAlpha ("Foil Alpha", Range(0,1)) = 0.65

        _RainbowIntensity ("Rainbow Intensity", Range(0,2)) = 0.35
        _RainbowSpeed ("Rainbow Speed", Range(0,5)) = 0.9

        _RimIntensity ("Rim Intensity", Range(0,2)) = 0.18
        _RimWidth ("Rim Width", Range(0.01,1)) = 0.32

        _Tilt ("Tilt (From Script)", Range(-1,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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

            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            sampler2D _FoilTex;
            float4 _FoilTex_ST;

            float _FoilScale;
            float _FoilIntensity;
            float _FoilAlpha;
            float _RainbowIntensity;
            float _RainbowSpeed;
            float _RimIntensity;
            float _RimWidth;
            float _Tilt;

            struct holo_appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct holo_v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uvFoil   : TEXCOORD1;
            };

            holo_v2f vert(holo_appdata IN)
            {
                holo_v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;

                // Foil UV: scaled + animated drift + slight shift by tilt
                float2 uv = IN.texcoord;
                float t = _Time.y;

                float2 drift = float2(0.07, 0.11) * t;
                float2 tiltShift = float2(_Tilt * 0.08, _Tilt * -0.05);

                OUT.uvFoil = uv * _FoilScale + drift + tiltShift;
                return OUT;
            }

            // Cheap rainbow gradient from a single phase value
            fixed3 Rainbow(float phase)
            {
                fixed3 c;
                c.r = 0.5 + 0.5 * sin(phase + 0.0);
                c.g = 0.5 + 0.5 * sin(phase + 2.094395102f);
                c.b = 0.5 + 0.5 * sin(phase + 4.188790204f);
                return c;
            }

            fixed4 frag(holo_v2f IN) : SV_Target
            {
                fixed4 baseCol = SampleSpriteTexture(IN.texcoord) * IN.color;

                // If sprite is transparent, keep it transparent.
                if (baseCol.a <= 0.001)
                    return 0;

                // Foil texture sample
                fixed4 foil = tex2D(_FoilTex, IN.uvFoil);

                // Use foil luminance as mask
                fixed foilMask = dot(foil.rgb, fixed3(0.299, 0.587, 0.114));

                // Rim based on distance to center (fake fresnel in 2D)
                float2 uv = IN.texcoord;
                float2 centered = abs(uv * 2.0 - 1.0);
                float edge = max(centered.x, centered.y); // 0 center .. 1 edges

                // Edge ramp. Width controls how far rim goes inward.
                float rim = smoothstep(1.0 - _RimWidth, 1.0, edge);

                // Rainbow phase reacts to tilt and time
                float phase = (uv.x * 6.28318) + (uv.y * 3.14159) + (_Time.y * _RainbowSpeed) + (_Tilt * 2.2);
                fixed3 rainbow = Rainbow(phase);

                // Foil shimmer intensity also reacts to tilt so wobble changes highlights.
                float tiltBoost = 1.0 + abs(_Tilt) * 0.55;

                // Keep contributions subtle.
                fixed holoMask = saturate(foilMask);
                fixed3 holo = (rainbow * _RainbowIntensity) * (holoMask * _FoilIntensity) * tiltBoost;
                fixed3 rimCol = rainbow * (_RimIntensity * rim) * tiltBoost;

                fixed3 add = (holo + rimCol) * _FoilAlpha;

                // Slightly remap add so it doesn't blow out whites.
                add *= (1.0 - baseCol.a * 0.0); // placeholder, keep stable

                fixed3 outRgb = baseCol.rgb + add;

                return fixed4(outRgb, baseCol.a);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

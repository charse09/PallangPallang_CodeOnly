Shader "UI/WaveCooldown"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _FillAmount ("Fill Amount", Range(0,1)) = 1.0
        _WaveColor ("Wave Color", Color) = (1, 0.85, 0, 1)
        _WaveBorderColor ("Wave Border Color", Color) = (1, 1, 0.7, 1)
        _WaveBorder ("Wave Border Thickness", Range(0, 0.1)) = 0.02
        
        _WaveAmplitude ("Wave Amplitude", Float) = 0.015
        _WaveFrequency ("Wave Frequency", Float) = 15.0
        _WaveSpeed ("Wave Speed", Float) = 5.0

        [Header(Glow Settings)]
        _GlowIntensity ("Glow Intensity", Float) = 3.0
        _GlowWhiteMix ("Glow White Mix", Range(0, 1)) = 0.4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma shader_feature_local_fragment UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _FillAmount;
            fixed4 _WaveColor;
            fixed4 _WaveBorderColor;
            float _WaveBorder;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _GlowIntensity;
            float _GlowWhiteMix;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Wavy calculation using sine and cosine waves
                float wave = _WaveAmplitude * sin(IN.texcoord.x * _WaveFrequency + _Time.y * _WaveSpeed);
                wave += _WaveAmplitude * 0.4 * cos(IN.texcoord.x * _WaveFrequency * 1.8 - _Time.y * _WaveSpeed * 0.7);

                // Scale wave amplitude down near 0 and 1 fill levels to avoid clipping artifacts
                float waveScale = smoothstep(0.0, 0.05, _FillAmount) * smoothstep(1.0, 0.95, _FillAmount);
                float threshold = _FillAmount + wave * waveScale;

                if (IN.texcoord.y < threshold)
                {
                    // Inside the liquid
                    if (threshold - IN.texcoord.y < _WaveBorder && _FillAmount < 0.99)
                    {
                        // Soft glowing edge
                        float borderLerp = (threshold - IN.texcoord.y) / _WaveBorder;
                        color.rgb = lerp(_WaveBorderColor.rgb * 1.3, color.rgb * _WaveColor.rgb, borderLerp);
                    }
                    else
                    {
                        // Liquid body
                        if (_FillAmount >= 0.99)
                        {
                            // HDR emission glow: lerp to white for the "hot" core look and multiply by intensity
                            fixed3 baseColor = color.rgb * _WaveColor.rgb;
                            color.rgb = lerp(baseColor, fixed3(1.0, 1.0, 1.0), _GlowWhiteMix) * _GlowIntensity;
                        }
                        else
                        {
                            color.rgb = color.rgb * _WaveColor.rgb;
                        }
                    }
                }
                else
                {
                    // Above/outside the liquid (cooldown remaining)
                    color.rgb = color.rgb * 0.35; // Dim the unfilled part
                }

                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
        ENDCG
        }
    }
}

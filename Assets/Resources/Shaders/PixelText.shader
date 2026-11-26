// Piksel yazı: yazı tipi dokusunu nokta (Point) örnekleyerek ve saydamlığı eşikleyerek çizer; harfler bulanık değil net piksel olur.
// Yazı tipinin dokusu bütün yazılarla paylaşılır; dokunun filtresini değiştirmek yerine örnekleme burada yapılır (diğer yazılar etkilenmez).
// GUI/Text Shader gibi TextMesh'in köşe rengini kullanır.
Shader "KelimeOyunu/PixelText"
{
    Properties
    {
        _MainTex ("Yazı tipi dokusu", 2D) = "white" {}
        _Cutoff ("Saydamlık eşiği", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Lighting Off
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState point_clamp_sampler; // Satır içi örnekleyici: dokunun kendi ayarından bağımsız, hep Point
            float4 _MainTex_ST;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float coverage = _MainTex.Sample(point_clamp_sampler, i.texcoord).a;
                float4 col = i.color;
                col.a *= step(_Cutoff, coverage); // Yarı dolu pikseller ya tam dolu ya boş: kenarlar keskin
                return col;
            }
            ENDHLSL
        }
    }
}

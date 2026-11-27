// Vuruş parlaması: sprite'ı resmin şekliyle tek renge (beyaz) boyar. Resmin saydamlığı ve köşe renginin (SpriteRenderer.color)
// saydamlığı korunur; renk kanalları yerine _FlashColor yazılır. Vurulan karakter/yaratık 1-2 kare bu malzemeyle çizilir,
// sonra kendi malzemesine döner. Işıksız, URP 2D'de PixelText gibi çalışır.
Shader "KelimeOyunu/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FlashColor ("Parlama rengi", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Lighting Off
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _FlashColor;

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
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.texcoord).a * i.color.a;
                return float4(_FlashColor.rgb, alpha * _FlashColor.a);
            }
            ENDHLSL
        }
    }
}

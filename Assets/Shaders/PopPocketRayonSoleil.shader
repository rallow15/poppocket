// Shader maison Pop Pocket — RAYONS DE SOLEIL (style Pokémon kawaii).
//
// Des faisceaux lumineux translucides qui descendent à travers les
// feuilles de la forêt. Le mesh est un simple quad vertical dont les
// sommets portent ALPHA 0 en bas → 0.35 en haut : le faisceau se fond
// dans l'air. Additif (Blend SrcAlpha One = lumière qui s'AJOUTE) pour
// l'effet « soleil à travers les feuilles ».
// Éclairage par n'importe quoi : c'est UNLIT, le rayon brille pareil
// jour/nuit. Pas de Zwrite (on voit à travers), pas de faces cachées
// (Cull Off : visible des deux côtés).
Shader "PopPocket/RayonSoleil"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True"
               "RenderType" = "Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 col : COLOR0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.col;
            }
            ENDCG
        }
    }
}
// ══════════════════════════════════════════════════════════════════════
//  EAU STYLISÉE DE L'ARÈNE — Pop Pocket (salim 03/10)
//  « One Click Add Water -Stylized Water Shader pour remplacer le fond » :
//  le pack est URP-ONLY → inutilisable en Built-in (shader rose). On refait
//  le MÊME look maison, 100 % compatible : eau qui ondule (3 ondes qui se
//  croisent = caustiques), reflets clairs sur les crêtes, mousse blanche au
//  bord. TOUT est calculé dans le shader : aucune image, aucune texture.
//  Les vagues utilisent la position MONDE → elles font qu'un entre le sol
//  et la cuve pose dessus (la même vague passe de l'un à l'autre).
// ══════════════════════════════════════════════════════════════════════
Shader "PopPocket/EauStylisee"
{
    Properties
    {
        _CouleurCentre ("Couleur au centre", Color) = (0.63, 0.93, 0.98, 1)
        _CouleurBord   ("Couleur au bord",   Color) = (0.33, 0.78, 0.94, 1)
        _CouleurVague  ("Reflets des vagues", Color) = (0.93, 1.00, 1.00, 1)
        _CouleurMousse ("Mousse du bord", Color)    = (0.94, 1.00, 1.00, 1)
        _Vitesse       ("Vitesse des vagues", Range(0.0, 2.0)) = 0.30
        _Echelle       ("Taille des vagues", Range(0.5, 12.0)) = 2.5
        _ForceReflets  ("Force des reflets", Range(0.0, 1.0)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            fixed4 _CouleurCentre;
            fixed4 _CouleurBord;
            fixed4 _CouleurVague;
            fixed4 _CouleurMousse;
            half   _Vitesse;
            half   _Echelle;
            half   _ForceReflets;

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;   // 0..1 sur le quad (dégradé + mousse)
                float3 onde : TEXCOORD1;   // position monde xz (vagues continues)
            };

            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                o.onde = mul(unity_ObjectToWorld, vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Vitesse;

                // ── 1) LES VAGUES : 3 ondes qui se croisent ─────────────
                // Les crêtes se multiplient → dessin de caustique stylisée,
                // comme les lignes claires au fond d'une piscine.
                float3 pw = i.onde;
                float v1 = sin(pw.x * _Echelle + t * 2.05);
                float v2 = sin(pw.z * (_Echelle * 0.87) - t * 1.55);
                float v3 = sin((pw.x - pw.z) * (_Echelle * 0.71) + t * 2.35);
                float vague = v1 + v2 + v3;                 // -3 .. +3

                // lignes claires là où le sommet des 3 ondes se croise
                float reflet = smoothstep(1.30, 2.75, vague);

                // ── 2) COULEUR DE BASE : dégradé centre → bord ──────────
                // max(|dx|,|dy|) : dégradé CARRÉ (suit le quad), coins pas
                // plus sombres que les milieux des côtés.
                float2 c0 = i.uv - 0.5;
                float r = max(abs(c0.x), abs(c0.y)) * 2.0;  // 0 centre → 1 bord
                fixed4 base = lerp(_CouleurCentre, _CouleurBord, saturate(r));

                // ── 3) MOUSSE : bord blanc qui ondule doucement ─────────
                float ang = atan2(c0.y, c0.x);
                float bord = r
                           + sin(ang * 22.0 + t * 3.0) * 0.018   // bord qui bouge
                           + vague * 0.010;                       // suit les vagues
                float mousse = smoothstep(0.86, 0.99, bord);

                fixed4 col = base + _CouleurVague * reflet * _ForceReflets;
                col = lerp(col, _CouleurMousse, mousse);
                return fixed4(col.rgb, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
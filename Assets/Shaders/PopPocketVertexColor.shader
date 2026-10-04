// Shader maison Pop Pocket — matériau peint PAR SOMMET (vertex colors).
//
// Utilisé par UrpMaterialConverter pour les matériaux du pack Willowwood
// (ex. M_RocksCover : mousse sur les rochers) qui n'ont AUCUN fichier de
// texture : leur couleur est peinte sommet par sommet, et le shader
// Standard du Built-in pipeline l'ignore (le mesh rendrait tout blanc).
// Ce shader fait Albedo = couleur du sommet × Tint, avec un éclairage
// Standard normal (ombre correcte sous les arbres).
Shader "PopPocket/VertexColor"
{
    Properties
    {
        _Color ("Teinte", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;

        // Unity remplit automatiquement IN.color avec la couleur du sommet.
        struct Input
        {
            float4 color : COLOR;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Smoothness = 0.25;
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
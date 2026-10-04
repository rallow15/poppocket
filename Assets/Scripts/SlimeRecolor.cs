using UnityEngine;

/// <summary>
/// Colore le slime selon son index de jeu (0 = joueur, 1..3 = bots).
/// Posé par le pack de slimes McSteeg (un seul modèle, 4 couleurs de corps) :
/// bleu = joueur, vert / orange / blanc = les 3 bots.
/// Le matériau des yeux (eyesMaterial) n'est jamais remplacé.
/// </summary>
[DefaultExecutionOrder(200)]
public class SlimeRecolor : MonoBehaviour
{
    [Tooltip("DOIT rester FALSE sur ce projet : ce script écrasait le look gelée du pack Symphonie par des couleurs pleines. Il est désactivé par défaut pour ne jamais pouvoir le refaire.")]
    public bool actif = false;

    public Material[] bodyByIndex;   // une couleur de corps par slime
    public Material eyesMaterial;    // matériau des yeux (non modifié)
    bool applied;

    void Update()
    {
        if (!actif) return;   // DÉSACTIVÉ : le look vient du pack Symphonie maintenant
        if (applied) return;   // une seule fois : l'index ne change pas en jeu
        var sc = GetComponent<SlimeController>();
        if (sc == null) return;   // pas un slime de jeu → rien à faire

        int idx = sc.isPlayer
            ? 0
            : Mathf.Clamp(sc.slimeIndex, 0, Mathf.Max(0, bodyByIndex.Length - 1));

        Material body = (bodyByIndex != null && bodyByIndex.Length > 0)
            ? bodyByIndex[idx] : null;
        if (body == null) { applied = true; return; }

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null || MatsEqual(mats[m], eyesMaterial) || MatsEqual(mats[m], body))
                    continue;
                mats[m] = body;   // slot du corps → couleur de CE slime
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
        applied = true;
    }

    static bool MatsEqual(Material a, Material b) => a != null && b != null && a == b;
}
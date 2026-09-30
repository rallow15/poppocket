#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ANCIENNE génération de texture "papier à bulles" sur le sol.
/// Désactivé : le jeu a maintenant de VRAIES bulles 3D posées sur un
/// sol bleu lisse (voir RobloxStyleRebuild). Gardé en no-op pour que le
/// fichier .meta reste stable.
/// </summary>
[InitializeOnLoad]
public static class BubbleWrapFloor
{
    static BubbleWrapFloor()
    {
        // plus rien à faire — le sol est géré par RobloxStyleRebuild
    }
}
#endif
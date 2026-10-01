using UnityEngine;

/// <summary>
/// Caméra qui s'adapte au sens du téléphone :
///   • PORTRAIT (vertical)  → VUE DE DESSUS FIXE (demandée par salim) :
///     la caméra ne bouge PAS, elle est au-dessus du centre de l'arène,
///     dézoomée juste assez pour voir TOUT le sol de bulles d'un coup,
///     comme un plateau de jeu sur une table.
///   • PAYSAGE (horizontal) → la vue d'ORIGINE : derrière le slime,
///     perspective rase, la caméra suit le joueur (RIEN n'a changé).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform du slime joueur (assigné par GameManager)")]
    public Transform followTarget;

    [Header("Vue 3e personne (PAYSAGE - inchangee)")]
    [Tooltip("Hauteur de la caméra au-dessus du joueur (paysage)")]
    public float height = 4.5f;
    [Tooltip("Distance de la caméra derrière le joueur (paysage)")]
    public float backDistance = 8.5f;
    [Tooltip("Hauteur du point visé (la caméra plonge un peu vers le sol) (paysage)")]
    public float lookAtHeight = 1.2f;

    [Header("Vue DE DESSUS (PORTRAIT seulement)")]
    [Tooltip("Portrait : caméra FIXE qui voit TOUT le sol de bulles d'un coup")]
    public bool portraitVueEntiere = true;
    [Tooltip("Portrait : marge autour du sol (en mètres) pour ne rien couper")]
    public float portraitMargeSol = 6f;
    [Tooltip("Portrait : hauteur de la caméra (SI la vue fixe est désactivée)")]
    public float portraitHeight = 20f;
    [Tooltip("Portrait : petit recul derrière le joueur (si vue fixe désactivée)")]
    public float portraitBackDistance = 1.2f;
    [Tooltip("Portrait : la caméra regarde le slime lui-même (pas devant)")]
    public float portraitLookAtHeight = 0f;

    [Header("Fluide / zoom")]
    [Tooltip("Vitesse de suivi fluide (plus faible = plus doux)")]
    public float smoothSpeed = 5f;
    [Tooltip("Champ de vision en mode PAYSAGE (degrés)")]
    public float fovLandscape = 55f;
    [Tooltip("Champ de vision en mode PORTRAIT (degrés)")]
    public float fovPortrait = 66f;

    private Camera cam;
    private bool isPortrait;
    private float currentFov;

    // Taille de l'arène (récupérée du BubbleSpawner) pour cadrer TOUT le sol
    private bool hasArena;
    private Vector3 arenaCenter;
    private float arenaHalfX = 14f, arenaHalfZ = 14f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;   // vue perspective pour l'effet 3D "rasant"
        isPortrait = Screen.height > Screen.width;
        currentFov = TargetFov();
        cam.fieldOfView = currentFov;
    }

    private void Start()
    {
        // On récupère la taille du sol de bulles pour le cadrage complet
        var sp = FindObjectOfType<BubbleSpawner>();
        if (sp != null)
        {
            arenaCenter = sp.transform.position;
            arenaHalfX = sp.arenaSize.x * 0.5f;
            arenaHalfZ = sp.arenaSize.y * 0.5f;
            hasArena = true;
        }
    }

    /// <summary>GameManager donne le slime joueur après le spawn.</summary>
    public void SetTarget(Transform target)
    {
        followTarget = target;
        if (followTarget != null)
        {
            // Saute instantanément sur la bonne position (pas de glissement au départ)
            transform.position = DesiredPosition();
            transform.LookAt(LookPoint());
        }
    }

    private void LateUpdate()
    {
        // 1) Adaptation à l'orientation (portrait / paysage)
        bool nowPortrait = Screen.height > Screen.width;
        if (nowPortrait != isPortrait)
        {
            isPortrait = nowPortrait;
        }

        // FOV fluide : s'ajuste aussi quand l'utilisateur tourne le téléphone
        float targetFov = TargetFov();
        currentFov = Mathf.Lerp(currentFov, targetFov, Time.deltaTime * 4f);
        cam.fieldOfView = currentFov;

        // 2) Position de la caméra selon le mode
        if (followTarget == null) return;

        if (isPortrait && portraitVueEntiere && hasArena)
        {
            // Vue de dessus FIXE : on se pose DIRECTEMENT au bon endroit
            // (pas de glissement → jamais un instant avec la vue coupée)
            transform.position = DesiredPosition();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                                              smoothSpeed * Time.deltaTime);
        }

        // 3) Rotation de la caméra selon le mode
        if (isPortrait && portraitVueEntiere && hasArena)
        {
            // Vue de dessus FIXE : bien droite vers le bas
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            transform.LookAt(LookPoint());
        }
    }

    /// <summary>
    /// Position cible :
    ///   • PORTRAIT + vue entière → FIXE au-dessus du CENTRE de l'arène,
    ///     dézoomée juste assez pour voir tout le sol de bulles.
    ///   • PORTRAIT + vue entière OFF → au-dessus du joueur (vue plongeante).
    ///   • PAYSAGE → comme avant (derrière le joueur, vue rase).
    /// </summary>
    private Vector3 DesiredPosition()
    {
        if (isPortrait && portraitVueEntiere && hasArena)
            return VueEntiereSolPosition();

        if (followTarget == null) return transform.position;

        if (isPortrait)
        {
            return followTarget.position
                   + Vector3.back * portraitBackDistance   // quasi à la verticale
                   + Vector3.up * portraitHeight;
        }

        return followTarget.position
               + Vector3.back * backDistance   // toujours derrière (sud)
               + Vector3.up * height;
    }

    /// <summary>Point que la caméra regarde :</summary>
    private Vector3 LookPoint()
    {
        if (followTarget == null) return Vector3.zero;

        if (isPortrait)
        {
            // Vue de dessus (si vue fixe off) : on regarde le slime lui-même
            return followTarget.position + Vector3.up * portraitLookAtHeight;
        }

        return followTarget.position
               + Vector3.up * lookAtHeight
               + Vector3.forward * 3.5f;  // on voit le tapis DEVANT le slime
    }

    /// <summary>
    /// Le dézoom MAGIQUE : la caméra monte exactement assez haut pour que
    /// TOUT le sol de bulles (l'arène + une marge) tienne dans l'écran.
    /// Ça marche quelle que soit la taille de l'arène et l'écran du téléphone.
    /// </summary>
    private Vector3 VueEntiereSolPosition()
    {
        // Largeur visible en fonction du FOV vertical et de la forme de l'écran
        float tanV = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(10f, currentFov) * 0.5f);
        float tanH = tanV * cam.aspect;

        float marge = portraitMargeSol;
        float besoinHauteurZ = (arenaHalfZ + marge) / tanV;                 // sens de l'écran vertical
        float besoinHauteurX = (arenaHalfX + marge) / Mathf.Max(0.05f, tanH); // sens horizontal

        // Petite sécurité (+8%) : même si le FOV ou l'écran varie un poil,
        // on garde toujours de l'air autour du sol → rien n'est coupé.
        float hauteur = Mathf.Max(besoinHauteurZ, besoinHauteurX) * 1.08f;
        return arenaCenter + Vector3.up * hauteur;
    }

    private float TargetFov()
    {
        return isPortrait ? fovPortrait : fovLandscape;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Feedback visuel immédiat dans l'éditeur
        if (Application.isPlaying) return;
        cam = GetComponent<Camera>();
        if (cam != null) cam.orthographic = false;
    }
#endif
}
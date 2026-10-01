using UnityEngine;

/// <summary>
/// Caméra qui s'adapte au sens du téléphone :
///   • PORTRAIT (vertical)  → VUE DE DESSUS (demandée par salim) :
///     caméra presque à la verticale au-dessus du slime, on voit le sol
///     autour de soi comme sur une carte.
///   • PAYSAGE (horizontal) → la vue d'ORIGINE : derrière le slime,
///     perspective rase (RIEN n'a changé pour ce mode).
/// Le suivi reste fluide dans les deux cas.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform du slime joueur (assigné par GameManager)")]
    public Transform followTarget;

    [Header("Vue 3e personne (PAYSAGE — inchangée)")]
    [Tooltip("Hauteur de la caméra au-dessus du joueur (paysage)")]
    public float height = 4.5f;
    [Tooltip("Distance de la caméra derrière le joueur (paysage)")]
    public float backDistance = 8.5f;
    [Tooltip("Hauteur du point visé (la caméra plonge un peu vers le sol) (paysage)")]
    public float lookAtHeight = 1.2f;

    [Header("Vue DE DESSUS (PORTRAIT seulement)")]
    [Tooltip("Portrait : hauteur de la caméra (vue plongeante) — plus haut = on voit plus large")]
    public float portraitHeight = 20f;
    [Tooltip("Portrait : petit recul derrière le joueur (presque à la verticale)")]
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

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;   // vue perspective pour l'effet 3D "rasant"
        isPortrait = Screen.height > Screen.width;
        currentFov = TargetFov();
        cam.fieldOfView = currentFov;
    }

    /// <summary>GameManager donne le slime joueur après le spawn.</summary>
    public void SetTarget(Transform target)
    {
        followTarget = target;
        if (followTarget != null)
        {
            // Saute instantanément sur le joueur (pas de glissement au départ)
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

        // 2) Suivi fluide du joueur, toujours DERRIÈRE lui
        if (followTarget == null) return;

        transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                                          smoothSpeed * Time.deltaTime);
        transform.LookAt(LookPoint());
    }

    /// <summary>
    /// Position cible :
    ///   • PAYSAGE  → comme avant (derrière le joueur, vue rase),
    ///   • PORTRAIT → presque à la verticale : vue DE DESSUS demandée par salim.
    /// </summary>
    private Vector3 DesiredPosition()
    {
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
            // Vue de dessus : on regarde le slime lui-même
            return followTarget.position + Vector3.up * portraitLookAtHeight;
        }

        return followTarget.position
               + Vector3.up * lookAtHeight
               + Vector3.forward * 3.5f;  // on voit le tapis DEVANT le slime
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
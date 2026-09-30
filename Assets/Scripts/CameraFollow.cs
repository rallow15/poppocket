using UnityEngine;

/// <summary>
/// Caméra à la 3e personne, façon Roblox : elle reste DERRIÈRE le slime
/// (direction fixe vers le sud) et le suit en glissant. Vue perspective
/// rase pour voir l'étendue du tapis de bulles devant soi.
/// S'adapte au portrait/paysage en élargissant le champ de vision (FOV).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform du slime joueur (assigné par GameManager)")]
    public Transform followTarget;

    [Header("Vue 3e personne")]
    [Tooltip("Hauteur de la caméra au-dessus du joueur")]
    public float height = 4.5f;
    [Tooltip("Distance de la caméra derrière le joueur")]
    public float backDistance = 8.5f;
    [Tooltip("Hauteur du point visé (la caméra plonge un peu vers le sol)")]
    public float lookAtHeight = 1.2f;

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

    /// <summary>Position cible : derrière le joueur, en hauteur.</summary>
    private Vector3 DesiredPosition()
    {
        if (followTarget == null) return transform.position;
        return followTarget.position
               + Vector3.back * backDistance   // toujours derrière (sud)
               + Vector3.up * height;
    }

    /// <summary>Point que la caméra regarde : un peu devant et au-dessus du slime.</summary>
    private Vector3 LookPoint()
    {
        if (followTarget == null) return Vector3.zero;
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
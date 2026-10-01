using UnityEngine;

/// <summary>
/// Caméra qui suit le slime DERRIÈRE lui (vue 3e personne).
/// La même vue pour vertical et horizontal — SAUF un petit dézoom en
/// vertical (l'écran est plus étroit, on recule un peu pour voir autant).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform du slime joueur (assigné par GameManager)")]
    public Transform followTarget;

    [Header("Vue 3e personne")]
    [Tooltip("Hauteur de la caméra au-dessus du joueur")]
    public float height = 5.0f;
    [Tooltip("Distance de la caméra derrière le joueur")]
    public float backDistance = 10.5f;
    [Tooltip("Hauteur du point visé (la caméra plonge un peu vers le sol)")]
    public float lookAtHeight = 1.6f;

    [Header("Portrait (vertical) seulement")]
    [Tooltip("En vertical : on recule un peu plus (1.25 = +25%) pour voir autant que l'horizontal")]
    public float portraitDezoomFacteur = 1.25f;

    [Header("Fluide / zoom")]
    [Tooltip("Vitesse de suivi fluide (plus faible = plus doux)")]
    public float smoothSpeed = 5f;
    [Tooltip("Champ de vision (degrés)")]
    public float fov = 66f;

    private Camera cam;
    private bool isPortrait;
    private float currentFov;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;   // vue perspective "rasante"
        isPortrait = Screen.height > Screen.width;
        currentFov = fov;
        cam.fieldOfView = currentFov;
    }

    /// <summary>GameManager donne le slime joueur après le spawn.</summary>
    public void SetTarget(Transform target)
    {
        followTarget = target;
        if (followTarget == null) return;

        // Saute instantanément sur le joueur (pas de glissement au départ)
        currentFov = fov;
        cam.fieldOfView = currentFov;
        transform.position = DesiredPosition();
        transform.LookAt(LookPoint());
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        // Portrait / paysage (peut changer si l'utilisateur tourne le téléphone)
        isPortrait = Screen.height > Screen.width;

        // Suivi fluide du joueur, toujours DERRIÈRE lui
        transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                                          smoothSpeed * Time.deltaTime);
        transform.LookAt(LookPoint());
    }

    /// <summary>
    /// Position cible : derrière le joueur.
    /// En VERTICAL on recule un peu plus (le dézoom demandé par salim).
    /// </summary>
    private Vector3 DesiredPosition()
    {
        if (followTarget == null) return transform.position;

        float dezoom = isPortrait ? portraitDezoomFacteur : 1f;

        return followTarget.position
               + Vector3.back * (backDistance * dezoom)   // toujours derrière (sud)
               + Vector3.up * (height * dezoom);
    }

    /// <summary>Point que la caméra regarde : le joueur + un peu devant (on voit le tapis devant).</summary>
    private Vector3 LookPoint()
    {
        if (followTarget == null) return Vector3.zero;

        return followTarget.position
               + Vector3.up * lookAtHeight
               + Vector3.forward * 3.5f;
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
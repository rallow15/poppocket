using UnityEngine;

/// <summary>
/// Caméra qui suit le slime dERRIÈRE lui (vue 3e personne).
/// LA MÊME VUE pour vertical ET horizontal (demande salim : « comme horizontal »).
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
    [Tooltip("Champ de vision (degrés)")]
    public float fovLandscape = 55f;

    private Camera cam;
    private float currentFov;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;   // vue perspective "rasante"
        currentFov = fovLandscape;
        cam.fieldOfView = currentFov;
    }

    /// <summary>GameManager donne le slime joueur après le spawn.</summary>
    public void SetTarget(Transform target)
    {
        followTarget = target;
        if (followTarget == null) return;

        // Saute instantanément sur le joueur (pas de glissement au départ)
        cam.fieldOfView = fovLandscape;
        transform.position = DesiredPosition();
        transform.LookAt(LookPoint());
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        cam.fieldOfView = fovLandscape;

        // Suivi fluide du joueur, toujours DERRIÈRE lui
        transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                                          smoothSpeed * Time.deltaTime);
        transform.LookAt(LookPoint());
    }

    /// <summary>Position cible : derrière le joueur, vue rase (comme avant dans les 2 sens).</summary>
    private Vector3 DesiredPosition()
    {
        if (followTarget == null) return transform.position;

        return followTarget.position
               + Vector3.back * backDistance   // toujours derrière (sud)
               + Vector3.up * height;
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
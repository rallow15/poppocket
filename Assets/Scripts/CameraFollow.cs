using UnityEngine;

/// <summary>
/// Caméra ISOMÉTRIQUE façon League of Legends (demande salim) :
/// en hauteur, inclinée ~45°, en perspective 3D (pas orthographique).
/// Caméra posée derrière le joueur : base Y=8, Z=-8 (comme LoL) puis
/// DÉZOOM demandé → Y=11, Z=-11, FOV 55. Le ~45° est toujours gardé
/// (hauteur et recul bougent ENSEMBLE).
/// La même vue pour vertical et horizontal — SAUF un petit dézoom en
/// vertical (l'écran est plus étroit, on recule un peu pour voir autant).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform du slime joueur (assigné par GameManager)")]
    public Transform followTarget;

    [Header("Vue isométrique (façon League of Legends)")]
    [Tooltip("Hauteur de la caméra au-dessus du joueur (Y=8 comme LoL, +3 au dézoom salim)")]
    public float height = 11.0f;
    [Tooltip("Distance derrière le joueur (Z=-8 comme LoL → inclinaison ~45°)")]
    public float backDistance = 11.0f;
    [Tooltip("Hauteur du point visé (0 = on vise le sol au niveau du joueur)")]
    public float lookAtHeight = 0.0f;

    [Header("Portrait (vertical) seulement")]
    [Tooltip("En vertical : on recule un peu plus (1.25 = +25%) pour voir autant que l'horizontal")]
    public float portraitDezoomFacteur = 1.25f;

    [Header("Fluide / zoom")]
    [Tooltip("Vitesse de suivi fluide (plus faible = plus doux)")]
    public float smoothSpeed = 5f;
    [Tooltip("Champ de vision (degrés) — LoL : entre 40 et 60")]
    public float fov = 55f;

    private Camera cam;
    private bool isPortrait;
    private float currentFov;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;   // perspective 3D (pas orthographique, comme LoL)
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

        // Suivi fluide du joueur, toujours AU-DESSUS de lui
        transform.position = Vector3.Lerp(transform.position, DesiredPosition(),
                                          smoothSpeed * Time.deltaTime);
        transform.LookAt(LookPoint());
    }

    /// <summary>
    /// Position cible : au-dessus du joueur, légèrement vers le sud.
    /// En VERTICAL on monte un peu plus (le dézoom demandé par salim).
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
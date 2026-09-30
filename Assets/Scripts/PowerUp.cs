using UnityEngine;

/// <summary>
/// Les 3 bonus du jeu (durée 3 s chacun) :
///  - Speed : le slime va 4x plus vite
///  - Aura  : les bulles éclatent TOUTES SEULES dans une petite zone autour du slime
///  - Zap   : TOUS les AUTRES slimes sont gelés (seul le ramasseur peut bouger)
/// Ramassable par le joueur OU les bots : il suffit de le toucher.
/// </summary>
public enum PowerUpType
{
    Speed,  // jaune  : déplacement rapide x4
    Aura,   // rose   : éclate les bulles autour soi
    Zap     // bleu   : étourdit tout le monde sauf toi
}

public class PowerUp : MonoBehaviour
{
    [Tooltip("Quel bonus ce pickup donne")]
    public PowerUpType type;

    private Vector3 basePos;   // position d'origine (pour flotter sur place)

    private void Start()
    {
        basePos = transform.position;
    }

    /// <summary>Le bonus tourne sur lui-même et flotte doucement (animation gratuite).</summary>
    private void Update()
    {
        transform.rotation = Quaternion.Euler(0f, Time.time * 140f, 0f);
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.12f);
    }

    /// <summary>Un slime (joueur OU bot) touche le bonus → effet tout de suite.</summary>
    private void OnTriggerEnter(Collider other)
    {
        SlimeController sc = other.GetComponentInParent<SlimeController>();
        if (sc == null) return; // ce n'est pas un slime

        PowerUpManager.Apply(type, sc);
        PowerUpManager.PlayBonusSound();
        Debug.Log("[POWER-UP] Ramassé : " + type + " par " +
                  (sc.isPlayer ? "le joueur !" : "un bot !"));
        Destroy(gameObject);
    }
}
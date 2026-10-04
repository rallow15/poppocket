using UnityEngine;

/// <summary>
/// Les 3 bonus du jeu (durée 3 s chacun) :
///  - Speed : éclair jaune → le slime va 4x plus vite
///  - Aura  : champignon rouge style Mario → le slime devient GEANT x3
///  - Zap   : flocon de neige → TOUS les AUTRES slimes sont gelés
/// Ramassable par le joueur OU les bots : il suffit de le toucher.
/// </summary>
public enum PowerUpType
{
    Speed,  // éclair jaune  : déplacement rapide x4
    Aura,   // champignon    : géant x3 pendant 3 s
    Zap     // flocon de gel : étourdit tout le monde sauf toi
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

        // FIX (salim 03/10 : « quand tu prend champignon tu grossi pas ») :
        // l'EFFET est appliqué EN PREMIER — si l'affichage HUD plantait, le
        // grossissement ne devait jamais rater à cause de lui.
        PowerUpManager.Apply(type, sc);

        // HUD ensuite : une ligne en haut de l'écran, QUI a pris le bonus
        // (Toi / Bot 1 / Bot 2 / Bot 3) + les secondes qui défilent.
        PowerUpHud.Show(type, sc);

        PowerUpManager.PlayBonusSound();
        Debug.Log("[POWER-UP] Ramassé : " + type + " par " +
                  (sc.isPlayer ? "le joueur !" : "un bot !"));
        Destroy(gameObject);
    }
}
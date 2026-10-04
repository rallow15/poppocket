using UnityEngine;
using UnityEngine.EventSystems; // Joystick Pack hérite de ces interfaces

/// <summary>
/// Contrôle un Slime : soit le joueur (via le joystick tactile), soit un bot IA.
/// Mouvement "glissant et rebondissant" assuré par le Rigidbody + PhysicMaterial.
/// L'IA fonce vers la bulle la plus proche, avec une petite inertie / bruit de
/// direction pour donner l'impression d'un bot "casual" (pas trop parfait).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SlimeController : MonoBehaviour
{
    [Header("Identité")]
    [Tooltip("true = contrôlé par le joueur au joystick / false = bot IA")]
    public bool isPlayer = false;

    [Tooltip("Index du slime (0-3), sert pour la couleur de l'UI et les scores")]
    public int slimeIndex = 0;

    [Header("Mouvement joueur")]
    [Tooltip("Accélération du joueur (m/s²) — ForceMode.Acceleration = indépendant de la masse")]
    public float playerForce = 60f;
    [Tooltip("Vitesse du saut du joueur (tap court = saut)")]
    public float jumpForce = 7f;
    [Tooltip("Force de squash appliquée en mouvement (effet satisfaisant)")]
    public float squashMagnitude = 0.15f;
    [Tooltip("Force de descente en plus quand le slime retombe (chute rapide, saut nerveux)")]
    public float fallBoost = 40f;

    [Header("IA - Réglages bot")]
    [Tooltip("Accélération de l'IA (m/s²) — moins fort que le joueur = équilibré")]
    public float botForce = 30f;
    [Tooltip("Portée du cercle de détection des bulles")]
    public float detectionRadius = 30f;
    [Tooltip("Secondes d'hésitation avant de changer de cible (bot moins mécanique)")]
    public float retargetDelay = 1.5f;

    [Header("Power-ups (durée 3 s, posés par PowerUpManager)")]
    [Tooltip("(Ancien rayon AURA — gardé pour le prefab, plus utilisé)")]
    public float auraRadius = 2.5f;

    // --- Power-ups : actifs tant que Time.time < ces dates (3 s chacun) ---
    private float speedUntil = -1f;     // bonus VITESSE
    private float giantUntil = -1f;     // bonus CHAMPIGNON : géant x3
    private bool giantActive;
    private float stunnedUntil = -1f;   // reçu un ZAP : gelé

    // --- Références ---
    private Rigidbody rb;
    private Transform currentTarget;    // bulle chassée par l'IA
    private float nextRetargetTime;     // hésitation IA
    private Vector3 squashBaseScale;    // scale d'origine pour le squash

    // Joystick du Joystick Pack (Fenerax) — référencé FAIBLEMENT (MonoBehaviour +
    // reflection sur la propriété Direction) pour que le projet compile AVANT
    // l'import du pack, et pour rester neutre si tu changes de joystick plus tard.
    private MonoBehaviour joystickSource;
    private System.Reflection.PropertyInfo directionProp;
    private System.Reflection.MethodInfo consumeJump;

    public Rigidbody Rigidbody => rb;

    private Vector2 smoothInput;   // direction lissée (mouvement plus fluide)

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        squashBaseScale = transform.localScale;

        // SÉCURITÉ : le fichier préfab a déjà été re-sauvegardé par Unity
        // avec la vieille valeur (botForce 15 → bots à 1,8 m/s, en dessous
        // du seuil d'éclatement des bulles → ils vibrent et ne pètent rien).
        // On remet la bonne valeur ICI, à l'exécution : plus rien ne peut
        // la perdre, même si Unity re-écrit le préfab.
        if (isPlayer == false && botForce < 32f) botForce = 32f;

        // FLUIDITÉ : le Rigidbody est interpolé entre 2 pas de physique
        // → le slime glisse à l'écran au lieu de "sautiller" à 60 Hz fixes.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Un slime ne doit pas basculer sur le côté
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        // Le joystick n'existe que pour le joueur
        if (isPlayer && GameManager.Instance != null)
        {
            joystickSource = GameManager.Instance.PlayerJoystick;
            if (joystickSource != null)
            {
                System.Type t = joystickSource.GetType();
                directionProp = t.GetProperty("Direction");
                consumeJump = t.GetMethod("ConsumeJump");
            }
        }
    }

    private void FixedUpdate()
    {
        // DESCENTE RAPIDE : le "frein à air" (linearDamping) ralentissait la
        // chute des deux sens. En descente, on l'annule et on pousse vers le
        // bas en plus → le slime retombe vite après son saut (saut nerveux).
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector3.down * (rb.linearDamping * -rb.linearVelocity.y + fallBoost),
                        ForceMode.Acceleration);
        }

        TickGiant(); // champignon : le slime grossit x3 tant que c'est actif

        // ZAP reçu : le slime est gelé, il ne peut RIEN faire
        if (Time.time < stunnedUntil)
        {
            ResetSquash();
            return;
        }

        if (isPlayer) HandlePlayerInput();
        else HandleBotAI();
    }

    // ────────────────────────────────────────────────────────────────
    //  JOUEUR
    // ────────────────────────────────────────────────────────────────
    private void HandlePlayerInput()
    {
        // SAUT : un tap court n'importe où sur l'écran (ou Espace sur PC).
        // On saute seulement si le slime touche le sol.
        if (consumeJump != null && (bool)consumeJump.Invoke(joystickSource, null)
            && IsGrounded())
        {
            // Annule la vitesse verticale puis pousse vers le haut
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        // Lit le vecteur du joystick (pack Fenerax) via reflection ;
        // ou, en édition sur PC pour tester : les flèches / WASD du clavier.
        Vector2 input = Vector2.zero;
        bool hasInput = false;

        if (joystickSource != null)
        {
            // Direction est un Vector2 (-1..1) exposé par le Joystick Pack
            System.Type t = joystickSource.GetType();
            var prop = t.GetProperty("Direction");
            if (prop != null)
            {
                object val = prop.GetValue(joystickSource, null);
                if (val is Vector2 v) { input = v; hasInput = true; }
            }
        }

        if (!hasInput)
        {
            // Fallback clavier (flèches / WASD) — marche aussi sur PC
            // même si le build cible Android ; sur téléphone : aucun clavier.
            input = new Vector2(Input.GetAxisRaw("Horizontal"),
                                Input.GetAxisRaw("Vertical"));
            hasInput = true;
        }

        if (!hasInput)
        {
            ResetSquash();
            return;
        }

        // FLUIDITÉ : on ne pousse pas la force brute du doigt, on LISSE
        // la direction (courbe exponentielle) → le slime démarre en douceur,
        // tourne sans à-coups et glisse un peu quand on relâche.
        smoothInput = Vector2.Lerp(smoothInput, input,
                                   1f - Mathf.Exp(-12f * Time.fixedDeltaTime));

        if (smoothInput.sqrMagnitude > 0.002f)
        {
            // Convertit l'input 2D (X = droite/gauche, Y = avant/arrière en vue 3e personne)
            // ForceMode.Acceleration : réactif quelle que soit la masse du Rigidbody.
            Vector3 force = new Vector3(smoothInput.x, 0f, smoothInput.y) * playerForce * PowerFactor;
            rb.AddForce(force, ForceMode.Acceleration);

            ApplySquash(smoothInput.sqrMagnitude);
        }
        else
        {
            ResetSquash();
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  IA (BOT)
    // ────────────────────────────────────────────────────────────────
    private void HandleBotAI()
    {
        // Trouve / conserve une cible (bulle la plus proche OU power-up proche)
        if (currentTarget == null || Time.time >= nextRetargetTime)
        {
            currentTarget = FindTarget();
            nextRetargetTime = Time.time + retargetDelay;
        }

        if (currentTarget == null) return; // plus de bulles : le round se termine bientôt

        // Direction vers la cible
        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        // Petit bruit d'hésitation : le bot dévie un peu de sa trajectoire
        // (rend les bots "casual" — parfait pour un party game familial).
        // Amplitude réduite (0,5) : avec 1,5 le bot perdait trop de vitesse
        // en zigzaguant et n'atteignait jamais le seuil d'éclatement des bulles.
        float wobble = Mathf.PerlinNoise(Time.time * 3f, slimeIndex * 10f) - 0.5f;
        Vector3 perpendicular = Vector3.Cross(toTarget.normalized, Vector3.up);
        toTarget += perpendicular * (wobble * 0.5f);

        if (toTarget.sqrMagnitude > 0.1f)
        {
            rb.AddForce(toTarget.normalized * (botForce * PowerFactor), ForceMode.Acceleration);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  POWER-UPS (appliqués par PowerUpManager quand un bonus est touché)
    // ────────────────────────────────────────────────────────────────
    /// <summary>Bonus VITESSE : le slime va 4 fois plus vite pendant la durée.</summary>
    public void ApplySpeedBoost(float duration) { speedUntil = Time.time + duration; }

    /// <summary>
    /// Bonus CHAMPIGNON (salim 03/10 : « pour le power up aura change le fait
    /// juste grossir le slime 3 fois ca taille ») → GEANT x3 pendant la durée.
    /// </summary>
    public void ApplyGiant(float duration) { giantUntil = Time.time + duration; }

    /// <summary>Bonus ZAP reçu : ce slime est gelé pendant la durée.</summary>
    public void ApplyStun(float duration) { stunnedUntil = Time.time + duration; }

    /// <summary>×4 quand le bonus VITESSE est actif, sinon ×1.</summary>
    private float PowerFactor => Time.time < speedUntil ? 4f : 1f;

    /// <summary>
    /// PROGRESSION (demande de salim, 03/10) : « les bots légèrement
    /// plus fort » quand on monte de niveau. Chaque niveau :
    ///   • bots +18 % plus rapides (plafonné à ×2 pour rester fair-play)
    ///   • ils hésitent 15 % moins longtemps (réaction plus vive)
    ///   • leur radar de bulles s'élargit un peu
    /// Appelé par GameManager.SpawnSlimes sur CHAQUE bot, au spawn.
    /// Le joueur, lui, ne change jamais : c'est l'arène qui devient
    /// plus dure, pas ton slime plus rapide.
    /// </summary>
    public void AppliquerNiveau(int niveau)
    {
        if (niveau <= 1) return;
        float forceMul = Mathf.Min(1f + 0.18f * (niveau - 1), 2f);
        botForce *= forceMul;
        retargetDelay = Mathf.Max(0.4f, retargetDelay * (1f - 0.15f * (niveau - 1)));
        detectionRadius = Mathf.Min(detectionRadius * (1f + 0.15f * (niveau - 1)), 55f);
    }

    /// <summary>
    /// Cible de l'IA : la bulle la plus proche, OU un power-up pas trop loin
    /// (les bots aussi partent chercher les bonus s'ils sont à côté).
    /// </summary>
    private Transform FindTarget()
    {
        Transform pickup = PowerUpManager.CurrentPickup;
        if (pickup != null)
        {
            float reach = detectionRadius * 0.6f; // bonus visé seulement s'il est proche
            if ((pickup.position - transform.position).sqrMagnitude < reach * reach)
                return pickup;
        }
        return FindNearestBubble();
    }

    /// <summary>
    /// CHAMPIGNON actif : le slime devient GEANT (x3) tant que le bonus dure,
    /// puis reprend sa taille normale (demande de salim, 03/10 : l'ancienne
    /// AURA qui éclatait les bulles est remplacée par ce grossissement).
    /// Astuce : on multiplie squashBaseScale par 3 — le squash & stretch
    /// continue de marcher pendant qu'il est géant.
    /// </summary>
    private void TickGiant()
    {
        bool active = Time.time < giantUntil && Time.time >= stunnedUntil;
        if (giantActive == active) return;
        giantActive = active;

        if (active)
        {
            squashBaseScale *= 3f;   // géant : le squash suit la même base
            transform.localScale = squashBaseScale;  // GROSSIT TOUT DE SUITE
            Debug.Log("[CHAMPIGNON] " + name + " devient GEANT !");
        }
        else
        {
            squashBaseScale /= 3f;   // retour taille normale (ramolli par ResetSquash)
            transform.localScale = squashBaseScale;
            Debug.Log("[CHAMPIGNON] " + name + " redevient normal.");
        }
    }

    /// <summary>true tant que le slime touche le sol (raycast sphère vers le bas).</summary>
    private bool IsGrounded()
    {
        // La sphère est à ~0,5 de hauteur ; on teste jusqu'à ~0,25 sous celle-ci.
        // (la sphère-cast ignore les colliders du même Rigidbody)
        return Physics.SphereCast(transform.position, 0.4f, Vector3.down,
                                  out RaycastHit hit, 0.75f);
    }

    /// <summary>Trouve la bulle vivante la plus proche via Physics.OverlapSphere.</summary>
    private Transform FindNearestBubble()
    {
        // LayerMask "Bubble" uniquement ; si le layer n'existe pas dans le
        // projet, on cherche dans tout (fallback pour que les bots jouent quand même)
        int bubbleLayer = LayerMask.NameToLayer("Bubble");
        int bubbleMask = bubbleLayer >= 0 ? (1 << bubbleLayer) : ~0;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, bubbleMask);

        Transform nearest = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            // Ignore les bulles déjà en train d'éclater
            if (col.GetComponent<Bubble>() == null) continue;

            float d = (col.transform.position - transform.position).sqrMagnitude;

            // FIX « bots figés au spawn » : la bulle PILE SOUS LES PIEDS
            // (à moins d'un mètre) est ignorée. Le radar la choisissait
            // (c'est la plus proche !) mais HandleBotAI n'applique sa force
            // qu'au-delà de 0,32 m → le bot restait immobile POUR TOUJOURS
            // sur sa bulle de coin. En l'ignorant, il vise celle d'à côté
            // (1,3 m sur la grille) : il a la place d'accélérer et l'éclate.
            if (d < 1.0f) continue;

            if (d < bestDist)
            {
                bestDist = d;
                nearest = col.transform;
            }
        }

        return nearest;
    }

    // ────────────────────────────────────────────────────────────────
    //  SQUASH & STRETCH (effet "satisfaisant")
    // ────────────────────────────────────────────────────────────────
    private void ApplySquash(float intensity)
    {
        float squash = Mathf.Clamp01(intensity) * squashMagnitude;
        // Multiplié composant par composant (Vector3 * Vector3 n'existe pas en C#)
        Vector3 squashed;
        squashed.x = squashBaseScale.x * (1f - squash);
        squashed.y = squashBaseScale.y * (1f + squash);
        squashed.z = squashBaseScale.z * (1f - squash);
        transform.localScale = Vector3.Lerp(
            transform.localScale, squashed,
            Time.fixedDeltaTime * 12f);
    }

    private void ResetSquash()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale, squashBaseScale, Time.fixedDeltaTime * 10f);
    }

    /// <summary>Appelé par GameManager pour réinitialiser la position au début d'un round.</summary>
    public void ResetToSpawn(Vector3 spawnPos)
    {
        rb.position = spawnPos;
        rb.linearVelocity = Vector3.zero;   // Unity 6 (Rigidbody.linearVelocity)
        rb.angularVelocity = Vector3.zero;
        transform.position = spawnPos;
        transform.localScale = squashBaseScale;
        currentTarget = null;

        // Nettoie tous les effets power-up au début d'un nouveau round
        speedUntil = giantUntil = stunnedUntil = -1f;
        if (giantActive) { giantActive = false; squashBaseScale /= 3f; }
        smoothInput = Vector2.zero;   // on repart du repos (pas de glisse fantôme)
    }
}
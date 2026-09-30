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
    [Tooltip("Rayon de l'AURA : les bulles dedans éclatent toutes seules")]
    public float auraRadius = 2.5f;

    // --- Power-ups : actifs tant que Time.time < ces dates (3 s chacun) ---
    private float speedUntil = -1f;     // bonus VITESSE
    private float auraUntil = -1f;      // bonus AURA
    private float stunnedUntil = -1f;   // reçu un ZAP : gelé
    private Transform auraDisc;         // disque rose sous le slime pendant l'aura
    private bool auraShown;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        squashBaseScale = transform.localScale;

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

        TickAura(); // l'aura éclate les bulles autour du slime (si active)

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

        if (input.sqrMagnitude > 0.05f)
        {
            // Convertit l'input 2D (X = droite/gauche, Y = avant/arrière en vue 3e personne)
            // ForceMode.Acceleration : réactif quelle que soit la masse du Rigidbody.
            Vector3 force = new Vector3(input.x, 0f, input.y) * playerForce * PowerFactor;
            rb.AddForce(force, ForceMode.Acceleration);

            ApplySquash(input.sqrMagnitude);
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
        // (rend les bots "casual" — parfait pour un party game familial)
        float wobble = Mathf.PerlinNoise(Time.time * 3f, slimeIndex * 10f) - 0.5f;
        Vector3 perpendicular = Vector3.Cross(toTarget.normalized, Vector3.up);
        toTarget += perpendicular * (wobble * 1.5f);

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

    /// <summary>Bonus AURA : les bulles proches éclatent toutes seules.</summary>
    public void ApplyAura(float duration) { auraUntil = Time.time + duration; }

    /// <summary>Bonus ZAP reçu : ce slime est gelé pendant la durée.</summary>
    public void ApplyStun(float duration) { stunnedUntil = Time.time + duration; }

    /// <summary>×4 quand le bonus VITESSE est actif, sinon ×1.</summary>
    private float PowerFactor => Time.time < speedUntil ? 4f : 1f;

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
    /// AURA active : éclate toutes les bulles dans le rayon autour du slime.
    /// Chaque bulle apporte +1 au PROPRIÉTAIRE de l'aura (joueur ou bot).
    /// </summary>
    private void TickAura()
    {
        // gelé par un ZAP : l'aura s'arrête aussi
        bool active = Time.time < auraUntil && Time.time >= stunnedUntil;
        SetAuraVisual(active);
        if (!active) return;

        int bubbleLayer = LayerMask.NameToLayer("Bubble");
        int mask = bubbleLayer >= 0 ? (1 << bubbleLayer) : ~0;
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius, mask);
        foreach (Collider col in hits)
        {
            Bubble b = col.GetComponent<Bubble>();
            if (b != null) b.Pop(gameObject); // le score va à CE slime
        }
    }

    /// <summary>Grand disque rose lumineux sous le slime pendant l'aura.</summary>
    private void SetAuraVisual(bool show)
    {
        if (auraShown == show) return;
        auraShown = show;

        if (show && auraDisc == null)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(disc.GetComponent<CapsuleCollider>()); // jamais de collision fantôme
            disc.name = "AuraZone";
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            disc.transform.localScale = new Vector3(auraRadius * 2f, 0.015f, auraRadius * 2f);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            Color c = new Color(1f, 0.45f, 0.85f, 0.35f); // rose translucide
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            disc.GetComponent<MeshRenderer>().material = mat;
            auraDisc = disc.transform;
        }
        if (auraDisc != null) auraDisc.gameObject.SetActive(show);
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
        speedUntil = auraUntil = stunnedUntil = -1f;
        SetAuraVisual(false);
    }
}
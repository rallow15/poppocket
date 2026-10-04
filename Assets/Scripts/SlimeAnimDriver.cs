using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; // ReflectionProbeMode / RefreshMode / TimeSlicing / ClearFlags (Unity 6)
using Symphonie.StoreAssets;

/// <summary>
/// PONT VERS LE PACK « Cartoon Jelly Slime » (Symphonie).
/// Le pack a DÉJÀ ses propres mouvements (animations Idle/Walk/Run pilotées
/// par un Animator avec les paramètres ZSpeed / XSpeed) : ce script ne fait
/// QUE brancher notre physique dessus, il ne déforme rien lui-même.
///
/// • Lit la vitesse du Rigidbody (SlimeController) → Anime ZSpeed 0..2
///   (0 = immobile, 1 = marche, 2 = course) avec le lissage du démo du pack
///   (montée 0,2 s / descente 0,5 s).
/// • Tourne SEULEMENT le visuel enfant (VisualSlime) vers la direction du
///   mouvement — la racine reste droite (le Rigidbody gère la physique).
/// • Pose le composant du pack SlimeVisual (gelée du noyau) et applique la
///   couleur officielle du slime selon slimeIndex (0 Bleu, 1 Vert, 2 Jaune, 3 Rouge).
/// • Ajuste automatiquement la taille du blob pour coller à la sphère de
///   collision (l'import du FBX a une échelle inconnue).
/// </summary>
[DefaultExecutionOrder(-20)] // AVANT SlimeController : on taille les bots AVANT que
                             // SlimeController mémorise son squashBaseScale
                             // (sans ça, le squash rétrécissait les bots à chaque saut)
public class SlimeAnimDriver : MonoBehaviour
{
    [Header("Couleur")]
    [Tooltip("FALSE = look officiel du pack (gelée translucide avec cœur lumineux).\nTRUE = couleur pleine unie par slime (0 Bleu, 1 Vert, 2 Jaune, 3 Rouge).")]
    public bool utiliserCouleursPleines = false;

    [Header("Look du pack")]
    [Tooltip("Si le blob porte encore un vieux matériau plat (McSteeg), on lui remet automatiquement le matériau gelée du pack à l'exécution.")]
    public bool restaurerLookPack = true;

    // SÉCURITÉ anti-écrasement : Unity peut re-sauvegarder les préfabs avec
    // des vielles données (matériau plat McSteeg) pendant qu'il est ouvert.
    // On NE COMBATS PLUS dans les fichiers : le code remet le bon matériau
    // à chaque apparition d'un slime. Partagé entre tous les slimes.
    private static Material materiauPack;

    [Header("Look du pack — matériau officiel")]
    [Tooltip("Référence le matériau gelée OFFICIEL du pack (Symphonie/Slime/Materials/BuiltIn/Slime.mat). On en crée une copie EN MÉMOIRE par slime : l'asset du pack n'est jamais modifié.")]
    public Material materielPackOfficiel;

    [Header("Bots")]
    [Tooltip("TRUE = les bots portent la gelée du pack TEINTÉE (vert/jaune/rouge selon l'index). Le joueur garde la gelée bleue officielle du pack. FALSE = tout le monde en gelée bleue du pack.")]
    public bool botsEnCouleurs = true;
    [Tooltip("Taille des bots (2 = identique au joueur, qui est à l'échelle 2).")]
    public float tailleBots = 2f;

    [Header("Pilotage de l'animation du pack (ZSpeed/XSpeed)")]
    [Tooltip("Vitesse monde atteignant le WALK complet (ZSpeed=1). Calculée auto à partir de la force du slime si 0.")]
    public float vitesseMarche = 0f;
    [Tooltip("Vitesse monde atteignant le RUN complet (ZSpeed=2). Calculée auto si 0.")]
    public float vitesseCourse = 0f;

    [Header("Rotation du visuel")]
    [Tooltip("Vitesse de rotation du blob vers sa direction de marche")]
    public float vitesseRotation = 10f;

    [Header("Auto-ajustement à la sphère de collision")]
    [Tooltip("Le blob occupe 92% du diamètre de la sphère de collision")]
    public float margeBlob = 0.92f;

    private SlimeController controller;
    private Rigidbody rb;
    private Animator animateur;
    private Transform visuel;

    // Couleurs officielles (copiées des demo mats du pack)
    private static readonly Color[] Couleurs =
    {
        new Color(0.14509805f, 0.18133720f, 0.74509805f), // 0 : Bleu (joueur)
        new Color(0.14509805f, 0.74509805f, 0.22963527f), // 1 : Vert
        new Color(0.90964930f, 0.91509430f, 0.08201314f), // 2 : Jaune
        new Color(0.74509805f, 0.14509805f, 0.15476716f), // 3 : Rouge
    };

    private void Awake()
    {
        controller = GetComponent<SlimeController>();
        // SlimeController.Awake n'a PAS encore tourné (on est avant lui) :
        // on va chercher le Rigidbody nous-même.
        rb = GetComponent<Rigidbody>();

        // Bots à la même taille que le joueur (salim : « bots sont petits
        // par rapport à moi ») — on écrase l'échelle du préfab ICI à
        // l'exécution : plus fiable que d'éditer le fichier (Unity peut
        // re-sauvegarder le préfab avec ses vieilles données).
        if (tailleBots > 0f && controller.isPlayer == false)
        {
            transform.localScale = new Vector3(tailleBots, tailleBots, tailleBots);
        }

        visuel = transform.Find("VisualSlime");
        if (visuel == null)
        {
            Debug.LogWarning("[ANIM-DRIVER] Pas de visuel 'VisualSlime' sous " + name);
            return;
        }

        // L'Animator du pack est sur le FBX enfant
        animateur = visuel.GetComponentInChildren<Animator>(true);

        // Gelée du pack : pose le composant officiel SlimeVisual
        // (son Awake trouve tout seul les enfants "Core" et "Anchor_Center").
        SlimeVisual sv = visuel.GetComponentInChildren<SlimeVisual>(true);
        if (sv == null)
        {
            sv = visuel.gameObject.AddComponent<SlimeVisual>();
        }
        sv.EnableDynamics = true;

        // Couleur par slimeIndex (SpawnSlimes le règle via GameManager)
        int idx = Mathf.Clamp(controller.slimeIndex, 0, Couleurs.Length - 1);

        // JOUEUR : gelée bleue officielle du pack (OverrideColor = false
        // garde les dégradés + cœur lumineux du shader, pas de couleur pleine).
        // BOTS : gelée du pack TEINTÉE (OverrideColor = true met le _Color du
        // shader, qui reste gelée — les couleurs Couleurs[] viennent des
        // matériaux démo officiels du pack, pas de couleurs plates).
        bool estJoueur = controller.isPlayer;
        if (estJoueur)
        {
            // SHOP (salim 03/10) : si un skin est équipé dans la boutique, la
            // gelée du pack est TEINTÉE de la couleur du skin (OverrideColor =
            // true). Le look gelée officiel (reflets + cœur lumineux) reste
            // intact — c'est exactement la même teinte que les bots.
            // Sans skin équipé : gelée bleue officielle (OverrideColor = false).
            ShopManager.Skin skin = ShopManager.SkinEquipee();
            bool skinActif = skin != null && !skin.defaut;
            sv.OverrideColor = skinActif;
            if (skinActif) sv.SlimeColor = skin.couleur;
        }
        else
        {
            sv.OverrideColor = botsEnCouleurs;
            if (botsEnCouleurs)
            {
                // Le GameManager peut encore changer slimeIndex après
                // l'Instantiate : on relit aussi dans Start() au cas où.
                sv.SlimeColor = Couleurs[idx];
            }
        }

        // Look du pack : si le blob traîne un vieux matériau plat dans son
        // prefab, on le remet gelée ici (à l'exécution, invincible).
        // Les bots en couleur ont besoin d'un matériau À EUX (pas le
        // partagé) : SlimeVisual écrit le _Color dessus, et un matériau
        // partagé teinterait tous les slimes du coup.
        if (restaurerLookPack)
        {
            RestaurerLookDuPack(estJoueur == false && botsEnCouleurs);
        }
    }

    // La REFLECTION PROBE : le shader du pack calcule la réfraction du gel
    // en lisant l'environnement capturé par la probe de la scène. Les démos
    // du pack en ont une, MainScene NON → nos slimes rendaient ternes/plats.
    // Créée UNE fois (statique) au runtime, réglée comme celle de la démo.
    private static bool probeCreee;

    // FIX IPHONE (salim 04/10) : slimes NOIRS au 1er lancement puis normaux
    // après un Rejouer. La vraie cause : la ReflectionProbe, créée une seule
    // fois par lancement, capturait un environnement pas encore prêt (sol
    // d'eau, décor) → les slimes réfractaient du NOIR. Au Rejouer la probe
    // n'était PAS recréée (le statique restait vrai) → pas de reflets →
    // slimes normaux mais ternes. On remet le drapeau à zéro à CHAQUE
    // chargement de scène : la probe est refaite proprement à chaque partie,
    // une fois que le décor est en place.
    static SlimeAnimDriver()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded
            += (scene, mode) => { probeCreee = false; };
    }

    /// <summary>
    /// Remet le matériau gelée OFFICIEL du pack (shader « Symphonie/Slime »)
    /// sur le slot corps de chaque renderer du blob — mais SEULEMENT si ce
    /// renderer ne porte pas déjà ce shader.
    ///
    /// POURQUOI LE SLIME ÉTAIT TERNE EN JEU ALORS QU'IL EST « NET » EN DÉMO :
    ///   1. « new Material(shader) » part des DÉFAUTS du shader : PAS de LUT
    ///      de diffusion (texture blanche!), PAS de cœur activé (keyword
    ///      _SHOWCORE_ON jamais posé), et des réglages moins beaux
    ///      (Smoothness 1, RefractETA 0.85…) — maintenant on CLONE le
    ///      matériau officiel référencé ci-dessus, qui porte TOUT.
    ///   2. la réfraction lit la ReflectionProbe de la scène (absente de
    ///      MainScene, présente dans les démos) → on la crée au runtime.
    /// On ne touche jamais les assets du pack : uniquement des copies mémoire.
    /// </summary>
    private void RestaurerLookDuPack(bool unParSlime)
    {
        // Base = le matériau officiel du pack ; secours = défauts du shader.
        Material baseMat = materielPackOfficiel != null ? materielPackOfficiel : null;
        Shader sh = baseMat != null ? baseMat.shader : Shader.Find("Symphonie/Slime");
        if (sh == null)
        {
            Debug.LogWarning("[ANIM-DRIVER] Shader du pack « Symphonie/Slime » introuvable — look non restauré.");
            return;
        }

        CreeProbeScene();

        // Bots en couleur : UN matériau par slime (SlimeVisual écrit le
        // _Color dessus, un matériau partagé teinterait tout le monde).
        // Joueur (gelée bleue du pack) : UN SEUL matériau partagé par tous.
        Material mat;
        if (unParSlime)
        {
            // « new Material(matériau source) » copie shader + VALEURS + KEYWORDS
            // → le clone du matériau officiel porte la LUT, le cœur, tout.
            mat = baseMat != null ? new Material(baseMat) : new Material(sh);
            mat.name = "SlimePack_Gel_" + gameObject.name;
        }
        else
        {
            if (materiauPack == null)
            {
                materiauPack = baseMat != null ? new Material(baseMat) : new Material(sh);
                materiauPack.name = "SlimePack_Gel";
            }
            mat = materiauPack;
        }

        // SECOURS (référence officielle non assignée) : on part des défauts du
        // shader, donc on réactive à la main ce que le matériau officiel garde.
        if (baseMat == null)
        {
            mat.EnableKeyword("_SHOWCORE_ON");   // cœur lumineux (shader_feature!)
            mat.SetFloat("_Smoothness", 0.9f);
            mat.SetFloat("_RefractETA", 0.95f);
            mat.SetFloat("_SurfaceDiffuseWeight", 0.1f);
            mat.SetFloat("_CoreScatterScale", 0.01f);
        }

        if (unParSlime == false)
        {
            Debug.Log("[ANIM-DRIVER] Gel du pack reconstruit à partir de " +
                      (materielPackOfficiel != null ? "Slime.mat OFFICIEL" : "défauts shader (LUT/cœur approx)"));
        }

        foreach (Renderer r in visuel.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            if (mats[0] != null && mats[0].shader == sh) continue; // déjà le pack

            var remplace = (Material[])mats.Clone();
            remplace[0] = mat;   // slot corps → matériau gelée du pack
            r.sharedMaterials = remplace;
        }
    }

    /// <summary>
    /// Crée (UNE fois par partie) une ReflectionProbe au centre de l'arène,
    /// copiée sur celle des démos du pack : le shader du pack y lit
    /// l'environnement pour la réfraction et les reflets du gel.
    /// Capture en temps réel mais ONZE : au démarrage, puis 1 s après
    /// (quand bulles et décor sont en place) — pas chaque image, trop cher.
    /// </summary>
    private static void CreeProbeScene()
    {
        if (probeCreee) return;
        probeCreee = true;

        if (Object.FindFirstObjectByType<ReflectionProbe>() != null) return; // déjà une

        GameObject go = new GameObject("ProbeEnvironnement");
        ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
        probe.transform.position = new Vector3(0f, 4f, 0f);   // comme la démo
        // Pas de bake possible en runtime : temps réel, capture par script.
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.size = new Vector3(60f, 20f, 60f);   // couvre l'arène
        probe.nearClipPlane = 0.3f;
        probe.farClipPlane = 1000f;
        probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        probe.cullingMask = -1;
        probe.resolution = 128;                // idem démo
        probe.intensity = 1f;
        probe.blendDistance = 1f;
        probe.boxProjection = true;
        probe.hdr = true;          // « HDR » renommé en minuscule (Unity 6)
        probe.importance = 1;      // idem (Unity 6)
        // FIX IPHONE 04/10 : on ne capture PLUS tout de suite. Juste après la
        // création, RenderProbe() sur le téléphone renvoyait un cubemap NOIR
        // (frame pas prête sur Metal) → les slimes réfractaient du noir et
        // restaient sombres. Les captures se font maintenant un peu après :
        //  1re à ~0,4 s (décor posé) — d'ici là le shader utilise l'ambiance
        //  par défaut (le même rendu qu'avant le Rejouer, donc correct),
        //  2e à ~1,2 s (bulles + bulles d'eau posées).
        RafraichirPlusTard runner = go.AddComponent<RafraichirPlusTard>();
        runner.Lancer();
    }

    /// <summary>Porteur des captures différées de la probe.</summary>
    private sealed class RafraichirPlusTard : MonoBehaviour
    {
        public void Lancer() { StartCoroutine(RafraichirCoro()); }
        private System.Collections.IEnumerator RafraichirCoro()
        {
            yield return new WaitForSeconds(0.4f);
            ReflectionProbe probe = GetComponent<ReflectionProbe>();
            if (probe != null) probe.RenderProbe();
            yield return new WaitForSeconds(0.8f);
            if (probe != null) probe.RenderProbe();   // décor + bulles posés → capture propre
        }
    }

    private void Start()
    {
        // Couleur finale : SpawnSlimes (coroutine) a pu poser slimeIndex
        // entre l'Instantiate et le Start → on relit ici pour être sûr.
        if (controller.isPlayer == false && botsEnCouleurs && visuel != null)
        {
            SlimeVisual sv = visuel.GetComponentInChildren<SlimeVisual>(true);
            if (sv != null)
                sv.SlimeColor = Couleurs[Mathf.Clamp(controller.slimeIndex, 0, Couleurs.Length - 1)];
        }

        // SHOP : même sécurité pour le JOUEUR — on (re)pose son skin équipé.
        if (controller.isPlayer && visuel != null)
        {
            SlimeVisual sv = visuel.GetComponentInChildren<SlimeVisual>(true);
            if (sv != null)
            {
                ShopManager.Skin skin = ShopManager.SkinEquipee();
                bool skinActif = skin != null && !skin.defaut;
                sv.OverrideColor = skinActif;
                if (skinActif) sv.SlimeColor = skin.couleur;
            }
        }

        // Vitesses d'animation déduites de la physique du slime :
        // vitesse terminale ≈ accélération / damping (damping Rigidbody = 8 ici)
        float damping = rb != null ? rb.linearDamping : 8f;
        if (damping < 0.5f) damping = 0.5f;
        float terminal = (controller.isPlayer ? controller.playerForce : controller.botForce) / damping;
        if (vitesseMarche <= 0f) vitesseMarche = terminal * 0.35f;  // ~marche
        if (vitesseCourse <= 0f) vitesseCourse = terminal * 0.95f;  // ~course

        StartCoroutine(AutoFit());
    }

    // CLEAN FPS (salim 05/10) : les paramètres de l'animator sont cherchés
    // par UN ID NUMÉRIQUE (hash calculé une fois en mémoire) au lieu d'une
    // recherche par TEXTE à chaque image, sur chaque slime.
    private static readonly int hashZSpeed = Animator.StringToHash("ZSpeed");
    private static readonly int hashXSpeed = Animator.StringToHash("XSpeed");

    private void Update()
    {
        if (visuel == null) return;

        // ---- ZSpeed / XSpeed du pack ----
        Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
        v.y = 0f;
        float vitesse = v.magnitude;

        // Cartographie : 0 → ZSpeed 0 (idle) ; vitesseMarche → 1 (walk) ;
        // vitesseCourse → 2 (run). Linéaire par morceaux, comme le démo du pack.
        float z;
        if (vitesse <= vitesseMarche)
            z = vitesseMarche > 0f ? (vitesse / vitesseMarche) * 1f : 0f;
        else
            z = 1f + Mathf.Clamp01((vitesse - vitesseMarche) /
                                   Mathf.Max(0.01f, vitesseCourse - vitesseMarche));

        // Lissage du démo du pack : accélère vite (0,2 s), freine doucement (0,5 s)
        float courant = animateur.GetFloat(hashZSpeed);
        float amorti = courant > z ? 0.5f : 0.2f;
        animateur.SetFloat(hashZSpeed, z, amorti, Time.deltaTime);
        animateur.SetFloat(hashXSpeed, 0f); // locomotion "avant" : on oriente le blob

        // ---- Rotation du visuel vers la direction du mouvement ----
        if (vitesse > 0.4f && visuel != null)
        {
            Quaternion cible = Quaternion.LookRotation(v.normalized, Vector3.up);
            visuel.rotation = Quaternion.Slerp(visuel.rotation, cible, vitesseRotation * Time.deltaTime);
        }
    }

    /// <summary>
    /// Ajuste la taille/position du blob pour qu'il colle à la sphère de
    /// collision (l'échelle d'import du FBX du pack est inconnue).
    /// </summary>
    private IEnumerator AutoFit()
    {
        yield return null; // laisse le pack s'instancier et poser ses LODs
        if (visuel == null) yield break;

        SphereCollider sph = GetComponent<SphereCollider>();
        Renderer r = visuel.GetComponentInChildren<Renderer>();
        if (sph == null || r == null) yield break;

        // ---- Échelle : le blob occupe "margeBlob" du diamètre de la sphère ----
        Vector3 taille = r.bounds.size;
        float actuel = Mathf.Max(Mathf.Max(taille.x, taille.y), taille.z);
        float cible = (sph.radius * 2f) * Mathf.Abs(transform.lossyScale.x) * margeBlob;
        if (actuel > 0.001f && cible > 0.001f)
        {
            float facteur = Mathf.Clamp(cible / actuel, 0.25f, 8f);
            visuel.localScale *= facteur;
        }

        // ---- Position : centre le blob, pied posé au niveau du bas de la sphère ----
        yield return null;
        r = visuel.GetComponentInChildren<Renderer>();
        if (r == null) yield break;
        Bounds b = r.bounds;
        float rayonBlob = (b.extents.x + b.extents.y + b.extents.z) / 3f;
        SphereCollider sph2 = GetComponent<SphereCollider>();
        if (sph2 == null) yield break;
        Bounds sb = sph2.bounds;
        Vector3 voulu = new Vector3(sb.center.x, sb.min.y + rayonBlob * 0.55f, sb.center.z);
        visuel.position += voulu - b.center;
    }
}
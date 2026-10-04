using System.Collections;
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════
//  ÉCLAT D'EAU quand une bulle éclate (salim 04/10 : « quand slime
//  éclate bulle d'eau j'aimerai un éclat d'eau, là c'est un éclat de
//  pixel »).
//
//  L'ancien éclat = le prefab de particules du pack : des CARRÉS de
//  pixels qui sautent. Ici, deux morceaux 100 % en code (textures
//  générées, matériaux en mémoire, aucun fichier sur disque, Built-in) :
//
//    1. LES GOUTTES : un ParticleSystem de gouttes d'eau qui giclent vers
//       le haut puis retombent (gravité). Le « carré » venait de la
//       texture par défaut du composant ParticleSystem → ici la texture
//       de la goutte est un dégradé ROND généré en code, et le renderer
//       est en mode Stretch : la goutte S'ALLONGE dans son mouvement,
//       comme une vraie goutte qui vole.
//
//    2. LA VAGUELETTE : un anneau qui s'étend AU SOL (l'eau qui s'épand
//       à l'endroit où la bulle a éclaté) et s'efface — la même technique
//       que l'ombre des bulles (ArenaDesign.MatOmbreBulle).
//
//  Appelé par Bubble.Pop() : EclatEau.Cree(transform.position). Ça marche
//  même si aucun prefab de particules n'est branché dans l'éditeur.
// ══════════════════════════════════════════════════════════════════════
public class EclatEau : MonoBehaviour
{
    /// <summary>
    /// Couleur de LA bulle qui éclate (posée au moment de la création par
    /// Bubble.Pop via Cree / ReparePack). salim 04/10 : « la même couleur
    /// que les bulles à éclater » → chaque éclat teinté par SA bulle.
    /// </summary>
    private Color teinteEau = new Color(0.62f, 0.90f, 0.98f);

    /// <summary>Fait gicler un éclat d'eau au point donné (lancé par Bubble.Pop).</summary>
    public static void Cree(Vector3 posDuPop, Color teinte)
    {
        GameObject go = new GameObject("EclatEau");
        go.transform.position = posDuPop;
        EclatEau eclat = go.AddComponent<EclatEau>();
        eclat.teinteEau = teinte;    // la goutte prend la tête de la bulle
    }

    // ── les 2 textures, générées une seule fois puis réutilisées ─────
    private static Texture2D texGoutte, texVaguelette;

    // ── le PACK NamuFX (salim 04/10) ──────────────────────────────
    // Le pack est 100 % shadergraph (URP en pratique) → tout rend
    // ROSE en Built-in. Solution : on garde les ANIMATIONS de son
    // prefab, mais on remplace ses matériaux par « Sprites/Default »
    // (shader Built-in) + 2 PETITS DÉCOUPAGES de sa texture (petits
    // PNG copiés dans Resources/Effets — l'original du pack n'est
    // JAMAIS touché, règle du projet). Texture en mémoire une seule fois.
    private static Material matBoulePack;

    // CLEAN FPS (salim 05/10) : plus de « new Material » à CHAQUE pop
    // (2-3 matériaux jetés à la poubelle mémoire par pop × 400 pops/round
    // = des sacs de GC en pleine partie).
    //  → le shader est cherché UNE seule fois,
    //  → le matériau des gouttes (jamais animé) est PARTAGÉ,
    //  → les matériaux des 2 quads animés (vaguelette / éclat) sont
    //    EMPRUNTÉS dans un petit pool puis RENDUS à la fin de leur
    //    animation → zéro création pendant le jeu.
    private static Shader shSprites;
    private static Material matGoutte;
    private static readonly System.Collections.Generic.Queue<Material> poolVague
        = new System.Collections.Generic.Queue<Material>();
    private static readonly System.Collections.Generic.Queue<Material> poolEclat
        = new System.Collections.Generic.Queue<Material>();

    /// <summary>Cherche le shader une fois, puis le garde (Shader.Find = cherchette à chaque appel).</summary>
    private static Shader SpritesShader()
    {
        if (shSprites == null) shSprites = Shader.Find("Sprites/Default");
        return shSprites;
    }

    /// <summary>Prend un matériau au pool (créé UNE fois), avec la texture et la queue demandées.</summary>
    private static Material Emprunter(System.Collections.Generic.Queue<Material> pool,
                                      Texture2D tex, int renderQueue)
    {
        if (pool.Count > 0) return pool.Dequeue();          // réutilisé tel quel
        var m = new Material(SpritesShader());
        m.mainTexture = tex;
        m.renderQueue = renderQueue;
        return m;
    }

    /// <summary>
    /// Répare un prefab du pack NamuFX : son shader ne marche pas en
    /// Built-in (matériau ROSE). On pose un shader Built-in + le dessin
    /// du pack sur TOUTES ses particules — les animations restent celles
    /// du pack. (BubblesBurst : 0,15-0,4 m, mode Billboard — compatible.)
    /// </summary>
    public static void ReparePack(GameObject burst, Color teinte)
    {
        var tex = Resources.Load<Texture2D>("Effets/BouleEau");
        if (tex == null) return;                    // PNG absent : on ne casse rien

        if (matBoulePack == null)
        {
            matBoulePack = new Material(Shader.Find("Sprites/Default"));
            matBoulePack.mainTexture = tex;
            matBoulePack.renderQueue = 2998;                    // après le sol
        }

        // LA teinte de la bulle qui éclate. On re-teinte à chaque pop :
        // les bursts durent 1 s et les 3 teintes du spawner sont déjà
        // très proches, un matériau partagé ne se voit pas.
        // ATTENTION : l'alpha de la bulle est de 0.42 (pour voir à travers)
        // → avec cet alpha, la goutte serait presque invisible. On force 0.9.
        Color c = teinte; c.a = 0.90f;
        matBoulePack.color = c;
        foreach (var r in burst.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            r.material = matBoulePack;
            // goutte qui TOURNE un peu : plus naturel, moins « copié-collé »
            if (r.renderMode == ParticleSystemRenderMode.Billboard)
                r.sortingFudge = 5f;         // les gouttes devant le flash au sol
        }
        foreach (var p in burst.GetComponentsInChildren<ParticleSystem>(true))
        {
            // TRÈS IMPORTANT : le pack découpe sa texture en une grille de
            // 8x8 cases (« feuille d'images »). Sur son planche 4096 ça montre
            // le bon dessin, mais sur notre petit blob doux chaque goutte ne
            // voit qu'UNE case = presque une couleur pleine = des CARRÉS.
            // On coupe le module, chaque goutte montre la texture ENTIÈRE.
            var feuille = p.textureSheetAnimation;
            feuille.enabled = false;

            var rot = p.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.75f, 0.75f);   // rad/s
        }
    }

    private static Texture2D TexGoutte()
    {
        if (texGoutte != null) return texGoutte;
        const int S = 32;
        var t = new Texture2D(S, S, TextureFormat.ARGB32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[S * S];
        float c = (S - 1) * 0.5f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / c;
                float a = 1f - r;
                a *= a;                                             // bord doux
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply();
        texGoutte = t;
        return t;
    }

    private static Texture2D TexVaguelette()
    {
        if (texVaguelette != null) return texVaguelette;
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.ARGB32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[S * S];
        float c = (S - 1) * 0.5f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / c;
                // un ANNEAU : il apparaît vers 40 % du rayon et se fond
                // doucement vers le bord (comme une onde sur l'eau)
                float a = Mathf.Clamp01((r - 0.40f) * 6f)
                        * (1f - Mathf.Clamp01((r - 0.72f) * 4f));
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply();
        texVaguelette = t;
        return t;
    }

    private void Start()
    {
        // ── 1) LES GOUTTES qui giclent ───────────────────────────────
        var ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.9f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.17f);
        // 2 gouttonnés de LA teinte de la bulle (légèrement éclaircie /
        // assombrie pour du relief, comme les vrais morceaux d'eau)
        Color claire = teinteEau; claire.a = 0.90f;
        Color foncee = Color.Lerp(teinteEau, Color.white, 0.5f); foncee.a = 0.90f;
        main.startColor = new ParticleSystem.MinMaxGradient(claire, foncee);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;  // s'auto-nettoie

        // tout part d'UN coup : un burst de 22 gouttes, et rien après
        var em = ps.emission;
        em.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

        // éventail large vers le haut (le cône crache le long de +Z,
        // donc on tourne le nez du transform vers le CIEL)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle  = 55f;
        shape.radius = 0.12f;
        transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        // renderer : texture RONDE en code (pas le carré d'Unity) + mode
        // Stretch → la goutte s'étire dans sa course, vrai look d'eau
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        // CLEAN FPS : matériau des gouttes créé UNE fois puis réutilisé
        // à chaque pop (il n'est jamais animé, il peut être partagé).
        if (matGoutte == null)
        {
            matGoutte = new Material(SpritesShader());
            matGoutte.mainTexture = TexGoutte();
        }
        rend.material = matGoutte;
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale  = 1.2f;
        rend.velocityScale = 0.30f;

        // ── 2) LA VAGUELETTE au sol (l'eau qui s'épand) ──────────────
        // posée sur le SOL sous le point du pop (raycast vers le bas) —
        // pas sur la hauteur du centre de la bulle, sinon elle flotte.
        float ySol = transform.position.y - 0.18f;
        if (Physics.Raycast(transform.position + Vector3.up * 0.3f,
                            Vector3.down, out RaycastHit hit, 4f))
            ySol = hit.point.y + 0.025f;

        GameObject vague = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(vague.GetComponent<Collider>());   // ne gêne pas le slime
        vague.name = "Vaguelette";
        vague.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        vague.transform.position = new Vector3(transform.position.x, ySol, transform.position.z);
        vague.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        Object.Destroy(vague, 1.0f);                      // nettoyage de sécurité

        // CLEAN FPS : on EMPRUNTE un matériau du pool au lieu d'en créer
        // un neuf à chaque pop (il est rendu au pool à la fin de l'anim).
        var matVague = Emprunter(poolVague, TexVaguelette(), 2995);
        vague.GetComponent<Renderer>().material = matVague;

        StartCoroutine(AnimerVaguelette(vague.transform, matVague));

        // ── 3) LE FLASH DE L'ÉCLAT : le dessin du PACK (grosse éclaboussure
        //    d'eau bleue posée au sol, S'ÉTEND et S'EFFACE très vite) ──
        var texEclat = Resources.Load<Texture2D>("Effets/EclatBulle");
        if (texEclat != null)
        {
            GameObject eclat = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(eclat.GetComponent<Collider>());
            eclat.name = "EclatDuPack";
            eclat.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            eclat.transform.position = new Vector3(transform.position.x, ySol + 0.014f, transform.position.z);
            eclat.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            // CLEAN FPS : idem, matériau EMPRUNTÉ au pool puis rendu.
            var matEclat = Emprunter(poolEclat, texEclat, 2996);
            eclat.GetComponent<Renderer>().material = matEclat;
            Object.Destroy(eclat, 0.7f);          // nettoyage de sécurité
            StartCoroutine(AnimerEclat(eclat.transform, matEclat));
        }
    }

    /// <summary>Le flash du pack S'ÉTEND (0.55 → 1.8 m) et S'EFFACE en 0,35 s.</summary>
    private IEnumerator AnimerEclat(Transform eclat, Material mat)
    {
        float duree = 0.35f, t = 0f;
        while (t < duree)
        {
            t += Time.deltaTime;
            float k = t / duree;
            float s = Mathf.Lerp(0.55f, 1.8f, k);
            eclat.localScale = new Vector3(s, s, 1f);
            Color c = Color.Lerp(teinteEau, Color.white, 0.5f);   // teinté SA bulle
            c.a = 1f - k;                        // s'efface en douceur
            mat.color = c;
            yield return null;
        }
        poolEclat.Enqueue(mat);              // CLEAN FPS : rendu au pool
    }

    /// <summary>La vaguelette S'ÉTEND (0.5 → 1.6 m) et S'EFFACE en 0,45 s.</summary>
    private IEnumerator AnimerVaguelette(Transform vague, Material mat)
    {
        Color eau = teinteEau;                        // la couleur de SA bulle
        float duree = 0.45f, t = 0f;
        float tailleDebut = 0.6f, tailleFin = 1.6f;

        while (t < duree)
        {
            t += Time.deltaTime;
            float k = t / duree;
            float s = Mathf.Lerp(tailleDebut, tailleFin, k);
            vague.localScale = new Vector3(s, s, 1f);
            eau.a = 0.75f * (1f - k);                 // disparaît en douceur
            mat.color = eau;
            yield return null;
        }
        poolVague.Enqueue(mat);              // CLEAN FPS : rendu au pool
    }
}
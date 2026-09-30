#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace PopPocket.EditorTools
{
    /// <summary>
    /// Construit TOUTE la scène PopPocket automatiquement.
    /// Dans Unity : menu POPPOCKET ▸ 1. Créer la scène complète
    ///                    POPPOCKET ▸ 2. Créer préfab slimes + bulles
    /// Rien à faire à la main avant d'appuyer sur Play.
    /// </summary>
    public static class PopPocketSceneSetup
    {
        // ─────────────────────────────────────────────────
        //  ÉTAPE 1 : scène complète
        // ─────────────────────────────────────────────────
        [MenuItem("POPPOCKET/1. Créer la scène complète", priority = 0)]
        public static void SetupFullScene()
        {
            // ---------- 1. ARÈNE (Plane 30 x 30) ----------
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
                arena.name = "Arena";
                arena.transform.position = Vector3.zero;
                arena.transform.localScale = new Vector3(3f, 1f, 3f); // plane = 10m → 30x30
            }

            // ---------- 2. CAMÉRA ----------
            GameObject camGO = null;
            Camera cam = null;
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.CompareTag("MainCamera")) { cam = c; camGO = c.gameObject; break; }
            if (camGO == null)
            {
                camGO = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
                camGO.tag = "MainCamera";
            }
            if (camGO.GetComponent<CameraFollow>() == null)
                camGO.AddComponent<CameraFollow>();
            var cf = camGO.GetComponent<CameraFollow>();
            // Caméra 3e personne : les réglages vivent dans le script CameraFollow
            camGO.transform.position = new Vector3(0f, 4.5f, -8.5f);

            // ---------- 3. AUDIO MANAGER (objet séparé) ----------
            GameObject audioGO = GameObject.Find("AudioManager");
            if (audioGO == null)
            {
                audioGO = new GameObject("AudioManager");
                audioGO.AddComponent<AudioManager>();
            }

            // ---------- 4. GAME MANAGER + SPAWNER ----------
            GameObject gmGO = GameObject.Find("GameManager") ?? new GameObject("GameManager");
            var gm = gmGO.GetComponent<GameManager>() ?? gmGO.AddComponent<GameManager>();
            var spawner = gmGO.GetComponent<BubbleSpawner>() ?? gmGO.AddComponent<BubbleSpawner>();
            // Tapis de bulles façon Roblox (RobloxStyleRebuild ajuste tout au lancement)
            spawner.arenaSize = new Vector2(30f, 30f);
            spawner.bubblesPerSide = 20;

            // ---------- 5. SPAWN POINTS ----------
            gm.startPositions = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject sp = GameObject.Find("Spawn" + i) ?? new GameObject("Spawn" + i);
                float x = (i % 2 == 0) ? -8f : 8f;
                float z = (i < 2) ? -8f : 8f;
                sp.transform.position = new Vector3(x, 1.5f, z);
                gm.startPositions[i] = sp.transform;
            }

            // ---------- 6. ORIENTATION MANAGER ----------
            if (GameObject.Find("OrientationManager") == null)
            {
                var omGO = new GameObject("OrientationManager");
                omGO.AddComponent<OrientationManager>();
            }

            // ---------- 7. EVENT SYSTEM (obligatoire pour joystick / boutons) ----------
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // ---------- 8. CANVAS ----------
            GameObject canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("GameCanvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Timer en haut au centre
            TMP_Text timerText = CreateTMPText(canvasGO.transform, "TimerText", "60",
                72, Color.white, new Vector2(0.5f, 1f), new Vector2(220, 80),
                new Vector2(0, -40));

            // Scores empilés en haut à droite
            GameObject scoreGO = GameObject.Find("ScoreBlock") ??
                new GameObject("ScoreBlock", typeof(RectTransform));
            scoreGO.transform.SetParent(canvasGO.transform, false);
            RectTransform srt = scoreGO.GetComponent<RectTransform>() ??
                scoreGO.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(1f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(-30, -30);
            srt.sizeDelta = new Vector2(220, 220);

            var vl = scoreGO.GetComponent<VerticalLayoutGroup>() ??
                     scoreGO.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.spacing = 4;
            vl.childControlWidth = true;
            vl.childControlHeight = false;

            TMP_Text[] scoreTexts = new TMP_Text[4];
            string[] labels = { "TOI", "Bot 1", "Bot 2", "Bot 3" };
            for (int i = 0; i < 4; i++)
            {
                scoreTexts[i] = CreateTMPText(scoreGO.transform,
                    "ScoreText_" + i, labels[i] + " : 0", 36, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(220, 50), Vector2.zero);
                scoreTexts[i].alignment = TextAlignmentOptions.Right;
                scoreTexts[i].color = (i == 0) ? new Color(0.2f, 0.6f, 1f)
                    : (i == 1) ? new Color(1f, 0.35f, 0.35f)
                    : (i == 2) ? new Color(0.3f, 1f, 0.4f)
                    : new Color(1f, 0.85f, 0.2f);
            }

            // ---------- 9. PANNEAU FIN DE ROUND ----------
            GameObject roundPanel = CreateOverlayPanel(canvasGO, "RoundEndPanel");
            TMP_Text roundTitle = CreateTMPText(roundPanel.transform, "RoundEndTitle",
                "FIN DU ROUND 1", 80, Color.white,
                new Vector2(0.5f, 0.65f), new Vector2(900, 120), Vector2.zero);
            TMP_Text roundScores = CreateTMPText(roundPanel.transform, "RoundEndScores",
                "Toi : 0\nBot 1 : 0\nBot 2 : 0\nBot 3 : 0", 48, Color.white,
                new Vector2(0.5f, 0.35f), new Vector2(700, 400), Vector2.zero);
            roundPanel.SetActive(false);

            // ---------- 10. PANNEAU GAGNANT + BOUTON REJOUER ----------
            GameObject winPanel = CreateOverlayPanel(canvasGO, "WinnerPanel");
            TMP_Text winTitle = CreateTMPText(winPanel.transform, "WinnerTitle",
                "TOI GAGNE !", 88, new Color(1f, 0.85f, 0.2f),
                new Vector2(0.5f, 0.62f), new Vector2(900, 140), Vector2.zero);
            TMP_Text winScore = CreateTMPText(winPanel.transform, "WinnerScore",
                "0 points", 56, Color.white,
                new Vector2(0.5f, 0.48f), new Vector2(700, 90), Vector2.zero);
            Button replay = CreateButton(winPanel.transform, "ReplayButton", "REJOUER",
                new Vector2(0.5f, 0.25f), new Vector2(420, 120));
            winPanel.SetActive(false);

            // ---------- 11. CONTRÔLE AU DOIGT (aucun bouton à l'écran !) ----------
            // Drag tactile : le doigt glisse n'importe où sur l'écran.
            if (GameObject.Find("TouchInput") == null)
            {
                var dragGO = new GameObject("TouchInput");
                dragGO.AddComponent<ScreenDragInput>();
            }
            gm.playerJoystick = GameObject.Find("TouchInput")
                .GetComponent<ScreenDragInput>();

            // ---------- 12. GAMEUI (relie tout) ----------
            GameObject uiHolder = GameObject.Find("GameUIHolder") ??
                                   new GameObject("GameUIHolder", typeof(RectTransform));
            uiHolder.transform.SetParent(canvasGO.transform, false);
            var gameUI = uiHolder.GetComponent<GameUI>() ?? uiHolder.AddComponent<GameUI>();
            gameUI.timerText = timerText;
            gameUI.scoreTexts = scoreTexts;
            gameUI.roundEndPanel = roundPanel;
            gameUI.roundEndTitleText = roundTitle;
            gameUI.roundEndScoresText = roundScores;
            gameUI.winnerPanel = winPanel;
            gameUI.winnerTitleText = winTitle;
            gameUI.winnerScoreText = winScore;
            gameUI.replayButton = replay;

            gm.gameCamera = cf;
            gm.spawner = spawner;
            gm.gameUI = gameUI;

            // ---------- 13. SAUVEGARDE ----------
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            string scenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
                scenePath = "Assets/Scenes/MainScene.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);

            EditorUtility.DisplayDialog("PopPocket Setup", "Scène créée et sauvegardée !\n\n" +
                "Étape suivante : POPPOCKET ▸ 2. Créer préfab slimes + bulles", "OK");
        }

        // ─────────────────────────────────────────────────
        //  ÉTAPE 2 : préfabs slimes + bulles + particules
        // ─────────────────────────────────────────────────
        [MenuItem("POPPOCKET/2. Créer préfab slimes + bulles", priority = 1)]
        public static void SetupPrefabsPlaceholders()
        {
            EnsureTag("Slime");
            int bubbleLayer = EnsureLayer("Bubble");

            System.IO.Directory.CreateDirectory("Assets/Prefabs");

            // ---------- SLIMES (sphères colorées qui rebondissent) ----------
            CreateSlimePrefab(isPlayer: true);
            CreateSlimePrefab(isPlayer: false);

            // ---------- PARTICULES "POP" (créé AVANT la bulle, pour le branchement) ----------
            GameObject psGO = new GameObject("PopBurstPrefab");
            var ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startSpeed = 4f;
            main.startSize = 0.14f;
            main.startLifetime = 0.7f;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.yellow,   0f),
                    new GradientColorKey(Color.cyan,     0.33f),
                    new GradientColorKey(Color.magenta,  0.66f),
                    new GradientColorKey(Color.white,    1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            var startColor = new ParticleSystem.MinMaxGradient(grad);
            startColor.mode = ParticleSystemGradientMode.RandomColor;
            main.startColor = startColor;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            psGO.AddComponent<ParticleBurst>();
            PrefabUtility.SaveAsPrefabAsset(psGO, "Assets/Prefabs/PopBurstPrefab.prefab");
            Object.DestroyImmediate(psGO);

            // ---------- BULLE ----------
            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "BubblePrefab";
            bubble.transform.localScale = Vector3.one * 0.9f;
            Material bmat = CreateSimpleMaterial(new Color(0.9f, 0.9f, 0.95f));
            bubble.GetComponent<MeshRenderer>().sharedMaterial = bmat;

            var brb = bubble.AddComponent<Rigidbody>();
            brb.mass = 0.4f;
            brb.interpolation = RigidbodyInterpolation.Interpolate;

            PhysicsMaterial pbm = new PhysicsMaterial("BubbleBounce");
            pbm.bounciness = 0.6f;
            pbm.dynamicFriction = 0.05f;
            pbm.bounceCombine = PhysicsMaterialCombine.Maximum;
            bubble.GetComponent<SphereCollider>().material = pbm;

            var bubbleScript = bubble.AddComponent<Bubble>();
            bubbleScript.minImpactVelocity = 2.0f;

            // Sons pop générés (Assets/Sounds/Pop1-3.wav) : branchement automatique
            AudioClip[] clips = new AudioClip[3] {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Pop1.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Pop2.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Pop3.wav")
            };
            if (clips[0] != null)
                bubbleScript.popSounds = clips;

            // Particules branchées automatiquement
            bubbleScript.popParticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/PopBurstPrefab.prefab");

            // Layer Bubble sur tout l'objet
            SetLayerRecursive(bubble, bubbleLayer);

            PrefabUtility.SaveAsPrefabAsset(bubble, "Assets/Prefabs/BubblePrefab.prefab");
            Object.DestroyImmediate(bubble);

            // ---------- BRANCHEMENT AUTO DU GAME MANAGER ----------
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.playerSlimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/PlayerSlimePrefab.prefab");
                gm.botSlimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/BotSlimePrefab.prefab");
                if (gm.spawner != null)
                    gm.spawner.bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Prefabs/BubblePrefab.prefab");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("PopPocket Prefabs",
                "3 préfabs créés dans Assets/Prefabs :\n" +
                "• PlayerSlimePrefab (bleu)\n• BotSlimePrefab (vert)\n• BubblePrefab\n\n" +
                "Sons + particules branchés automatiquement ✔\n\n" +
                "APPUIE SUR PLAY ! 🫧", "OK");
        }

        // ─────────────────────────────────────────────────
        //  OUTILS
        // ─────────────────────────────────────────────────
        /// <summary>Crée un préfab slime placeholder (sphère) joueur ou bot.</summary>
        private static void CreateSlimePrefab(bool isPlayer)
        {
            string name = isPlayer ? "PlayerSlimePrefab" : "BotSlimePrefab";
            GameObject slime = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            slime.name = name;
            slime.transform.localScale = Vector3.one * 1.4f;

            Material mat = CreateSimpleMaterial(isPlayer
                ? new Color(0.25f, 0.6f, 1f)
                : new Color(0.3f, 1f, 0.45f));
            slime.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var rb = slime.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            PhysicsMaterial pm = new PhysicsMaterial("SlimeBounce");
            pm.bounciness = 0.8f;
            pm.dynamicFriction = 0.1f;
            pm.staticFriction = 0.1f;
            pm.bounceCombine = PhysicsMaterialCombine.Maximum;
            pm.frictionCombine = PhysicsMaterialCombine.Maximum;
            slime.GetComponent<SphereCollider>().material = pm;

            var sc = slime.AddComponent<SlimeController>();
            sc.isPlayer = isPlayer;

            slime.tag = "Slime";

            PrefabUtility.SaveAsPrefabAsset(slime, "Assets/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(slime);
        }

        /// <summary>Material simple compatible Built-in OU URP.</summary>
        private static Material CreateSimpleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }

        /// <summary>Panneau plein écran semi-transparent.</summary>
        private static GameObject CreateOverlayPanel(GameObject canvas, string name)
        {
            GameObject panel = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = panel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.78f);
            return panel;
        }

        /// <summary>Texte TextMeshPro centré, ancré.</summary>
        private static TMP_Text CreateTMPText(Transform parent, string name, string text,
            float size, Color color, Vector2 anchor, Vector2 sizeDelta, Vector2 pos)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold;
            return txt;
        }

        /// <summary>Bouton UI simple avec libellé.</summary>
        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchor, Vector2 size)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.95f, 0.6f, 0.15f); // orange

            TMP_Text txt = CreateTMPText(go.transform, "Label", label, 44, Color.white,
                new Vector2(0.5f, 0.5f), size, Vector2.zero);
            txt.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        /// <summary>Crée le tag s'il n'existe pas (sinon erreur à l'assignation).</summary>
        private static void EnsureTag(string tag)
        {
            SerializedObject tagMgr = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagMgr.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            int idx = tags.arraySize;
            tags.arraySize++;
            tags.GetArrayElementAtIndex(idx).stringValue = tag;
            tagMgr.ApplyModifiedProperties();
        }

        /// <summary>Crée le layer s'il n'existe pas. Renvoie son index.</summary>
        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing != -1) return existing;

            SerializedObject tagMgr = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagMgr.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagMgr.ApplyModifiedProperties();
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Applique un layer à un GameObject et tous ses enfants.</summary>
        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}
#endif
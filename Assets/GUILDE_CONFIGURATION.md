# 🎮 PopPocket — Guide de configuration Unity pas-à-pas

> **But** : assembler le jeu "éclate les bulles" avec les 4 slimes en 30 minutes.
> Scripts fournis (dans `Assets/Scripts/`) : `Bubble.cs`, `SlimeController.cs`,
> `GameManager.cs`, `BubbleSpawner.cs`, `CameraFollow.cs`, `OrientationManager.cs`,
> `GameUI.cs`, `AudioManager.cs`, `ParticleBurst.cs`.

---

## 0) Création du projet

1. **Unity Hub → New Project → 3D (Core)** — Template **"Universal 3D"** ou **"3D (URP)"**.
2. Nom : `PopPocket`. Plateforme cible : **Android** (File → Build Profiles → Android → Switch Platform).
3. Player Settings → **Resolution and Presentation** → Default Orientation : **Auto Rotation** (portrait + paysage cochés).

---

## 1) Importer les 4 packs gratuits (Asset Store)

| Asset | Usage dans le jeu |
|---|---|
| **Joystick Pack** (Fenerax) | le stick tactile du joueur |
| **3 SLIME - LOWPOLY** | les 4 personnages |
| **Free Pop Sound Effects Pack** | 1 à 3 clips branchés dans `Bubble.popSounds` |
| **Bubblewrap Texture Vol.01** | sol du terrain |

Menue : **Window → Package Manager → My Assets** → Import All.

---

## 2) Tags & Layers

Menu **Edit → Project Settings → Tags and Layers**.

- **Tags** : crée le tag **`Slime`**. (Le tag est mis sur les préfabs de slimes, les `Bubble.cs` y regardent.)
- **Layers** : dans la partie **Builtin Layers**, un layer déjà nommé `Water` est libre… mais plus propre :
  - Utilise un **layer utilisateur** libre (ex. layer 6) et renomme-le **`Bubble`**.
  - **Toutes les bulles doivent avoir ce layer**, et le `LayerMask.GetMask("Bubble")` du SlimeController IA ne voit qu'elles.

---

## 3) Physique (l'effet "satisfaisant")

**Project Settings → Physics** (si URP, c'est Project Settings → Physics aussi).

### 3.a Le Physic Material des slimes
1. **Assets → Create → Physic Material** → nomme-le `SlimeBounce`.
2. Réglages exacts :
   - **Dynamic Friction** : `0.1`
   - **Static Friction** : `0.1`
   - **Bounciness** : `0.8`
   - **Friction Combine** : `Maximum`
   - **Bounce Combine** : `Maximum`
3. Glisse-le dans le **SphereCollider** de chaque préfab de slime.

### 3.b Le Physic Material des bulles
1. Nouveau Physic Material `BubbleBounce` :
   - **Bounciness** : `0.6`, **Friction** : `0.05`
   - **Friction Combine** : `Maximum`, **Bounce Combine** : `Maximum`
2. Sur le SphereCollider des bulles.

> ⚠️ Si ta version d'Unity n'a plus "Physic Material" mais "Physics Material", c'est la même chose (renommé en 6.0).

---

## 4) Le terrain (l'arène)

1. **GameObject → 3D Object → Plane** — nom `Arena`.
   - Position `(0, 0, 0)`, scale `(2, 1, 2)` → arène 20×20 m.
2. Assigne le material **Bubblewrap** du pack de texture Ehsan Vaezi
   (glisse le material `Bubblewrap_*` directement sur le Plane).
3. **Mesh Collider** sur le Plane : coché par défaut, laisse tel quel.

---

## 5) Le préfab Slime

1. Glisse un des slimes du pack "3 SLIME - LOWPOLY" dans la scène.
2. Sur ce GameObject **racine du slime** :
   - **Add → Rigidbody** (Interpolate : `Interpolate`, Collision Detection : `Continuous`).
   - **Add → Sphere Collider** (ou garde le collider fourni) → glisse `SlimeBounce` dans le champ **Material**.
   - **Add Component → SlimeController.cs** (le script du dossier `Assets/Scripts/`).
   - **Tag : `Slime`** (obligatoire !).
3. Dans SlimeController : **isPlayer** = coché pour TOI, décoché pour les 3 bots.
4. **Glisse le GameObject dans le dossier `Assets/Prefabs/`** pour créer les 2 préfabs
   (clic droit → Create → Folder dans Assets, puis fait un drag du slime dedans).
   Tu obtiens `PlayerSlimePrefab` et `BotSlimePrefab` (peut-être pareil qu'avec 3 variations de couleur).

> 💡 Les scripts `SlimeController` et `Bubble` cherchent le **tag `Slime` sur le GameObject racine**.
> Si le collider du tag est sur un child, le tag doit être sur le même GameObject que le script `SlimeController`.
> De même, le `Rigidbody` doit être sur le **même GameObject** que le tag (sinon la détection d'impact via
> `collision.gameObject.CompareTag("Slime")` échoue).

---

## 6) Le préfab Bulle

1. **GameObject → 3D Object → Sphere** — nom `Bubble`.
   - Scale : `(0.9, 0.9, 0.9)` — rayon ~0.45.
   - Position Y à environ 0.45 (pose sur le sol).
2. Sur ce GameObject :
   - **Rigidbody** : Interpolate `Interpolate`, Use Gravity **coché** (roule légèrement au sol — satisfaisant).
   - **Sphere Collider** → glisse `BubbleBounce` dans **Material**.
   - **Layer = Bubble**.
   - Décoche **`Play`**... c'est un préfab, tu ne joues rien — passe à l'étape suivante.
3. **Add Component → Bubble.cs**.
4. Crée le **prefab** : dossier `Assets/Prefabs/` → drag de `Bubble` dedans.
5. Crée le **préfab de particules** :
   - **GameObject → Particle System** — nom `PopBurst`.
   - Laisse les réglages par défaut avec :
     - **Start Color** : aléatoire dans les tons de ton bubblewrap (teinté par exemple en rose/bleu).
     - **Start Speed** : 3 ; **Start Size** : 0.12 ; **Start Lifetime** : 0.7.
     - **Emission → Rate over Time** : 0 ; **Burst Count** = 15.
     - **Shape** : Sphere, Radius 0.1.
     - **Main → Duration** : 0.4 ; **Looping** : décoché ; **Playback Speed** : 1.5.
   - **Add Component → ParticleBurst.cs** (gère sa propre destruction).
   - Préfab : drag vers `Assets/Prefabs/PopBurstPrefab`.

---

## 7) La bulle "pop" sonore

1. Dans le préfab `Bubble` sélectionné, champ **popSounds** de `Bubble.cs` : **Taille = 3**.
2. Glisse 1-3 clips du pack "Free Pop Sound Effects" dedans.
3. Champ **popParticlePrefab** : glisse le préfab `PopBurst` créé à l'étape 6.
4. Crée le **AudioManager** : GameObject vide nommé `AudioManager` → **Add Component → AudioManager.cs**.
   C'est lui qui gère le pool et le spatialBlend = 0 (son 2D, idéal téléphone sans écouteurs).

---

## 8) La caméra

1. **GameObject → 3D Object → Camera** — nom `MainCamera` (ou transforme la caméra de la scène).
   Tu peux réutiliser celle déjà dans la scène (garde juste le tag **MainCamera**).
2. Add Component → `CameraFollow.cs`.
3. Réglages conseillés :
   - **height** : 20 ; **backOffset** : -8 ; **tiltAngle** : 60°.
   - **baseOrthoSize** : 8 (paysage) ; **portraitZoomFactor** : 1.55 (portrait).
   - **smoothSpeed** : 6.
   - **Orthographic** sera coché automatiquement par le script.

---

## 9) Le GameManager (orchestrateur)

GameObject vide nommé `GameManager` :

1. Add Component → `GameManager.cs`.
2. Add Component → `BubbleSpawner.cs` (le même GameObject, c'est plus simple).
3. Assigner dans GameManager :
   - **playerSlimePrefab** → ton préfab slime joueur.
   - **botSlimePrefab** → ton préfab slime bot.
   - **gameCamera** → `MainCamera`.
   - **spawner** → le même GameObject (auto-référence du GameManager, glisse lui-même dans le slot).
   - **gameUI** → l'objet `GameUI` créé juste après (étape 10).
   - **playerJoystick** → le joystick d'UI créé à l'étape 10.
4. Étends **startPositions** à **taille 4**. Crée 4 GameObjects vides aux coins de l'arène:
   - `Spawn0` position `(-8, 1.5, -8)`   (joueur)
   - `Spawn1` position `(8, 1.5, -8)`
   - `Spawn2` position `(-8, 1.5, 8)`
   - `Spawn3` position `(8, 1.5, 8)`
   Glisse chacun dans les 4 slots de **startPositions**.
5. Dans **BubbleSpawner** :
   - **bubblePrefab** → préfab `Bubble`.
   - **bubbleCount** : 50.
   - **arenaSize** : (20, 20).
   - **minSpacing** : 1.2.
6. GameObject vide nommé `OrientationManager` → **Add Component → OrientationManager.cs**.
   (Aucune reference obligatoire. Tu peux y glisser l'objet `GameUI` dans `adaptivePanels`).

---

## 10) L'UI (Canvas)

### 10.a Canvas principal
1. **GameObject → UI → Canvas** — nom `GameCanvas`.
   - **Render Mode** : `Screen Space - Overlay`.
2. **Canvas Scaler** (composant existant sur le Canvas) :
   - UI Scale Mode = **Scale With Screen Size**
   - Reference Resolution = **1080 x 1920**
   - Screen Match Mode = **Match Width Or Height**
   - Match = 0.5 … et **0.2** en pratique (0.5 donne un bon compromis).
3. **Canvas Renderer → Additional Settings → Sort Order** : laisse par défaut.

### 10.b Le HUD (chrono + 4 scores)
1. Sur `GameCanvas` → **GameObject → UI → Text - TextMeshPro** nommé `TimerText`
   (top centre ; Anchor Preset `Top Center`) — TMP demande l'import "TextMeshPro Essentials",
   fenêtre qui propose l'import une fois : **Import TMP Essentials**.
2. 4 autres Text TMP `ScoreText_0..3`, ancrés en haut à droite (2×2) :
   - ScoreText_0 en haut, ScoreText_1 à droite, ScoreText_2 en bas, ScoreText_3 à gauche.
3. Crée un GameObject vide `GameUI` (dans Canvas) → **Add Component → GameUI.cs**.
4. Glisse :
   - **timerText** → le Text TMP `TimerText`.
   - **scoreTexts** (4 éléments) → les 4 Texts TMP.
5. Couleurs : dans GameUI, réglages slimeColors (0 bleu, 1 rouge, 2 vert, 3 jaune).
   Recolore les Text TMP manuellement pour donner la couleur du slime correspondant.

### 10.c Panneau "Fin du round"
1. Sur `GameCanvas` → **GameObject → UI → Panel** nommé `RoundEndPanel`.
   - Image Couleur : noir semi-transparent (Alpha = 200).
   - Au centre, 2 Text TMP : `RoundEndTitle` et `RoundEndScores`.
2. Glisse RoundEndPanel dans champ **roundEndPanel** du GameUI,
   les 2 textes dans **roundEndTitleText** et **roundEndScoresText**.

### 10.d Panneau final (gagnant + Rejouer)
1. Sur `GameCanvas` → **GameObject → UI → Panel** nommé `WinnerPanel`.
   - Image Couleur : noir semi-transparent.
   - 2 Texts TMP au centre : `WinnerTitle` et `WinnerScore`.
   - **GameObject → UI → Button** nommé `ReplayButton` (texte « REJOUER »).
2. Glisse dans GameUI : **winnerPanel**, **winnerTitleText**, **winnerScoreText**,
   **replayButton** (le champ suffit : le script y a déjà branché le clic
   vers `GameManager.OnReplayButton`, pas besoin de configurer OnClick manuellement).

### 10.e Le joystick
1. Import du Joystick Pack : il crée 2 dossiers `SimpleJoystick` & `VariableJoystick`.
2. Dans GameCanvas : glisse le préfab **`Fixed Joystick`** du pack (dossier `Joystick Pack/Prefabs`)
   → nomme-le `PlayerJoystick`.
3. Ajuste ton ancre : coin bas-gauche (Anchor Preset `Bottom Left`), taille ~300×300.
4. Sur l'objet `GameManager`, **playerJoystick** → `PlayerJoystick` glissé dedans
   (c'est un FixedJoystick — compatible avec notre type `Joystick` du pack).
5. Option (facile) : fais un drag du préfab **Joystick Pads** (fonds) derrière le stick pour le style.

> 📌 Le Joystick Pack expose la propriété `Direction` (Vector2 -1..1) que
> `SlimeController.HandlePlayerInput()` lit automatiquement.

---

## 11) Éclairage / ambiance

1. **GameObject → Light → Directional Light** (la scène 3D Core en a toujours une) :
   - Intensity 0.8, Rotation (45°, 30°, 0°) pour un joli relief du film à bulles.
2. **Window → Rendering → Lighting → Environment** :
   - **Ambient Source : Gradient** — Ambient Color : un gris clair (0.6) pour éviter les zones noires.
3. Optionnel très "ASMR" : **GameObject → Light → Point Light** au-dessus de l'arène,
   intensité douce, couleur chaude/rosée — les pops brillent doucement.

---

## 12) Récapitulatif de la hiérarchie de scène attendue

```
SampleScene
├── Arena (Plane + material bubblewrap)
├── MainCamera (CameraFollow.cs)
├── Directional Light
├── AudioManager (AudioManager.cs)
├── GameManager (GameManager.cs + BubbleSpawner.cs)
│     └── [les slimes & bulles seront spawnés par code]
├── OrientationManager (OrientationManager.cs)
├── Spawn0 / Spawn1 / Spawn2 / Spawn3 (Transforms vides)
├── GameCanvas (Screen Space - Overlay, Canvas Scaler 1080x1920)
│   ├── TimerText (TMP)
│   ├── ScoreText_0 .. ScoreText_3 (TMP)
│   ├── PlayerJoystick (Fixed Joystick du pack)
│   ├── RoundEndPanel (Panel + 2 TMP)
│   └── WinnerPanel (Panel + 2 TMP + ReplayButton)
```

---

## 13) Checklist avant de jouer (Play)

- [ ] Tag **Slime** sur le GameObject racine de tous les slimes.
- [ ] Layer **Bubble** sur les bulles (préfab compris).
- [ ] PhysicMaterials assignés : `SlimeBounce` sur les slimes, `BubbleBounce` sur les bulles.
- [ ] `popSounds` (3 clips) et `popParticlePrefab` remplis dans le préfab Bubble.
- [ ] **startPositions** du GameManager : 4 slots non vides.
- [ ] GameUI : timerText, 4 scoreTexts, les 2 panneaux, replayButton remplis.
- [ ] GameManager : les 6 champs remplis (préfab, caméra, spawner, gameUI, joystick).
- [ ] **Canvas Scaler** : Scale With Screen Size + 1080x1920.
- [ ] Player Settings : Default Orientation = Auto Rotation (portrait + paysage).

Appuie sur **Play** : ta caméra devrait suivre ton slime, tu pousses le stick… et ça pop.

---

## 14) Ajuster l'équilibre du jeu (Game Design)

Le but est la satisfaction, pas la difficulté. Tunes ça :

| Fait quoi | Change où |
|---|---|
| Slime plus rapide | `playerForce` (SlimeController, 12 → 15) |
| Bots plus lents | `botForce` (10 → 7) |
| Bots plus maladroits | monte `retargetDelay` (1.5 → 3) et `detectionRadius` baisse (30 → 15) |
| Pops plus faciles | `minImpactVelocity` (Bubble.cs : 2.0 → 1.4) |
| Slime plus "gluant" | Physic Material : Bounciness 0.8 → 0.55 |
| Slime plus "ressort" | Bounciness → 1.0 (fun mais déstabilisant à jouer) |
| Round plus long | `roundDuration` (GameManager) |

---

## 15) Astuces mobiles finales (avant le build APK)

1. **Build Profiles → Android → Switch Platform** (fait-le en premier, économise du temps).
2. **Player Settings → Other Settings** :
   - Scripting Backend : **IL2CPP** ; Target Architectures : **ARMv7 + ARM64**.
   - Graphics API : supprime Vulkan, garde **OpenGLES3** (stabilité sur vieux téléphones).
3. **Texture des 4 packs** → Compression : **Crunch** (éco de mémoire).
4. Teste à **60 fps** (Application.targetFrameRate est déjà dans GameManager).
5. **Quality Settings → VSync Count : Don't Sync** + Fixed Timestep 0.02 (60Hz physique stable).

Bon pop ! 🫧
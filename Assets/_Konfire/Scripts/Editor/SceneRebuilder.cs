using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class TileData
{
    public int x;
    public int y;
    public int z;
    public string s;
}

[System.Serializable]
public class TilemapData
{
    public string name;
    public TileData[] tiles;
}

[System.Serializable]
public class ObjectData
{
    public string name;
    public string parent;
    public string tag;
    public int layer;
    public float x, y, z;
    public float sx, sy, sz;
    public string sprite;
    public int sortOrder;
    public bool hasBoxCol;
    public float boxOffX, boxOffY, boxSizeX, boxSizeY;
    public bool boxTrig;
    public bool hasCircleCol;
    public float circleRadius;
    public bool circleTrig;
}

[System.Serializable]
public class BuildData
{
    public TilemapData[] tilemaps;
    public ObjectData[] objects;
}

public class SceneRebuilder
{
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    [MenuItem("Konfire/Rebuild Authentic Game Scene")]
    public static void RebuildScene()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // Clear existing scene root objects
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            Object.DestroyImmediate(r);
        }

        string jsonPath = "Assets/_Konfire/scene_build_data.json";
        string json = File.ReadAllText(jsonPath);
        BuildData data = JsonUtility.FromJson<BuildData>(json);

        // Preload sprites from OriginalSprites
        string spriteDir = "Assets/_Konfire/Sprites/OriginalSprites";
        Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();

        string[] pngFiles = Directory.GetFiles(spriteDir, "*.png");
        foreach (var f in pngFiles)
        {
            string sName = Path.GetFileNameWithoutExtension(f);
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(f);
            if (sp != null)
            {
                spriteCache[sName] = sp;
                Tile t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = sp;
                tileCache[sName] = t;
            }
        }
        Debug.Log("Loaded " + spriteCache.Count + " sprites into cache.");

        // 1. Create Grid and Tilemaps
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        Dictionary<string, Tilemap> tilemaps = new Dictionary<string, Tilemap>();
        foreach (var tmData in data.tilemaps)
        {
            GameObject tmGO = new GameObject(tmData.name);
            tmGO.transform.SetParent(gridGO.transform, false);
            Tilemap tm = tmGO.AddComponent<Tilemap>();
            TilemapRenderer tr = tmGO.AddComponent<TilemapRenderer>();
            tr.sortingLayerName = "Default";

            if (tmData.name == "Ground_Layer") tr.sortingOrder = 0;
            else if (tmData.name == "Wall_Layer") tr.sortingOrder = 1;
            else if (tmData.name == "Collision_Layer")
            {
                tr.sortingOrder = 0;
                tmGO.AddComponent<TilemapCollider2D>();
                var rb2d = tmGO.AddComponent<Rigidbody2D>();
                rb2d.bodyType = RigidbodyType2D.Static;
            }

            foreach (var td in tmData.tiles)
            {
                if (tileCache.TryGetValue(td.s, out Tile tile))
                {
                    tm.SetTile(new Vector3Int(td.x, td.y, td.z), tile);
                }
            }
            tilemaps[tmData.name] = tm;
        }

        // 2. Load Assets
        GameObject logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Konfire/Prefabs/Log.prefab");
        GameObject catPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Konfire/Prefabs/GhostCat.prefab");
        RuntimeAnimatorController goblinAnim = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Konfire/Animations/Goblin/GoblinAnimator.controller");
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Konfire/Scripts/Player/PlayerInputActions.inputactions");

        AudioClip footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Konfire/Audio/audio-editor-output.wav");
        AudioClip axeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Konfire/Audio/hitting-stalactites-with-axe-212652.mp3");
        AudioClip fireBurnClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Konfire/Audio/flame-igniting-with-whoomph-gregor-quendel-1-00-02.mp3");
        AudioClip musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Konfire/Audio/Lost in the Pixel Pines.mp3");
        AudioClip catClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Konfire/Audio/ghost-6979.mp3");

        // 3. Create GameObjects
        Dictionary<string, GameObject> createdObjects = new Dictionary<string, GameObject>();
        createdObjects["Grid"] = gridGO;

        foreach (var obj in data.objects)
        {
            // Skip UI objects handled separately
            if (obj.name == "Canvas" || obj.parent == "Canvas" || obj.parent == "Fuel_Slider" || 
                obj.parent == "Fill Area" || obj.parent == "Score_Background" || 
                obj.parent == "GameOver_Screen" || obj.name == "EventSystem")
            {
                continue;
            }

            GameObject go = new GameObject(obj.name);
            go.transform.position = new Vector3(obj.x, obj.y, obj.z);
            go.transform.localScale = new Vector3(obj.sx > 0.001f ? obj.sx : 1f, obj.sy > 0.001f ? obj.sy : 1f, obj.sz > 0.001f ? obj.sz : 1f);

            if (!string.IsNullOrEmpty(obj.tag) && obj.tag != "Untagged")
            {
                go.tag = obj.tag;
            }
            go.layer = obj.layer;

            // Sprite Renderer
            if (!string.IsNullOrEmpty(obj.sprite) && spriteCache.TryGetValue(obj.sprite, out Sprite sp))
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = obj.sortOrder;
            }

            // Box Collider
            if (obj.hasBoxCol)
            {
                BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                bc.offset = new Vector2(obj.boxOffX, obj.boxOffY);
                bc.size = new Vector2(obj.boxSizeX > 0.01f ? obj.boxSizeX : 1f, obj.boxSizeY > 0.01f ? obj.boxSizeY : 1f);
                bc.isTrigger = obj.boxTrig;
            }

            // Circle Collider
            if (obj.hasCircleCol)
            {
                CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
                cc.radius = obj.circleRadius > 0.01f ? obj.circleRadius : 0.5f;
                cc.isTrigger = obj.circleTrig;
            }

            createdObjects[obj.name] = go;
        }

        // Set Parents
        foreach (var obj in data.objects)
        {
            if (!string.IsNullOrEmpty(obj.parent) && createdObjects.ContainsKey(obj.parent) && createdObjects.ContainsKey(obj.name))
            {
                createdObjects[obj.name].transform.SetParent(createdObjects[obj.parent].transform, true);
            }
        }

        // 4. Configure Special Components

        // A. Bonfire & Altar
        GameObject bonfireLogic = createdObjects["BonfireLogic"];
        bonfireLogic.tag = "Bonfire";
        CircleCollider2D bfCol = GetOrAdd<CircleCollider2D>(bonfireLogic);
        bfCol.isTrigger = true;
        bfCol.radius = 2.5f;

        AudioSource bfAudio = bonfireLogic.AddComponent<AudioSource>();
        bfAudio.playOnAwake = false;
        bfAudio.clip = fireBurnClip;

        Bonfire bonfireComp = bonfireLogic.AddComponent<Bonfire>();

        // Fire Glow Halo
        GameObject fireBase = createdObjects.ContainsKey("Fire_Base") ? createdObjects["Fire_Base"] : bonfireLogic;
        Sprite haloSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Visuals/GlowHalo.png");
        if (haloSprite != null)
        {
            GameObject haloGO = new GameObject("Fire_Glow_Halo");
            haloGO.transform.SetParent(fireBase.transform, false);
            haloGO.transform.localPosition = Vector3.zero;
            haloGO.transform.localScale = new Vector3(5f, 4.5f, 1f);
            SpriteRenderer hsr = haloGO.AddComponent<SpriteRenderer>();
            hsr.sprite = haloSprite;
            hsr.color = new Color(1f, 0.55f, 0.1f, 0.45f);
            hsr.sortingOrder = 3;
            haloGO.AddComponent<FireGlowFlicker>();
        }
        bonfireComp.logBurnSource = bfAudio;

        // NoSpawnZone
        if (createdObjects.ContainsKey("NoSpawnZone"))
        {
            GameObject nsz = createdObjects["NoSpawnZone"];
            nsz.tag = "NoSpawn";
            BoxCollider2D nszCol = GetOrAdd<BoxCollider2D>(nsz);
            nszCol.isTrigger = true;
            nszCol.size = new Vector2(14f, 12f);
        }

        // Altar Entrance & BuffZone
        GameObject altarEntrance = createdObjects["Altar_Entrance"];
        GameObject glowPink = createdObjects.ContainsKey("Glow_Effect") ? createdObjects["Glow_Effect"] : null;
        GameObject glowBlue = createdObjects.ContainsKey("Blue_Glow_Effect") ? createdObjects["Blue_Glow_Effect"] : null;
        GameObject buffZone = createdObjects.ContainsKey("Buff_Zone") ? createdObjects["Buff_Zone"] : null;

        if (buffZone != null)
        {
            buffZone.tag = "BuffZone";
            BoxCollider2D bzCol = GetOrAdd<BoxCollider2D>(buffZone);
            bzCol.isTrigger = true;
            bzCol.size = new Vector2(4f, 4f);
        }

        if (glowPink != null)
        {
            SpriteRenderer gsr = GetOrAdd<SpriteRenderer>(glowPink);
            gsr.sprite = spriteCache.ContainsKey("TX Props Rune Pillar X2 Glow") ? spriteCache["TX Props Rune Pillar X2 Glow"] : null;
            gsr.color = new Color(1f, 0.2f, 0.8f, 0.8f);
            gsr.sortingOrder = 6;
            glowPink.SetActive(false);
            bonfireComp.altarGlow = glowPink;
        }

        if (glowBlue != null)
        {
            SpriteRenderer bsr = GetOrAdd<SpriteRenderer>(glowBlue);
            bsr.sprite = spriteCache.ContainsKey("TX Props Rune Pillar Broken Glow") ? spriteCache["TX Props Rune Pillar Broken Glow"] : null;
            bsr.color = new Color(0f, 0.8f, 1f, 0.8f);
            bsr.sortingOrder = 6;
            glowBlue.SetActive(false);
            bonfireComp.blueAltarGlow = glowBlue;
        }

        // B. Player
        GameObject player = createdObjects["Player"];
        player.tag = "Player";
        player.layer = 6; // Player layer
        var playerCol = GetOrAdd<CircleCollider2D>(player);
        playerCol.radius = 0.45f;

        Rigidbody2D playerRb = GetOrAdd<Rigidbody2D>(player);
        playerRb.bodyType = RigidbodyType2D.Dynamic;
        playerRb.gravityScale = 0f;
        playerRb.freezeRotation = true;
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PlayerController pc = player.AddComponent<PlayerController>();
        PlayerInput pi = player.AddComponent<PlayerInput>();
        pi.actions = inputActions;

        Animator pAnim = GetOrAdd<Animator>(player);
        pAnim.runtimeAnimatorController = goblinAnim;

        AudioSource pFoot = player.AddComponent<AudioSource>();
        pFoot.playOnAwake = false;
        pFoot.loop = true;
        pFoot.clip = footstepClip;

        AudioSource pAxe = player.AddComponent<AudioSource>();
        pAxe.playOnAwake = false;
        pAxe.clip = axeClip;

        pc.footstepSource = pFoot;
        pc.axeChopSource = pAxe;
        pc.altarGlow = glowPink;
        pc.blueAltarGlow = glowBlue;

        // Thought Bubble
        if (createdObjects.ContainsKey("ThoughtBubble"))
        {
            GameObject tb = createdObjects["ThoughtBubble"];
            tb.transform.localPosition = new Vector3(1.1f, 1.3f, 0f);
            tb.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            SpriteRenderer tbsr = tb.GetComponent<SpriteRenderer>();
            if (tbsr != null) tbsr.sortingOrder = 15;
            tb.AddComponent<ThoughtBubbleBob>();
            tb.SetActive(false);
            pc.thoughtBubble = tb;
        }

        // C. Trees
        string[] treeNames = new string[] { "Tree_1", "Tree_1 (1)", "Tree_1 (2)" };
        foreach (var tName in treeNames)
        {
            if (createdObjects.ContainsKey(tName))
            {
                GameObject tgo = createdObjects[tName];
                tgo.tag = "Tree";
                tgo.layer = 8; // Trees layer
                Tree tComp = tgo.AddComponent<Tree>();
                tComp.logPrefab = logPrefab;

                Transform spTr = tgo.transform.Find("SpawnPoint");
                if (spTr != null) tComp.spawnPoint = spTr;

                BoxCollider2D tcol = GetOrAdd<BoxCollider2D>(tgo);
                tcol.size = new Vector2(1.2f, 1.2f);
                tcol.offset = new Vector2(0f, -0.5f);
            }
        }

        // D. ForestManager
        GameObject fmGO = createdObjects["ForestManager"];
        ForestManager fm = fmGO.AddComponent<ForestManager>();
        fm.catPrefab = catPrefab;
        fm.playerTransform = player.transform;
        fm.minTreesBeforeCat = 14;
        fm.maxTreesBeforeCat = 20;

        AudioSource catAudio = fmGO.AddComponent<AudioSource>();
        catAudio.playOnAwake = false;
        catAudio.clip = catClip;
        fm.catSpawnSource = catAudio;

        // Tree prefabs list
        List<GameObject> treePrefabs = new List<GameObject>();
        string[] pPaths = new string[]
        {
            "Assets/_Konfire/Prefabs/Tree_GlowingWillow.prefab",
            "Assets/_Konfire/Prefabs/Tree_WhiteDead.prefab",
            "Assets/_Konfire/Prefabs/Tree_Bonsai.prefab",
            "Assets/_Konfire/Prefabs/Tree_Treant.prefab",
            "Assets/_Konfire/Prefabs/Tree_Golden.prefab"
        };
        foreach (var pp in pPaths)
        {
            GameObject tp = AssetDatabase.LoadAssetAtPath<GameObject>(pp);
            if (tp != null) treePrefabs.Add(tp);
        }
        fm.treePrefabs = treePrefabs;

        // E. GameManager
        GameObject gmGO = createdObjects["GameManager"];
        GameManager gm = gmGO.AddComponent<GameManager>();

        AudioSource musicAudio = gmGO.AddComponent<AudioSource>();
        musicAudio.loop = true;
        musicAudio.clip = musicClip;
        gm.gameMusicSource = musicAudio;

        AudioSource sfxAudio = gmGO.AddComponent<AudioSource>();
        sfxAudio.playOnAwake = false;
        gm.buttonClickSource = sfxAudio;
        gm.gameOverClip = catClip;

        // F. Camera Rig Architecture
        GameObject camGO = createdObjects["Main Camera"];
        Camera cam = GetOrAdd<Camera>(camGO);
        cam.orthographic = true;
        cam.orthographicSize = 7.5f; cam.backgroundColor = new Color(0.196f, 0.365f, 0.208f, 1f); cam.clearFlags = CameraClearFlags.SolidColor;

        GameObject cameraRig = new GameObject("CameraRig");
        cameraRig.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, 0f);
        var camFollow = cameraRig.AddComponent<Cainos.PixelArtTopDown_Basic.CameraFollow>();
        camFollow.target = player.transform;
        camFollow.lerpSpeed = 4.5f;

        camGO.transform.SetParent(cameraRig.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0f, -10f);
        var shake = camGO.GetComponent<CameraShake>();
        if (shake == null) shake = camGO.AddComponent<CameraShake>();
        var camData = camGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (camData == null) camData = camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // G. Create UI Canvas
        CreateGameUI(bonfireComp, gm);

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully rebuilt GameScene.unity with authentic layout!");
    }

    static void CreateGameUI(Bonfire bonfire, GameManager gm)
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 0. Cinematic Forest Vignette (Atmospheric depth)
        Sprite vigSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Visuals/Vignette.png");
        if (vigSprite != null)
        {
            GameObject vigGO = new GameObject("Vignette_Overlay");
            vigGO.transform.SetParent(canvasGO.transform, false);
            RectTransform vigRt = vigGO.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.sizeDelta = Vector2.zero;
            UnityEngine.UI.Image vigImg = vigGO.AddComponent<UnityEngine.UI.Image>();
            vigImg.sprite = vigSprite;
            vigImg.color = new Color(1f, 1f, 1f, 0.55f);
            vigImg.raycastTarget = false;
        }

        // 1. Fuel Slider (Top-Left, Authentic Ornate Carved Frame)
        Sprite fuelFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/OriginalSprites/ChatGPT Image Oct 29, 2025, 09_14_26 PM_3.png");
        GameObject sliderGO = new GameObject("Fuel_Slider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        RectTransform srt = sliderGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 1f);
        srt.anchoredPosition = new Vector2(35f, -25f);
        srt.sizeDelta = new Vector2(360f, 90f);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        // Dark background slot behind fire
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        RectTransform bgrt = bgGO.AddComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = new Vector2(40f, 22f);
        bgrt.offsetMax = new Vector2(-40f, -22f);
        UnityEngine.UI.Image bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.12f, 0.05f, 0.02f, 0.95f);

        // Fill Area
        GameObject faGO = new GameObject("Fill Area");
        faGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fart = faGO.AddComponent<RectTransform>();
        fart.anchorMin = Vector2.zero;
        fart.anchorMax = Vector2.one;
        fart.offsetMin = new Vector2(42f, 24f);
        fart.offsetMax = new Vector2(-42f, -24f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(faGO.transform, false);
        RectTransform fillrt = fillGO.AddComponent<RectTransform>();
        fillrt.anchorMin = Vector2.zero;
        fillrt.anchorMax = Vector2.one;
        fillrt.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image fillImg = fillGO.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(1f, 0.42f, 0.06f, 1f);

        slider.fillRect = fillrt;
        bonfire.fuelSlider = slider;

        // Ornate Wood/Stone Frame on top
        if (fuelFrame != null)
        {
            GameObject frameGO = new GameObject("Frame");
            frameGO.transform.SetParent(sliderGO.transform, false);
            RectTransform frt = frameGO.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.sizeDelta = Vector2.zero;
            UnityEngine.UI.Image frImg = frameGO.AddComponent<UnityEngine.UI.Image>();
            frImg.sprite = fuelFrame;
            frImg.preserveAspect = false;
            frImg.raycastTarget = false;
        }

        // 2. Score Banner (Top-Right, Authentic Carved Banner)
        Sprite scoreBanner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/OriginalSprites/ChatGPT Image Oct 29, 2025, 09_14_26 PM_0.png");
        GameObject scoreBgGO = new GameObject("Score_Background");
        scoreBgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform sbrt = scoreBgGO.AddComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(1f, 1f);
        sbrt.anchorMax = new Vector2(1f, 1f);
        sbrt.pivot = new Vector2(1f, 1f);
        sbrt.anchoredPosition = new Vector2(-35f, -25f);
        sbrt.sizeDelta = new Vector2(340f, 106f);

        UnityEngine.UI.Image sbImg = scoreBgGO.AddComponent<UnityEngine.UI.Image>();
        if (scoreBanner != null)
        {
            sbImg.sprite = scoreBanner;
            sbImg.preserveAspect = false;
        }
        else
        {
            sbImg.color = new Color(0.2f, 0.15f, 0.12f, 0.9f);
        }

        GameObject scoreTextGO = new GameObject("ScoreText");
        scoreTextGO.transform.SetParent(scoreBgGO.transform, false);
        RectTransform strt = scoreTextGO.AddComponent<RectTransform>();
        strt.anchorMin = Vector2.zero;
        strt.anchorMax = Vector2.one;
        strt.offsetMin = new Vector2(25f, 10f);
        strt.offsetMax = new Vector2(-25f, -10f);
        TextMeshProUGUI tmpScore = scoreTextGO.AddComponent<TextMeshProUGUI>();
        tmpScore.text = "Score: 0";
        tmpScore.alignment = TextAlignmentOptions.Center;
        tmpScore.fontSize = 32f;
        tmpScore.fontStyle = FontStyles.Bold;
        tmpScore.color = new Color(1f, 0.94f, 0.82f, 1f);
        gm.scoreText = tmpScore;

        // 3. Notification Text (Bottom-Left)
        GameObject notifGO = new GameObject("NotificationText");
        notifGO.transform.SetParent(canvasGO.transform, false);
        RectTransform notifrt = notifGO.AddComponent<RectTransform>();
        notifrt.anchorMin = new Vector2(0f, 0f);
        notifrt.anchorMax = new Vector2(0f, 0f);
        notifrt.pivot = new Vector2(0f, 0f);
        notifrt.anchoredPosition = new Vector2(35f, 30f);
        notifrt.sizeDelta = new Vector2(800f, 45f);

        TextMeshProUGUI notifTmp = notifGO.AddComponent<TextMeshProUGUI>();
        notifTmp.text = "";
        notifTmp.fontSize = 22f;
        notifTmp.fontStyle = FontStyles.Bold;
        notifTmp.color = new Color(1f, 0.92f, 0.4f, 1f);
        notifGO.SetActive(false);
        gm.notificationText = notifTmp;

        // 4. Game Over Screen
        Sprite goBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/UI/GameOverBg.png");
        GameObject goScreen = new GameObject("GameOver_Screen");
        goScreen.transform.SetParent(canvasGO.transform, false);
        RectTransform gort = goScreen.AddComponent<RectTransform>();
        gort.anchorMin = Vector2.zero;
        gort.anchorMax = Vector2.one;
        gort.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image goImg = goScreen.AddComponent<UnityEngine.UI.Image>();
        if (goBg != null)
        {
            goImg.sprite = goBg;
            goImg.preserveAspect = true;
        }
        else
        {
            goImg.color = new Color(0.04f, 0.06f, 0.05f, 0.92f);
        }

        // Title
        GameObject goTitleGO = new GameObject("GameOverTitle");
        goTitleGO.transform.SetParent(goScreen.transform, false);
        RectTransform titleRt = goTitleGO.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.72f);
        titleRt.anchorMax = new Vector2(0.5f, 0.72f);
        titleRt.sizeDelta = new Vector2(800f, 90f);
        TextMeshProUGUI titleTmp = goTitleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "GAME OVER";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 62f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.35f, 0.2f, 1f);

        // Final score / reason text
        GameObject finalScoreGO = new GameObject("FinalScore_Text");
        finalScoreGO.transform.SetParent(goScreen.transform, false);
        RectTransform fsrt = finalScoreGO.AddComponent<RectTransform>();
        fsrt.anchorMin = new Vector2(0.5f, 0.54f);
        fsrt.anchorMax = new Vector2(0.5f, 0.54f);
        fsrt.sizeDelta = new Vector2(700f, 90f);
        TextMeshProUGUI fsTmp = finalScoreGO.AddComponent<TextMeshProUGUI>();
        fsTmp.text = "The Fire Went Out\nFinal Score: 0";
        fsTmp.alignment = TextAlignmentOptions.Center;
        fsTmp.fontSize = 32f;
        fsTmp.fontStyle = FontStyles.Bold;
        fsTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        gm.gameOverText = fsTmp;

        // Restart button
        GameObject restartBtnGO = new GameObject("RestartButton");
        restartBtnGO.transform.SetParent(goScreen.transform, false);
        RectTransform rbrt = restartBtnGO.AddComponent<RectTransform>();
        rbrt.anchorMin = new Vector2(0.5f, 0.38f);
        rbrt.anchorMax = new Vector2(0.5f, 0.38f);
        rbrt.sizeDelta = new Vector2(260f, 55f);
        UnityEngine.UI.Image rbImg = restartBtnGO.AddComponent<UnityEngine.UI.Image>();
        rbImg.color = new Color(0.18f, 0.38f, 0.25f, 1f);
        Button rBtn = restartBtnGO.AddComponent<Button>();
        rBtn.onClick.AddListener(gm.RestartGame);

        GameObject rTxtGO = new GameObject("Text");
        rTxtGO.transform.SetParent(restartBtnGO.transform, false);
        RectTransform rtrt = rTxtGO.AddComponent<RectTransform>();
        rtrt.anchorMin = Vector2.zero;
        rtrt.anchorMax = Vector2.one;
        rtrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI rtmp = rTxtGO.AddComponent<TextMeshProUGUI>();
        rtmp.text = "PLAY AGAIN";
        rtmp.alignment = TextAlignmentOptions.Center;
        rtmp.fontSize = 24f;
        rtmp.fontStyle = FontStyles.Bold;
        rtmp.color = Color.white;

        // Main Menu button
        GameObject menuBtnGO = new GameObject("MainMenuButton");
        menuBtnGO.transform.SetParent(goScreen.transform, false);
        RectTransform mbrt = menuBtnGO.AddComponent<RectTransform>();
        mbrt.anchorMin = new Vector2(0.5f, 0.26f);
        mbrt.anchorMax = new Vector2(0.5f, 0.26f);
        mbrt.sizeDelta = new Vector2(260f, 55f);
        UnityEngine.UI.Image mbImg = menuBtnGO.AddComponent<UnityEngine.UI.Image>();
        mbImg.color = new Color(0.35f, 0.2f, 0.15f, 1f);
        Button mBtn = menuBtnGO.AddComponent<Button>();
        mBtn.onClick.AddListener(gm.LoadMainMenu);

        GameObject mTxtGO = new GameObject("Text");
        mTxtGO.transform.SetParent(menuBtnGO.transform, false);
        RectTransform mtrt = mTxtGO.AddComponent<RectTransform>();
        mtrt.anchorMin = Vector2.zero;
        mtrt.anchorMax = Vector2.one;
        mtrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI mtmp = mTxtGO.AddComponent<TextMeshProUGUI>();
        mtmp.text = "MAIN MENU";
        mtmp.alignment = TextAlignmentOptions.Center;
        mtmp.fontSize = 24f;
        mtmp.fontStyle = FontStyles.Bold;
        mtmp.color = Color.white;

        goScreen.SetActive(false);
        gm.gameOverScreen = goScreen;
    }

}

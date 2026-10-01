using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CatWaveManager : MonoBehaviour
{
    public static CatWaveManager Instance;

    public float gameTimer = 0f;
    public float initialGracePeriod = 20f;
    public float spawnCheckInterval = 8f;
    private float nextSpawnCheck = 0f;

    // Outer forest spawn bounds
    private Vector2[] spawnAnchors = new Vector2[]
    {
        new Vector2(-18f, 10f),
        new Vector2(18f, 10f),
        new Vector2(-18f, -8f),
        new Vector2(18f, -8f),
        new Vector2(0f, -12f),
        new Vector2(0f, 13f)
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        gameTimer += Time.deltaTime;

        if (gameTimer < initialGracePeriod) return;

        if (Time.time >= nextSpawnCheck)
        {
            nextSpawnCheck = Time.time + spawnCheckInterval;
            CheckAndSpawnCats();
        }
    }

    public int GetAllowedMaxCats()
    {
        if (gameTimer < initialGracePeriod) return 0;
        if (gameTimer < 60f) return 1;
        if (gameTimer < 120f) return 2;
        if (gameTimer < 180f) return 3;
        if (gameTimer < 250f) return 4;
        return 5;
    }

    private void CheckAndSpawnCats()
    {
        int currentCats = FindObjectsByType<GhostCatAI>(FindObjectsInactive.Exclude).Length;
        int maxAllowed = GetAllowedMaxCats();

        if (currentCats < maxAllowed)
        {
            Vector2 spawnPos = spawnAnchors[Random.Range(0, spawnAnchors.Length)];
            SpawnCat(spawnPos);
        }
    }

    public GameObject SpawnCat(Vector3 pos)
    {
        GameObject cat = new GameObject("GhostCat_Spirit");
        cat.transform.position = pos;
        cat.tag = "Untagged";

        var rb = cat.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = cat.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.85f;

        var solidCol = cat.AddComponent<CircleCollider2D>();
        solidCol.isTrigger = false;
        solidCol.radius = 0.45f;

        var sr = cat.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 7;
        #if UNITY_EDITOR
        var unlitMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/URP2D_SpriteUnlit.mat");
        if (unlitMat != null) sr.sharedMaterial = unlitMat;
        #endif

        // Soft spooky violet light
        var lightObj = new GameObject("Glow");
        lightObj.transform.SetParent(cat.transform, false);
        var l2d = lightObj.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = new Color(0.75f, 0.3f, 1.0f, 1.0f);
        l2d.intensity = 0.9f;
        l2d.pointLightInnerRadius = 0.5f;
        l2d.pointLightOuterRadius = 3.2f;

        cat.AddComponent<GhostCatAI>();

        GameManager.Instance?.ShowNotification("⚠️ A Forest Spirit Cat emerges from the shadows!");
        return cat;
    }
}

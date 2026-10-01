using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class GhostCatAI : MonoBehaviour
{
    [Header("Movement & Balance")]
    public float moveSpeed = 1.85f; // Slower than player (player base is 3.2f) so player can outrun!
    public bool canKill = false;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Sprite frontSprite;
    private Sprite sideSprite;
    private Sprite backSprite;

    private float baseScaleY = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        #if UNITY_EDITOR
        frontSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Cat/Cat_Front.png");
        sideSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Cat/Cat_Side.png");
        backSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Cat/Cat_Back.png");
        #endif

        if (sr != null && frontSprite != null) sr.sprite = frontSprite;
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        StartCoroutine(SpawnGraceRoutine());
    }

    private IEnumerator SpawnGraceRoutine()
    {
        // 2-second ghost fade-in grace period
        canKill = false;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.2f;
            sr.color = c;
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0.2f, 1f, elapsed / 2f);
                sr.color = c;
                yield return null;
            }
        }
        canKill = true;
    }

    void FixedUpdate()
    {
        if (!enabled || playerTransform == null || rb == null) return;

        Vector2 playerPos = playerTransform.position;
        Vector2 catPos = rb.position;
        Vector2 dir = (playerPos - catPos).normalized;

        // Check if CatRepel buff is active on player: if so, FLEE away from player!
        bool isRepelled = BuffManager.Instance != null && BuffManager.Instance.activeBuff == BuffType.CatRepel;
        if (isRepelled)
        {
            dir = -dir; // Run away!
        }

        // Sprite direction logic
        UpdateDirectionSprite(dir);

        // Gentle floating bob
        float bob = Mathf.Sin(Time.time * 6f) * 0.05f;

        rb.MovePosition(catPos + dir * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateDirectionSprite(Vector2 dir)
    {
        if (sr == null) return;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (sideSprite != null) sr.sprite = sideSprite;
            sr.flipX = dir.x < 0; // flip when moving left
        }
        else if (dir.y > 0.1f)
        {
            if (backSprite != null) sr.sprite = backSprite;
            sr.flipX = false;
        }
        else
        {
            if (frontSprite != null) sr.sprite = frontSprite;
            sr.flipX = false;
        }
    }

    public void Banish()
    {
        HitFXManager.Instance?.TriggerCatBanished(transform.position);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100);
            GameManager.Instance.ShowNotification("GHOST CAT BANISHED! (+100 pts)");
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canKill) return;

        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.hasWeapon)
            {
                // Player strikes back with Holy Weapon!
                pc.UseWeaponStrike(this);
                return;
            }

            // Otherwise, touch means game over
            GameManager.Instance?.EndGame("The Forest Spirit Cat caught you");
        }
    }
}

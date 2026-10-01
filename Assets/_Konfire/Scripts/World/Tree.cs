using UnityEngine;
using System.Collections;

public class Tree : MonoBehaviour
{
    [Header("Tree Stats")]
    public int health = 3;
    public int maxHealth = 3;
    public GameObject logPrefab;
    public Transform spawnPoint;

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine shakeRoutine;
    private Quaternion originalRot;

    void Awake()
    {
        // 1 to 5 hits randomly, as requested
        health = Random.Range(1, 6);
        maxHealth = health;
    }

    void Start()
    {
        originalRot = transform.rotation;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        if (ForestManager.Instance != null)
        {
            ForestManager.Instance.RegisterTree();
        }
    }

    void OnDestroy()
    {
        if (ForestManager.Instance != null)
        {
            ForestManager.Instance.UnregisterTree();
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Vector3 fxPos = spawnPoint != null ? spawnPoint.position : transform.position;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeTreeVisual());

        if (health <= 0)
        {
            HitFXManager.Instance?.TriggerTreeFelled(fxPos);

            // Spawn log
            if (logPrefab != null)
            {
                Instantiate(logPrefab, fxPos, Quaternion.identity);
            }

            // Weapon Drop Chance (Comeback mechanic!)
            // Base 20% chance, boosts to 45% if cats are active or fuel is low!
            float weaponChance = 0.20f;
            int catCount = FindObjectsByType<GhostCatAI>(FindObjectsInactive.Exclude).Length;
            Bonfire bf = FindAnyObjectByType<Bonfire>();
            if (catCount >= 1 || (bf != null && bf.currentFuel < 30f))
            {
                weaponChance = 0.45f;
            }

            if (Random.value < weaponChance)
            {
                WeaponPickup.SpawnWeapon(fxPos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f), 0f));
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(15);
                GameManager.Instance.ShowNotification("Tree felled! Timber collected.");
            }

            Destroy(gameObject, 0.05f);
        }
        else
        {
            HitFXManager.Instance?.TriggerChopHit(fxPos);
        }
    }

    private IEnumerator ShakeTreeVisual()
    {
        if (sr != null) sr.color = new Color(1.2f, 1.2f, 1.2f, 1f); // subtle flash
        transform.rotation = originalRot * Quaternion.Euler(0, 0, -4f);
        yield return new WaitForSeconds(0.04f);
        transform.rotation = originalRot * Quaternion.Euler(0, 0, 3f);
        yield return new WaitForSeconds(0.04f);
        transform.rotation = originalRot;
        if (sr != null) sr.color = originalColor;
        shakeRoutine = null;
    }
}

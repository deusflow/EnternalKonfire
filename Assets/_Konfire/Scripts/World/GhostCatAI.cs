// GhostCatAI.cs
// Spooky ghost cat AI with menacing URP 2D aura

using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class GhostCatAI : MonoBehaviour
{
    public float moveSpeed = 1.2f;
    private Transform playerTransform;
    private bool canKill = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("GhostCatAI: No object with tag 'Player' found. Disabling AI.");
            enabled = false;
            return;
        }

        rb = GetComponentInParent<Rigidbody2D>() ?? GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        // Add spooky menacing purple light
        var catLight = GetComponentInChildren<Light2D>();
        if (catLight == null)
        {
            GameObject lightGO = new GameObject("GhostCat_Glow");
            lightGO.transform.SetParent(transform, false);
            catLight = lightGO.AddComponent<Light2D>();
            catLight.lightType = Light2D.LightType.Point;
            catLight.color = new Color(0.85f, 0.2f, 1f, 1f); // Menacing spectral violet
            catLight.intensity = 1.2f;
            catLight.pointLightInnerRadius = 0.8f;
            catLight.pointLightOuterRadius = 4.0f;
        }

        StartCoroutine(GracePeriod());
    }

    IEnumerator GracePeriod()
    {
        yield return new WaitForSeconds(2f);
        canKill = true; 
        Debug.Log("The cat started hunting!");
    }

    void FixedUpdate()
    {
        if (!enabled) return;
        if (playerTransform != null && canKill && rb != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            if (sr != null && Mathf.Abs(direction.x) > 0.05f)
            {
                sr.flipX = direction.x < 0;
            }
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canKill && other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame("The Ghost Cat caught you");
            }
            else
            {
                Debug.LogError("GhostCatAI: GameManager.Instance == null, can't end game.");
            }
        }
    }
}

// GhostCatAI.cs
// This script controls the spooky ghost cat's AI.
// Comments are written by a student for other students!

using UnityEngine;
using System.Collections;

public class GhostCatAI : MonoBehaviour
{
    public float moveSpeed = 1f;
    private Transform playerTransform;
    private bool canKill = false;

    private Rigidbody2D rb; // Reference to Rigidbody2D for movement

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

        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (rb == null)
        {
            Debug.LogError("GhostCatAI: No Rigidbody2D found on object or parent. Disabling AI.");
            enabled = false;
            return;
        }

        StartCoroutine(GracePeriod());
    }

    IEnumerator GracePeriod()
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds before cat can kill
        canKill = true; 
        Debug.Log("The cat started hunting!");
    }

    // We moved the movement logic from Update to FixedUpdate
    // This is the right place for physics-based movement
    void FixedUpdate()
    {
        if (!enabled) return;
        if (playerTransform != null && canKill)
        {
            // Find direction to player
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            // Move the cat using physics
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // This code stays unchanged, it works fine
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canKill && other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame("The cat caught you");
            }
            else
            {
                Debug.LogError("GhostCatAI: GameManager.Instance == null, can't end game.");
            }
        }
    }
}
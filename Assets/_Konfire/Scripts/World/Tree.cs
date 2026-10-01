// Tree.cs
// This script handles tree health, log spawning, and tree registration.
// Comments are written by a student for other students!

using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health;
    public GameObject logPrefab;
    public Transform spawnPoint;
    private Animator animator; // Reference to Animator for tree hit animation

    void Start()
    {
        health = Random.Range(2, 6); // Trees have random health between 2 and 5
        animator = GetComponent<Animator>(); 
        if (animator != null)
        {
            animator.ResetTrigger("isHit");
        }
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
        Debug.Log("Tree health: " + health);
        if (animator != null)
        {
            animator.SetTrigger("isHit");
        }

        if (health <= 0)
        {
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            if (logPrefab != null)
            {
                Instantiate(logPrefab, spawnPos, Quaternion.identity);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(10);
            }
            Destroy(gameObject);
        }
    }
}
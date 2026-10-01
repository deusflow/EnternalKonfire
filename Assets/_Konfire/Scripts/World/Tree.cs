using UnityEngine;
using System.Collections;

public class Tree : MonoBehaviour
{
    public int health;
    public GameObject logPrefab;
    public Transform spawnPoint;
    private Animator animator;
    private Coroutine shakeRoutine;
    private Quaternion originalRot;

    void Start()
    {
        health = Random.Range(2, 4); // 2-3 hits as per GDD
        originalRot = transform.rotation;
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

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeTree());

        Vector3 fxPos = spawnPoint != null ? spawnPoint.position : transform.position;

        if (health <= 0)
        {
            HitFXManager.Instance?.TriggerTreeFelled(fxPos);

            if (logPrefab != null)
            {
                Instantiate(logPrefab, fxPos, Quaternion.identity);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(10);
                int left = ForestManager.Instance != null ? ForestManager.Instance.treeCount - 1 : 0;
                GameManager.Instance.ShowNotification("Tree chopped! Log dropped on ground.");
            }
            Destroy(gameObject, 0.08f);
        }
        else
        {
            HitFXManager.Instance?.TriggerChopHit(fxPos);
        }
    }

    private IEnumerator ShakeTree()
    {
        transform.rotation = originalRot * Quaternion.Euler(0, 0, -6f);
        yield return new WaitForSeconds(0.04f);
        transform.rotation = originalRot * Quaternion.Euler(0, 0, 5f);
        yield return new WaitForSeconds(0.04f);
        transform.rotation = originalRot * Quaternion.Euler(0, 0, -2f);
        yield return new WaitForSeconds(0.04f);
        transform.rotation = originalRot;
        shakeRoutine = null;
    }
}

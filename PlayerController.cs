// PlayerController.cs
// FINAL VERSION (with full sound and buff tuning)
//
// This script controls the player character. Comments are written for students by a student :)

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Needed for ActivateFuryBuff coroutine

public class PlayerController : MonoBehaviour
{
    // --- Tweakable variables for the Inspector ---
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Interaction Settings")]
    public float interactRadius = 1f;
    public LayerMask treeLayer;
    public GameObject thoughtBubble;
    public GameObject altarGlow;
    public GameObject blueAltarGlow; 

    // --- Audio slots for sound effects ---
    [Header("Audio Setup")]
    public AudioSource footstepSource; // Footstep sound (should be looped)
    public AudioSource axeChopSource;  // Axe chop sound (short SFX)

    // --- Internal script variables ---
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private bool isCarryingLog = false;
    private bool isFuryActive = false;

    // --- AWAKE method ---
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (thoughtBubble != null) thoughtBubble.SetActive(false);
    }

    // --- ONMOVE method (updated) ---
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f) 
        { 
            animator.SetBool("isWalking", true); 
            animator.SetFloat("moveX", moveInput.x); 
            animator.SetFloat("moveY", moveInput.y);
            // Play footstep sound if not already playing
            if (footstepSource != null && !footstepSource.isPlaying)
            {
                footstepSource.Play(); 
            }
        }
        else 
        { 
            animator.SetBool("isWalking", false); 
            // Stop footstep sound if player stops
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop(); 
            }
        }
    }

    // --- ONCHOP method (updated) ---
    public void OnChop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        // Play axe chop sound every time space is pressed
        if (axeChopSource != null)
        {
            // PlayOneShot lets us overlap sounds if player spams the button
            axeChopSource.PlayOneShot(axeChopSource.clip);
        }
        animator.SetTrigger("isChopping");

        // If carrying a log, try to deliver it to the bonfire
        if (isCarryingLog)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Collider2D nearestBonfire = null;
            float minDistSq = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.CompareTag("Bonfire")) continue;
                float d = (hit.transform.position - transform.position).sqrMagnitude;
                if (d < minDistSq)
                {
                    minDistSq = d;
                    nearestBonfire = hit;
                }
            }
            if (nearestBonfire != null)
            {
                var bonfire = nearestBonfire.GetComponent<Bonfire>();
                if (bonfire != null)
                {
                    bonfire.AddFuel(25); // Give 25 fuel instead of 10
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
                    isCarryingLog = false;
                    if (thoughtBubble != null) thoughtBubble.SetActive(false);
                    Debug.Log("Log delivered to bonfire!");
                }
                else
                {
                    Debug.LogWarning("Object with Bonfire tag nearby doesn't have Bonfire component.");
                }
            }
            else
            {
                Debug.Log("No bonfire nearby to deliver the log.");
            }
            return;
        }

        // If not carrying a log, try to pick up the nearest one
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Collider2D nearestLog = null;
            float minDistSq = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.CompareTag("Log")) continue;
                float d = (hit.transform.position - transform.position).sqrMagnitude;
                if (d < minDistSq)
                {
                    minDistSq = d;
                    nearestLog = hit;
                }
            }
            if (nearestLog != null)
            {
                isCarryingLog = true;
                if (thoughtBubble != null) thoughtBubble.SetActive(true);
                Destroy(nearestLog.gameObject);
                Debug.Log("Picked up a log!");
                return;
            }
        }

        // If no log, try to chop the nearest tree
        Collider2D[] treeHits = Physics2D.OverlapCircleAll(transform.position, interactRadius, treeLayer);
        if (treeHits != null && treeHits.Length > 0)
        {
            Collider2D nearestTree = null;
            float minDistSq = float.MaxValue;
            foreach (var hit in treeHits)
            {
                if (hit == null) continue;
                if (hit.GetComponent<Tree>() == null) continue;
                float d = (hit.transform.position - transform.position).sqrMagnitude;
                if (d < minDistSq)
                {
                    minDistSq = d;
                    nearestTree = hit;
                }
            }
            if (nearestTree != null)
            {
                int damage = isFuryActive ? 999 : 1;
                nearestTree.GetComponent<Tree>()?.TakeDamage(damage);
            }
        }
    }

    // --- FIXEDUPDATE method ---
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // --- Buff logic ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BuffZone"))
        {
            if (altarGlow != null && altarGlow.activeSelf)
            {
                StartCoroutine(ActivateFuryBuff());
                altarGlow.SetActive(false);
            }
            else if (blueAltarGlow != null && blueAltarGlow.activeSelf)
            {
                ActivateForestWardBuff();
                blueAltarGlow.SetActive(false);
            }
        }
    }

    void ActivateForestWardBuff()
    {
        Debug.Log("FOREST WARD ACTIVATED!");
        Tree[] allTrees = FindObjectsByType<Tree>(FindObjectsSortMode.None);
        int treesToDestroy = 5;
        for (int i = 0; i < allTrees.Length && i < treesToDestroy; i++)
        {
            // Check allTrees.Length to avoid errors
            if (allTrees[i] != null)
            {
                Destroy(allTrees[i].gameObject);
            }
        }
    }

    // --- Fury buff coroutine ---
    System.Collections.IEnumerator ActivateFuryBuff()
    {
        Debug.Log("FURY BUFF ACTIVATED!");
        isFuryActive = true;
        // Buff lasts for 7 seconds
        yield return new WaitForSeconds(7f);
        isFuryActive = false;
        Debug.Log("Fury buff ended.");
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;

    [Header("Interaction Settings")]
    public float interactRadius = 3.2f;
    public LayerMask treeLayer;
    public GameObject thoughtBubble;
    public GameObject altarGlow;
    public GameObject blueAltarGlow;

    [Header("Audio Setup")]
    public AudioSource footstepSource;
    public AudioSource axeChopSource;
    public AudioSource pickupSource;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lastFacingDirection = Vector2.down;
    public bool isCarryingLog = false;
    public bool isFuryActive = false;
    private float lastChopTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (thoughtBubble != null) thoughtBubble.SetActive(false);
    }

    void Update()
    {
        // 1. Direct Input Polling (Works seamlessly in Legacy, New Input System, or Both)
        float h = 0f;
        float v = 0f;

        // Try Legacy Input (WASD & Arrow Keys)
        try
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }
        catch {}

        // Try New Input System (Keyboard)
#if ENABLE_INPUT_SYSTEM
        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f) && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
        }
#endif

        moveInput = new Vector2(h, v);

        // Update animation and footsteps
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = moveInput.normalized;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("isWalking", true);
                animator.SetFloat("moveX", lastFacingDirection.x);
                animator.SetFloat("moveY", lastFacingDirection.y);
            }
            if (footstepSource != null && footstepSource.clip != null && !footstepSource.isPlaying)
            {
                footstepSource.Play();
            }
        }
        else
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetFloat("moveX", lastFacingDirection.x);
                animator.SetFloat("moveY", lastFacingDirection.y);
            }
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }

        // 2. Spacebar Chop / Action Polling
        bool spacePressed = false;
        try { spacePressed = Input.GetKeyDown(KeyCode.Space); } catch {}

#if ENABLE_INPUT_SYSTEM
        if (!spacePressed && Keyboard.current != null)
        {
            spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#endif

        if (spacePressed)
        {
            ExecuteChopAction();
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector2 targetVel = moveInput.normalized * moveSpeed;
            rb.linearVelocity = targetVel;
            rb.MovePosition(rb.position + targetVel * Time.fixedDeltaTime);
        }
    }

    // Input System Callback support
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnChop()
    {
        ExecuteChopAction();
    }

    public void OnChop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ExecuteChopAction();
        }
    }

    public void ExecuteChopAction()
    {
        if (Time.unscaledTime - lastChopTime < 0.15f) return;
        lastChopTime = Time.unscaledTime;

        // Play axe swing sound & animation
        if (axeChopSource != null && axeChopSource.clip != null)
        {
            axeChopSource.PlayOneShot(axeChopSource.clip);
        }
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("moveX", lastFacingDirection.x);
            animator.SetFloat("moveY", lastFacingDirection.y);
            animator.SetTrigger("isChopping");
        }

        // Case 1: If carrying a log, deliver to Bonfire
        if (isCarryingLog)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Bonfire targetBonfire = null;
            float minDistSq = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                var b = hit.GetComponent<Bonfire>() ?? hit.GetComponentInParent<Bonfire>();
                if (b == null && hit.CompareTag("Bonfire")) b = FindAnyObjectByType<Bonfire>();
                if (b != null)
                {
                    float d = (hit.transform.position - transform.position).sqrMagnitude;
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        targetBonfire = b;
                    }
                }
            }

            if (targetBonfire == null)
            {
                // Fallback check by direct distance to any active Bonfire
                Bonfire anyBf = FindAnyObjectByType<Bonfire>();
                if (anyBf != null && Vector2.Distance(transform.position, anyBf.transform.position) <= interactRadius + 1.5f)
                {
                    targetBonfire = anyBf;
                }
            }

            if (targetBonfire != null)
            {
                targetBonfire.AddFuel(25f);
                if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
                isCarryingLog = false;
                if (thoughtBubble != null) thoughtBubble.SetActive(false);
                GameManager.Instance?.ShowNotification("Log sacrificed to the bonfire! (+50 pts)");
            }
            else
            {
                GameManager.Instance?.ShowNotification("No bonfire nearby to offer the log.");
            }
            return;
        }

        // Case 2: If NOT carrying log, check for nearby Logs on ground to pick up
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Collider2D nearestLog = null;
            float minDistSq = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit == null || !hit.CompareTag("Log")) continue;
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
                if (pickupSource != null && pickupSource.clip != null)
                {
                    pickupSource.PlayOneShot(pickupSource.clip);
                }
                Destroy(nearestLog.gameObject);
                GameManager.Instance?.ShowNotification("Picked up a log! Deliver it to the bonfire.");
                return;
            }
        }

        // Case 3: If no log, chop nearest tree
        {
            Collider2D[] treeHits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Tree nearestTree = null;
            float minDistSq = float.MaxValue;
            foreach (var hit in treeHits)
            {
                if (hit == null) continue;
                var t = hit.GetComponent<Tree>() ?? hit.GetComponentInParent<Tree>();
                if (t != null)
                {
                    float d = (hit.transform.position - transform.position).sqrMagnitude;
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        nearestTree = t;
                    }
                }
            }

            if (nearestTree != null)
            {
                int damage = isFuryActive ? 999 : 1;
                nearestTree.TakeDamage(damage);
            }
        }
    }

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
        GameManager.Instance?.ShowNotification("SACRED FOREST WARD ACTIVATED! 5 trees cleansed!");
        Tree[] allTrees = FindObjectsByType<Tree>(FindObjectsInactive.Exclude);
        int treesToDestroy = 5;
        for (int i = 0; i < allTrees.Length && i < treesToDestroy; i++)
        {
            if (allTrees[i] != null)
            {
                Destroy(allTrees[i].gameObject);
            }
        }
    }

    IEnumerator ActivateFuryBuff()
    {
        GameManager.Instance?.ShowNotification("FURY OF THE WOODCUTTER ACTIVATED! (10s 1-hit chop)");
        isFuryActive = true;
        yield return new WaitForSeconds(10f);
        isFuryActive = false;
        GameManager.Instance?.ShowNotification("Fury buff expired.");
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Interaction Settings")]
    public float interactRadius = 2.5f;
    public LayerMask treeLayer;
    public GameObject thoughtBubble;
    public GameObject altarGlow;
    public GameObject blueAltarGlow;

    [Header("Audio Setup")]
    public AudioSource footstepSource;
    public AudioSource axeChopSource;
    public AudioSource pickupSource;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.down;
    private Animator animator;
    public bool isCarryingLog = false;
    public bool isFuryActive = false;
    private PlayerInput playerInput;
    private bool registeredEvents = false;
    private float lastChopTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (thoughtBubble != null) thoughtBubble.SetActive(false);
    }

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            var moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }
            var chopAction = playerInput.actions.FindAction("Chop");
            if (chopAction != null)
            {
                chopAction.performed += OnChop;
            }
            registeredEvents = true;
        }
    }

    void OnDestroy()
    {
        if (registeredEvents && playerInput != null && playerInput.actions != null)
        {
            var moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }
            var chopAction = playerInput.actions.FindAction("Chop");
            if (chopAction != null)
            {
                chopAction.performed -= OnChop;
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f)
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
    }

    public void OnChop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.unscaledTime - lastChopTime < 0.15f) return;
        lastChopTime = Time.unscaledTime;

        // Trigger chop animation and sound in facing direction
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

        // Case 1: If carrying a log, try to deliver to the Bonfire
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

            if (targetBonfire != null)
            {
                targetBonfire.AddFuel(25f);
                if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
                isCarryingLog = false;
                if (thoughtBubble != null) thoughtBubble.SetActive(false);
                GameManager.Instance?.ShowNotification("Полено доставлено в костёр! (+50 очков)");
                Debug.Log("Log delivered to bonfire!");
            }
            else
            {
                GameManager.Instance?.ShowNotification("Рядом нет костра для доставки полена.");
                Debug.Log("No bonfire nearby to deliver the log.");
            }
            return;
        }

        // Case 2: If NOT carrying a log, check for nearby Logs on the ground to pick up
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
                if (pickupSource != null && pickupSource.clip != null)
                {
                    pickupSource.PlayOneShot(pickupSource.clip);
                }
                Destroy(nearestLog.gameObject);
                GameManager.Instance?.ShowNotification("Полено подобрано! Отнеси его к костру.");
                Debug.Log("Picked up a log!");
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

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
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
        GameManager.Instance?.ShowNotification("СВЯЩЕННЫЙ ОБЕРЕГ АКТИВИРОВАН! 5 деревьев уничтожено!");
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
        GameManager.Instance?.ShowNotification("ЯРОСТЬ ДРОВОСЕКА АКТИВИРОВАНА на 10 сек! (Рубка с 1 удара)");
        isFuryActive = true;
        yield return new WaitForSeconds(10f);
        isFuryActive = false;
        GameManager.Instance?.ShowNotification("Действие Ярости закончилось.");
    }
}

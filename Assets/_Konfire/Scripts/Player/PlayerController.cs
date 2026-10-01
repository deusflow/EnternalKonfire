using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 3.2f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private Vector2 lastFacingDirection = Vector2.down;

    [Header("Interaction & Inventory")]
    public bool isCarryingLog = false;
    public float interactRadius = 1.6f;
    public GameObject thoughtBubble;

    [Header("Combat & Weapon")]
    public bool hasWeapon = false;
    public int weaponCharges = 0;
    public GameObject weaponVisual;

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioSource axeChopSource;
    public AudioSource pickupSource;

    [Header("Legacy / Altar References")]
    public GameObject altarGlow;
    public GameObject blueAltarGlow;

    private float lastActionTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (thoughtBubble != null) thoughtBubble.SetActive(false);
        if (weaponVisual != null) weaponVisual.SetActive(false);
    }

    void Update()
    {
        // 1. Movement Input Polling (Dual-stack support)
        float h = 0f;
        float v = 0f;

        try
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }
        catch {}

#if ENABLE_INPUT_SYSTEM
        if (h == 0 && v == 0 && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
            else if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
        }
#endif

        if (moveInput == Vector2.zero || (h != 0 || v != 0))
        {
            moveInput = new Vector2(h, v);
        }

        // Animation update
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (isMoving)
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

        // 2. Spacebar Action Polling
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
            ExecuteAction();
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            float currentSpeed = baseMoveSpeed;

            // Speed reduction if carrying heavy log
            if (isCarryingLog) currentSpeed *= 0.85f;

            // Speed Buff
            if (BuffManager.Instance != null && BuffManager.Instance.activeBuff == BuffType.SpeedBoost)
            {
                currentSpeed *= 1.6f;
            }

            // Low Fuel Panic Sprint (Comeback mechanic!)
            Bonfire bf = FindAnyObjectByType<Bonfire>();
            if (bf != null && bf.currentFuel < 20f)
            {
                currentSpeed *= 1.15f;
            }

            Vector2 targetVel = moveInput.normalized * currentSpeed;
            rb.linearVelocity = targetVel;
        }
    }

    // Input System Callbacks
    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    public void OnChop() { ExecuteAction(); }

    public void AcquireWeapon(int charges)
    {
        hasWeapon = true;
        weaponCharges += charges;
        if (weaponVisual != null) weaponVisual.SetActive(true);
        if (pickupSource != null) pickupSource.Play();
        GameManager.Instance?.ShowNotification($"HOLY SPIRIT AXE! ({weaponCharges} strikes against Ghost Cats)");
        TutorialManager.Instance?.TriggerWeaponTutorial();
    }

    public void UseWeaponStrike(GhostCatAI cat)
    {
        if (!hasWeapon) return;

        weaponCharges--;
        if (weaponCharges <= 0)
        {
            hasWeapon = false;
            if (weaponVisual != null) weaponVisual.SetActive(false);
            GameManager.Instance?.ShowNotification("Holy weapon shattered!");
        }
        else
        {
            GameManager.Instance?.ShowNotification($"Cat banished! ({weaponCharges} weapon strikes left)");
        }

        cat.Banish();
    }

    public void ExecuteAction()
    {
        if (Time.unscaledTime - lastActionTime < 0.22f) return;
        lastActionTime = Time.unscaledTime;

        // Play swing sound & animation
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

        // Priority 1: If has weapon, strike nearby Ghost Cat!
        if (hasWeapon)
        {
            Collider2D[] catHits = Physics2D.OverlapCircleAll(transform.position, interactRadius + 0.6f);
            foreach (var hit in catHits)
            {
                if (hit == null) continue;
                var cat = hit.GetComponent<GhostCatAI>() ?? hit.GetComponentInParent<GhostCatAI>();
                if (cat != null)
                {
                    UseWeaponStrike(cat);
                    return;
                }
            }
        }

        // Priority 2: If carrying a log, deliver to Bonfire
        if (isCarryingLog)
        {
            Bonfire targetBonfire = FindAnyObjectByType<Bonfire>();
            if (targetBonfire != null && Vector2.Distance(transform.position, targetBonfire.transform.position) <= interactRadius + 2.2f)
            {
                targetBonfire.AddFuel(25f);
                CameraShake.Instance?.Shake(0.04f, 0.08f);
                if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
                isCarryingLog = false;
                if (thoughtBubble != null) thoughtBubble.SetActive(false);
                AltarManager.Instance?.OnLogDelivered();
                GameManager.Instance?.ShowNotification("Log offered to the sacred fire! (+50 pts)");
                return;
            }
            else
            {
                GameManager.Instance?.ShowNotification("Get closer to the bonfire to offer the log.");
                return;
            }
        }

        // Priority 3: Pick up nearby Log on ground
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
                GameManager.Instance?.ShowNotification("Picked up timber! Deliver it to the bonfire.");
                return;
            }
        }

        // Priority 4: Chop nearest Tree
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
                bool isFury = BuffManager.Instance != null && BuffManager.Instance.activeBuff == BuffType.WoodcutterFury;
                int damage = isFury ? 999 : 1;
                nearestTree.TakeDamage(damage);
            }
        }
    }
}

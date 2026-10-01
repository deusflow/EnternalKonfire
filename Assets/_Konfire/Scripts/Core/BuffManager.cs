using UnityEngine;
using System.Collections;
using System;

public enum BuffType
{
    None,
    SpeedBoost,     // +60% movement speed (12s)
    WoodcutterFury, // 1-hit tree chop (10s)
    ForestCleanse,  // Instantly fell nearby trees
    CatRepel        // Holy aura pushes ghost cats away (12s)
}

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    [Header("Active Buff State")]
    public BuffType activeBuff = BuffType.None;
    public float buffDurationRemaining = 0f;
    public float maxBuffDuration = 10f;

    public event Action<BuffType, float, float> OnBuffUpdated; // buff, remaining, max
    public event Action OnBuffExpired;

    private PlayerController player;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        if (activeBuff != BuffType.None)
        {
            buffDurationRemaining -= Time.deltaTime;
            OnBuffUpdated?.Invoke(activeBuff, buffDurationRemaining, maxBuffDuration);

            if (buffDurationRemaining <= 0f)
            {
                ExpireBuff();
            }
        }
    }

    public void ApplyBuff(BuffType type, float duration)
    {
        activeBuff = type;
        maxBuffDuration = duration;
        buffDurationRemaining = duration;

        if (type == BuffType.ForestCleanse)
        {
            ExecuteForestCleanse();
            // Cleanse is an instant blast, keep a short flash
            buffDurationRemaining = 2f;
            maxBuffDuration = 2f;
        }

        OnBuffUpdated?.Invoke(activeBuff, buffDurationRemaining, maxBuffDuration);

        string buffName = GetBuffDisplayName(type);
        GameManager.Instance?.ShowNotification($"BUFF ACTIVATED: {buffName}!");
    }

    private void ExecuteForestCleanse()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        Tree[] trees = FindObjectsByType<Tree>(FindObjectsInactive.Exclude);
        int felled = 0;
        foreach (var t in trees)
        {
            if (t != null && Vector2.Distance(center, t.transform.position) <= 6.0f)
            {
                t.TakeDamage(999);
                felled++;
            }
        }

        CameraShake.Instance?.Shake(0.08f, 0.15f);
        GameManager.Instance?.ShowNotification($"DRUIDIC SHOCKWAVE: Cleared {felled} trees!");
    }

    private void ExpireBuff()
    {
        activeBuff = BuffType.None;
        buffDurationRemaining = 0f;
        OnBuffExpired?.Invoke();
        GameManager.Instance?.ShowNotification("Buff expired.");
    }

    public static string GetBuffDisplayName(BuffType type)
    {
        switch (type)
        {
            case BuffType.SpeedBoost: return "⚡ Swift Wind (+60% Speed)";
            case BuffType.WoodcutterFury: return "🪓 Woodcutter Fury (1-Hit Chop)";
            case BuffType.ForestCleanse: return "🌿 Druidic Cleanse (Ring Blast)";
            case BuffType.CatRepel: return "🛡️ Spirit Ward (Cats Repelled)";
            default: return "";
        }
    }
}

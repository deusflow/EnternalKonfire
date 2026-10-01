using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffWeaponHUD : MonoBehaviour
{
    [Header("Buff UI")]
    public GameObject buffContainer;
    public Image buffIcon;
    public TMP_Text buffText;
    public Slider buffTimerSlider;

    [Header("Weapon UI")]
    public GameObject weaponContainer;
    public Image weaponIcon;
    public TMP_Text weaponChargesText;

    private PlayerController player;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        if (buffContainer != null) buffContainer.SetActive(false);
        if (weaponContainer != null) weaponContainer.SetActive(false);

        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffUpdated += HandleBuffUpdated;
            BuffManager.Instance.OnBuffExpired += HandleBuffExpired;
        }
    }

    void OnDestroy()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffUpdated -= HandleBuffUpdated;
            BuffManager.Instance.OnBuffExpired -= HandleBuffExpired;
        }
    }

    void Update()
    {
        // Update Weapon HUD
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null && weaponContainer != null)
        {
            if (player.hasWeapon && player.weaponCharges > 0)
            {
                weaponContainer.SetActive(true);
                if (weaponChargesText != null)
                {
                    weaponChargesText.text = $"x{player.weaponCharges}";
                }
            }
            else
            {
                weaponContainer.SetActive(false);
            }
        }
    }

    private void HandleBuffUpdated(BuffType type, float remaining, float maxDuration)
    {
        if (buffContainer == null) return;

        if (type == BuffType.None || remaining <= 0f)
        {
            buffContainer.SetActive(false);
            return;
        }

        buffContainer.SetActive(true);
        if (buffTimerSlider != null)
        {
            buffTimerSlider.maxValue = maxDuration;
            buffTimerSlider.value = remaining;
        }

        if (buffText != null)
        {
            buffText.text = $"{BuffManager.GetBuffDisplayName(type)} ({remaining:F1}s)";
        }

        if (buffIcon != null)
        {
            string spriteName = "";
            switch (type)
            {
                case BuffType.SpeedBoost: spriteName = "Buff_Speed"; break;
                case BuffType.WoodcutterFury: spriteName = "Buff_Fury"; break;
                case BuffType.ForestCleanse: spriteName = "Buff_Cleanse"; break;
                case BuffType.CatRepel: spriteName = "Buff_Repel"; break;
            }

            #if UNITY_EDITOR
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Konfire/Sprites/Items/{spriteName}.png");
            if (s != null) buffIcon.sprite = s;
            #endif
        }
    }

    private void HandleBuffExpired()
    {
        if (buffContainer != null) buffContainer.SetActive(false);
    }
}

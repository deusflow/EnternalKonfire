using UnityEngine;
using UnityEngine.UI;

public class Bonfire : MonoBehaviour
{
    public float maxFuel = 100f;
    public float currentFuel = 100f;
    public float burnRate = 2f;

    public Slider fuelSlider;
    public GameObject altarGlow;
    public GameObject blueAltarGlow;

    [Header("Buff Tracking (GDD)")]
    public int mediumFuelDeliveries = 0;
    public int highFuelDeliveries = 0;

    [Header("Audio Setup")]
    public AudioSource logBurnSource;

    void Start()
    {
        if (currentFuel <= 0) currentFuel = maxFuel;
        if (fuelSlider != null) fuelSlider.maxValue = maxFuel;
        if (altarGlow != null) altarGlow.SetActive(false);
        if (blueAltarGlow != null) blueAltarGlow.SetActive(false);
        UpdateFuelSlider();
    }

    void Update()
    {
        if (currentFuel > 0.001f)
        {
            currentFuel -= burnRate * Time.deltaTime;
            UpdateFuelSlider();
        }
        else
        {
            currentFuel = 0f;
            UpdateFuelSlider();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame("The bonfire went out");
            }
            this.enabled = false;
        }
    }

    public void AddFuel(float amount)
    {
        if (logBurnSource != null)
        {
            logBurnSource.Play();
        }

        currentFuel += amount;
        if (currentFuel > maxFuel) currentFuel = maxFuel;
        UpdateFuelSlider();

        // GDD Buff Trigger Logic
        // Rosk Bøf (Fury): Deliver 2 logs while fuel is medium (40 - 80)
        if (currentFuel >= 40f && currentFuel <= 80f)
        {
            mediumFuelDeliveries++;
            if (mediumFuelDeliveries >= 2)
            {
                mediumFuelDeliveries = 0;
                if (altarGlow != null)
                {
                    altarGlow.SetActive(true);
                    GameManager.Instance?.ShowNotification("АЛТАРЬ ЗАРЯЖЕН! Розовая аура: Ярость лесоруба готова!");
                }
            }
        }
        // Blå Bøf (Forest Ward): Deliver 3 logs while fuel is roaring (80+)
        else if (currentFuel > 80f)
        {
            highFuelDeliveries++;
            if (highFuelDeliveries >= 3)
            {
                highFuelDeliveries = 0;
                if (blueAltarGlow != null)
                {
                    blueAltarGlow.SetActive(true);
                    GameManager.Instance?.ShowNotification("АЛТАРЬ ЗАРЯЖЕН! Синяя аура: Священный оберег леса готов!");
                }
            }
        }
    }

    void UpdateFuelSlider()
    {
        if (fuelSlider != null)
        {
            fuelSlider.value = currentFuel;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Log"))
        {
            AddFuel(25f);
            if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
            Destroy(collision.gameObject);
        }
    }
}

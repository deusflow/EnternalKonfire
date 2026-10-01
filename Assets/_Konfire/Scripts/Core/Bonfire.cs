using UnityEngine;
using UnityEngine.UI;

public class Bonfire : MonoBehaviour
{
    public float maxFuel = 100f;
    public float currentFuel = 100f;
    public float baseBurnRate = 2f;

    public Slider fuelSlider;
    public AudioSource logBurnSource;

    [Header("Legacy / Altar Glow References")]
    public GameObject altarGlow;
    public GameObject blueAltarGlow;

    void Start()
    {
        if (currentFuel <= 0) currentFuel = maxFuel;
        if (fuelSlider != null) fuelSlider.maxValue = maxFuel;
        UpdateFuelSlider();
    }

    void Update()
    {
        if (currentFuel > 0.001f)
        {
            // Comeback mechanic: when fuel is critical (< 20%), embers smolder slower
            float effectiveBurnRate = currentFuel < 20f ? (baseBurnRate * 0.5f) : baseBurnRate;
            currentFuel -= effectiveBurnRate * Time.deltaTime;
            UpdateFuelSlider();
        }
        else
        {
            currentFuel = 0f;
            UpdateFuelSlider();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame("The sacred bonfire went out");
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
            AltarManager.Instance?.OnLogDelivered();
            Destroy(collision.gameObject);
        }
    }
}

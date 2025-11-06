/*
 * Bonfire.cs
 * UPDATED: Buff logic is now random (RNG).
 *
 * This script manages the bonfire's fuel, buffs, and related sounds.
 * Comments are written by a student for other students!
 */

using UnityEngine;
using UnityEngine.UI; 

public class Bonfire : MonoBehaviour
{
    public float maxFuel = 100f;
    public float currentFuel; 
    public float burnRate = 2f; 

    public Slider fuelSlider;
    public GameObject altarGlow;
    public GameObject blueAltarGlow; 
    
    [Header("Buff Settings")]
    [Tooltip("Chance (0.0 to 1.0) for a buff to appear when you deliver a log")]
    [Range(0, 1)]
    public float buffChance = 0.25f; // 25% chance by default

    [Header("Audio Setup")]
    public AudioSource logBurnSource; 
    
    void Start()
    {
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

    // --- ADDFUEL method (major update) ---
    public void AddFuel(float amount)
    {
        // 1. Play log burn sound
        if (logBurnSource != null)
        {
            logBurnSource.Play();
        }
        // 2. Add fuel
        currentFuel += amount;
        if (currentFuel > maxFuel) currentFuel = maxFuel;
        UpdateFuelSlider();
        // 3. Check if altar is clear (no active buff)
        bool altarIsClear = (altarGlow != null && !altarGlow.activeSelf) && 
                            (blueAltarGlow != null && !blueAltarGlow.activeSelf);
        if (altarIsClear)
        {
            // 4. Roll the dice for a random buff
            if (Random.value <= buffChance) 
            {
                Debug.Log("LUCKY! The altar is charging with a random buff...");
                // 5. 50/50 chance for which buff you get
                if (Random.value <= 0.5f)
                {
                    // Pink buff (Fury)
                    altarGlow.SetActive(true);
                }
                else
                {
                    // Blue buff (Ward)
                    blueAltarGlow.SetActive(true);
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
            AddFuel(10f); 
            Destroy(collision.gameObject); 
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BonfireLightFlicker : MonoBehaviour
{
    private Light2D fireLight;
    private Bonfire bonfire;
    public float flickerSpeed = 5.5f;

    void Awake()
    {
        fireLight = GetComponent<Light2D>();
        bonfire = FindAnyObjectByType<Bonfire>();
    }

    void Update()
    {
        if (fireLight == null) return;

        float fuelRatio = 1f;
        if (bonfire != null)
        {
            fuelRatio = Mathf.Clamp01(bonfire.currentFuel / bonfire.maxFuel);
        }

        // Base values scaled by fuel level
        float baseIntensity = Mathf.Lerp(0.85f, 2.3f, fuelRatio);
        float baseOuterRadius = Mathf.Lerp(6.5f, 16f, fuelRatio);
        float baseInnerRadius = Mathf.Lerp(1.2f, 3.5f, fuelRatio);

        // Organic flame flicker via Perlin noise
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensityJitter = (noise - 0.5f) * 0.35f * fuelRatio;
        float radiusJitter = (Mathf.PerlinNoise(0f, Time.time * (flickerSpeed * 0.8f)) - 0.5f) * 1.0f * fuelRatio;

        fireLight.intensity = Mathf.Max(0.2f, baseIntensity + intensityJitter);
        fireLight.pointLightOuterRadius = Mathf.Max(3f, baseOuterRadius + radiusJitter);
        fireLight.pointLightInnerRadius = Mathf.Max(0.5f, baseInnerRadius);
    }
}

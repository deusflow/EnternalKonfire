using UnityEngine;

public class FireGlowFlicker : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 initialScale;
    private Color baseColor;

    public float flickerSpeed = 5f;
    public float scaleVariation = 0.08f;
    public float alphaVariation = 0.12f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.5f);
        float scaleOffset = (noise - 0.5f) * 2f * scaleVariation;
        transform.localScale = initialScale + Vector3.one * scaleOffset;

        if (sr != null)
        {
            float a = Mathf.Clamp01(baseColor.a + (noise - 0.5f) * 2f * alphaVariation);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
        }
    }
}

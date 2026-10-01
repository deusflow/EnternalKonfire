using UnityEngine;
using System.Collections;

public class HitFXManager : MonoBehaviour
{
    public static HitFXManager Instance;

    private ParticleSystem hitParticlePool;
    private ParticleSystem fellParticlePool;
    private ParticleSystem catBanishPool;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        Material unlitMat = Resources.Load<Material>("URP2D_SpriteUnlit");
        if (unlitMat == null)
        {
            // Fallback load from Assets/Settings/
            #if UNITY_EDITOR
            unlitMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/URP2D_SpriteUnlit.mat");
            #endif
        }

        // 1. Wood & Leaf chop sparks (Warm amber wood + foliage green, NO purple!)
        hitParticlePool = CreateParticleSystem("ChopHitFX", unlitMat, 10,
            new Color(0.72f, 0.45f, 0.20f), // Warm Oak
            new Color(0.24f, 0.58f, 0.22f)  // Forest Leaf
        );

        // 2. Tree Felled burst
        fellParticlePool = CreateParticleSystem("TreeFellFX", unlitMat, 25,
            new Color(0.65f, 0.38f, 0.18f),
            new Color(0.20f, 0.62f, 0.25f)
        );

        // 3. Cat Banishment FX (Golden holy radiance)
        catBanishPool = CreateParticleSystem("CatBanishFX", unlitMat, 30,
            new Color(1.0f, 0.88f, 0.35f),  // Radiant Gold
            new Color(0.2f, 0.95f, 1.0f)   // Spirit Cyan
        );
    }

    private ParticleSystem CreateParticleSystem(string name, Material mat, int maxParticles, Color col1, Color col2)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 1.6f;
        main.maxParticles = maxParticles * 3;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(col1, 0.0f), new GradientColorKey(col2, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        renderer.sortingOrder = 20;

        return ps;
    }

    public void TriggerChopHit(Vector3 pos)
    {
        if (hitParticlePool != null)
        {
            hitParticlePool.transform.position = pos;
            hitParticlePool.Emit(6);
        }

        // Gentle, pleasant micro-shake
        CameraShake.Instance?.Shake(0.025f, 0.05f);

        // Very brief micro-hitstop for snappy feel
        StartCoroutine(HitStopRoutine(0.025f));
    }

    public void TriggerTreeFelled(Vector3 pos)
    {
        if (fellParticlePool != null)
        {
            fellParticlePool.transform.position = pos;
            fellParticlePool.Emit(20);
        }

        // Soft crunch shake
        CameraShake.Instance?.Shake(0.06f, 0.09f);
        StartCoroutine(HitStopRoutine(0.04f));
    }

    public void TriggerCatBanished(Vector3 pos)
    {
        if (catBanishPool != null)
        {
            catBanishPool.transform.position = pos;
            catBanishPool.Emit(25);
        }

        CameraShake.Instance?.Shake(0.07f, 0.10f);
        StartCoroutine(HitStopRoutine(0.05f));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float prevScale = Time.timeScale;
        if (prevScale > 0.1f)
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevScale;
        }
    }
}

using UnityEngine;
using System.Collections;

public class HitFXManager : MonoBehaviour
{
    public static HitFXManager Instance;

    private ParticleSystem hitParticlePool;
    private ParticleSystem fellParticlePool;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        hitParticlePool = CreateParticleSystem("ChopHitFX", 12, new Color(0.72f, 0.45f, 0.2f), new Color(0.2f, 0.65f, 0.25f));
        fellParticlePool = CreateParticleSystem("TreeFellFX", 40, new Color(0.65f, 0.4f, 0.18f), new Color(0.15f, 0.7f, 0.25f));
    }

    private ParticleSystem CreateParticleSystem(string name, int maxParticles, Color woodCol, Color leafCol)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.gravityModifier = 1.8f;
        main.maxParticles = 100;

        // Gradient color from wood to leaf
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(woodCol, 0.0f), new GradientColorKey(leafCol, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 18;

        return ps;
    }

    public void TriggerChopHit(Vector3 pos)
    {
        if (hitParticlePool != null)
        {
            hitParticlePool.transform.position = pos;
            hitParticlePool.Emit(8);
        }

        // Camera micro-shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.09f, 0.08f);
        }

        // Hit-stop micro-freeze
        StartCoroutine(HitStopRoutine(0.04f));
    }

    public void TriggerTreeFelled(Vector3 pos)
    {
        if (fellParticlePool != null)
        {
            fellParticlePool.transform.position = pos;
            fellParticlePool.Emit(30);
        }

        // Big crunch shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.24f, 0.16f);
        }

        // Deeper hit-stop
        StartCoroutine(HitStopRoutine(0.06f));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float prevScale = Time.timeScale;
        if (prevScale > 0.1f)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevScale;
        }
    }
}

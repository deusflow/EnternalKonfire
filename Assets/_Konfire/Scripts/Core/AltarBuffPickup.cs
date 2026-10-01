using UnityEngine;

public class AltarBuffPickup : MonoBehaviour
{
    public BuffType buffType;
    public float duration = 12f;
    private float startY;
    private SpriteRenderer sr;

    void Start()
    {
        startY = transform.position.y;
        sr = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    void Update()
    {
        // Gentle mystical floating animation
        float newY = startY + Mathf.Sin(Time.time * 4.0f) * 0.1f;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void UpdateSprite()
    {
        if (sr == null) return;
        string spriteName = "";
        switch (buffType)
        {
            case BuffType.SpeedBoost: spriteName = "Buff_Speed"; break;
            case BuffType.WoodcutterFury: spriteName = "Buff_Fury"; break;
            case BuffType.ForestCleanse: spriteName = "Buff_Cleanse"; break;
            case BuffType.CatRepel: spriteName = "Buff_Repel"; break;
        }

        #if UNITY_EDITOR
        var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Konfire/Sprites/Items/{spriteName}.png");
        if (s != null) sr.sprite = s;
        #endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            BuffManager.Instance?.ApplyBuff(buffType, duration);
            Destroy(gameObject);
        }
    }

    public static GameObject SpawnBuff(Vector3 pos, BuffType type, float dur = 12f)
    {
        GameObject go = new GameObject($"Buff_{type}_Pickup");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 9;

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
        if (s != null) sr.sprite = s;
        #endif

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var pickup = go.AddComponent<AltarBuffPickup>();
        pickup.buffType = type;
        pickup.duration = dur;

        return go;
    }
}

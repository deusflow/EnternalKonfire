using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public int charges = 3;
    private float startY;
    private SpriteRenderer sr;

    void Start()
    {
        startY = transform.position.y;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Gentle mystical floating animation
        float newY = startY + Mathf.Sin(Time.time * 4.5f) * 0.12f;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var pc = collision.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.AcquireWeapon(charges);
                Destroy(gameObject);
            }
        }
    }

    public static void SpawnWeapon(Vector3 position)
    {
        GameObject go = new GameObject("Weapon_SpiritAxe_Pickup");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Weapon_SpiritAxe");
        if (sprite == null)
        {
            #if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Konfire/Sprites/Items/Weapon_SpiritAxe.png");
            #endif
        }
        sr.sprite = sprite;
        sr.sortingOrder = 8;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<WeaponPickup>();
    }
}

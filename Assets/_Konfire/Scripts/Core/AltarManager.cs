using UnityEngine;

public class AltarManager : MonoBehaviour
{
    public static AltarManager Instance;

    public Transform leftPedestal;
    public Transform rightPedestal;

    private GameObject leftBuff;
    private GameObject rightBuff;

    private float timer = 0f;
    public float spawnInterval = 28f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Initial spawn on left pedestal
        SpawnRandomBuffOnPedestal(true);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            if (leftBuff == null) SpawnRandomBuffOnPedestal(true);
            else if (rightBuff == null) SpawnRandomBuffOnPedestal(false);
        }
    }

    public void OnLogDelivered()
    {
        // Reward: if either pedestal is empty, 50% chance to immediately spawn a buff!
        if (leftBuff == null && Random.value < 0.5f)
        {
            SpawnRandomBuffOnPedestal(true);
        }
        else if (rightBuff == null && Random.value < 0.5f)
        {
            SpawnRandomBuffOnPedestal(false);
        }
    }

    public void SpawnRandomBuffOnPedestal(bool isLeft)
    {
        Vector3 pos = isLeft
            ? (leftPedestal != null ? leftPedestal.position : new Vector3(-2.8f, 6.8f, 0f))
            : (rightPedestal != null ? rightPedestal.position : new Vector3(2.8f, 6.8f, 0f));

        BuffType[] types = new BuffType[] { BuffType.SpeedBoost, BuffType.WoodcutterFury, BuffType.ForestCleanse, BuffType.CatRepel };
        BuffType chosen = types[Random.Range(0, types.Length)];

        GameObject spawned = AltarBuffPickup.SpawnBuff(pos, chosen, 12f);
        if (isLeft) leftBuff = spawned;
        else rightBuff = spawned;
    }
}

using UnityEngine;
using System.Collections.Generic;

public class ForestManager : MonoBehaviour
{
    public static ForestManager Instance;

    [Header("Cat Spawning")]
    public int treeCount = 0;
    public int minTreesBeforeCat = 14;
    public int maxTreesBeforeCat = 20;
    public int actualCatLimit;
    public GameObject catPrefab;
    public Transform playerTransform;
    private bool isCatSpawned = false;

    [Header("Audio Setup")]
    public AudioSource catSpawnSource;

    [Header("Forest Growth")]
    public List<GameObject> treePrefabs;
    public float initialSpawnDelay = 10f;
    public float minSpawnDelay = 2f;
    public float difficultyIncreaseInterval = 25f;
    public float delayReduction = 0.5f;

    [Header("Spawn Boundaries")]
    public float spawnPadding = 1.0f;
    private Camera mainCamera;
    private float camHeight;
    private float camWidth;

    private float currentSpawnDelay;
    private float spawnTimer;
    private float difficultyTimer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        actualCatLimit = Random.Range(minTreesBeforeCat, maxTreesBeforeCat + 1);
        currentSpawnDelay = initialSpawnDelay;
        spawnTimer = currentSpawnDelay;
        difficultyTimer = difficultyIncreaseInterval;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            camHeight = 10f;
            camWidth = 18f;
        }
        else
        {
            camHeight = mainCamera.orthographicSize;
            camWidth = camHeight * mainCamera.aspect;
        }
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnTree();
            spawnTimer = currentSpawnDelay;
        }

        difficultyTimer -= Time.deltaTime;
        if (difficultyTimer <= 0)
        {
            IncreaseDifficulty();
            difficultyTimer = difficultyIncreaseInterval;
        }
    }

    void SpawnTree()
    {
        if (treePrefabs == null || treePrefabs.Count == 0) return;
        GameObject prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Count)];

        for (int i = 0; i < 15; i++)
        {
            float spawnX = Random.Range(-18f + spawnPadding, 18f - spawnPadding);
            float spawnY = Random.Range(-9f + spawnPadding, 9f - spawnPadding);
            Vector2 spawnPosition = new Vector2(spawnX, spawnY);

            // Check if too close to center altar platform (X: -6..6, Y: 0..9)
            if (spawnX >= -6f && spawnX <= 6f && spawnY >= -1f && spawnY <= 9.5f)
            {
                continue; // Do not spawn on Altar or stairs!
            }

            Collider2D treeHit = Physics2D.OverlapCircle(spawnPosition, 1.8f, LayerMask.GetMask("Trees"));
            Collider2D noSpawnHit = Physics2D.OverlapCircle(spawnPosition, 0.5f);
            bool isNoSpawnZone = (noSpawnHit != null && noSpawnHit.CompareTag("NoSpawn"));

            if (treeHit == null && !isNoSpawnZone)
            {
                GameObject newTree = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                float randomScale = Random.Range(0.85f, 1.25f);
                newTree.transform.localScale = new Vector3(randomScale, randomScale, 1f);
                return;
            }
        }
    }

    void IncreaseDifficulty()
    {
        if (currentSpawnDelay > minSpawnDelay)
        {
            currentSpawnDelay -= delayReduction;
            GameManager.Instance?.ShowNotification("THE FOREST THICKENS! Trees spawning faster!");
            Debug.Log("Forest is getting harder! New spawn delay: " + currentSpawnDelay);
        }
    }

    public void RegisterTree()
    {
        treeCount++;
        GameManager.Instance?.ShowNotification("Forest trees: " + treeCount + " / " + actualCatLimit);
        Debug.Log("Tree added! Total: " + treeCount + " / " + actualCatLimit);
        CheckForCatSpawn();
    }

    public void UnregisterTree()
    {
        treeCount--;
        Debug.Log("Tree removed! Left: " + treeCount);
    }

    void CheckForCatSpawn()
    {
        if (treeCount >= actualCatLimit && !isCatSpawned)
        {
            SpawnCat();
        }
    }

    void SpawnCat()
    {
        if (isCatSpawned) return;
        if (catPrefab == null) return;
        isCatSpawned = true;
        GameManager.Instance?.ShowNotification("THE FOREST GHOST HAS AWAKENED! RUN!");
        Debug.Log("The cat has spawned! Good luck :)");
        if (catSpawnSource != null)
        {
            catSpawnSource.Play();
        }
        Vector3 spawnPos = playerTransform != null ? playerTransform.position + new Vector3(8, 6, 0) : new Vector3(9f, 7.5f, 0);
        Instantiate(catPrefab, spawnPos, Quaternion.identity);
    }
}

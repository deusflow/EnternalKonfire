// ForestManager.cs
//
// Handles all the tree spawning and the spooky cat event. I tried to make the forest get harder over time.
// If you see any weird bugs, sorry! Let me know :)
//
// (Wrote these comments for my classmates and teachers. If something's confusing, just ask!)

using UnityEngine;
using System.Collections.Generic;

public class ForestManager : MonoBehaviour
{
    public static ForestManager Instance;

    [Header("Cat Spawning")]
    public int treeCount = 0;
    public int minTreesBeforeCat = 40; // Cat won't show up before this many trees
    public int maxTreesBeforeCat = 60; // ...or after this many
    private int actualCatLimit; // The real number is random each game
    public GameObject catPrefab;
    public Transform playerTransform;
    private bool isCatSpawned = false; // Only one cat per game!

    [Header("Audio Setup")]
    public AudioSource catSpawnSource; // Plays a sound when the cat spawns (it's kinda scary)

    [Header("Forest Growth")]
    public List<GameObject> treePrefabs; // You can add more tree types here
    public float initialSpawnDelay = 10f; // How slow trees spawn at the start
    public float minSpawnDelay = 2f;      // Fastest possible spawn
    public float difficultyIncreaseInterval = 30f; // How often it gets harder
    public float delayReduction = 0.5f;   // How much faster each time

    [Header("Spawn Boundaries")]
    public float spawnPadding = 1.0f; // Trees won't spawn too close to the edge
    private Camera mainCamera;
    private float camHeight;
    private float camWidth;

    private float currentSpawnDelay;
    private float spawnTimer;
    private float difficultyTimer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        // Pick a random number of trees before the cat shows up (makes it less predictable)
        actualCatLimit = Random.Range(minTreesBeforeCat, maxTreesBeforeCat + 1);
        Debug.Log("Heads up: The cat will appear after " + actualCatLimit + " trees!");
        currentSpawnDelay = initialSpawnDelay;
        spawnTimer = currentSpawnDelay;
        difficultyTimer = difficultyIncreaseInterval;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // If you see this error, something's wrong with the camera setup
            Debug.LogError("ForestManager: No main camera found. Tree spawning might be broken.");
            camHeight = 5f; 
            camWidth = 9f;
        }
        else
        {
            camHeight = mainCamera.orthographicSize;
            camWidth = camHeight * mainCamera.aspect;
        }
    }

    void Update()
    {
        // This timer controls when to spawn the next tree
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnTree();
            spawnTimer = currentSpawnDelay; 
        }

        // This timer makes the game harder every so often
        difficultyTimer -= Time.deltaTime;
        if (difficultyTimer <= 0)
        {
            IncreaseDifficulty();
            difficultyTimer = difficultyIncreaseInterval; 
        }
    }

    void SpawnTree()
    {
        // If you forgot to add tree prefabs, nothing will spawn!
        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogError("ForestManager: No tree prefabs set in the inspector.");
            return;
        }
        GameObject prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Count)];

        // Try up to 10 times to find a good spawn spot
        for (int i = 0; i < 10; i++)
        {
            float spawnX = Random.Range(-camWidth + spawnPadding, camWidth - spawnPadding);
            float spawnY = Random.Range(-camHeight + spawnPadding, camHeight - spawnPadding);
            Vector2 spawnPosition = new Vector2(spawnX, spawnY);

            Collider2D treeHit = Physics2D.OverlapCircle(spawnPosition, 1.5f, LayerMask.GetMask("Trees"));
            Collider2D noSpawnHit = Physics2D.OverlapCircle(spawnPosition, 0.1f, LayerMask.GetMask("Default"));
            bool isNoSpawnZone = (noSpawnHit != null && noSpawnHit.CompareTag("NoSpawn"));

            if (treeHit == null && !isNoSpawnZone)
            {
                GameObject newTree = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                float randomScale = Random.Range(0.8f, 1.3f); // Trees can be different sizes
                newTree.transform.localScale = new Vector3(randomScale, randomScale, 1f);
                return; 
            }
        }
    }

    void IncreaseDifficulty()
    {
        // Makes trees spawn faster, but not too fast
        if (currentSpawnDelay > minSpawnDelay)
        {
            currentSpawnDelay -= delayReduction;
            Debug.Log("Forest is getting harder! New spawn delay: " + currentSpawnDelay);
        }
    }

    public void RegisterTree()
    {
        treeCount++;
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
        // If enough trees have spawned and the cat isn't here yet, spawn it!
        if (treeCount >= actualCatLimit && !isCatSpawned)
        {
            SpawnCat();
        }
    }

    void SpawnCat()
    {
        if (isCatSpawned) return;
        if (catPrefab == null || playerTransform == null)
        {
            Debug.LogError("ForestManager: Can't spawn cat — missing catPrefab or playerTransform. Check inspector!");
            return;
        }
        isCatSpawned = true; 
        Debug.Log("The cat has spawned! Good luck :)");
        // Play a sound when the cat appears (jump scare?)
        if (catSpawnSource != null)
        {
            catSpawnSource.Play();
        }
        // Cat spawns a bit away from the player (so you have a chance!)
        Instantiate(catPrefab, playerTransform.position + new Vector3(10, 8, 0), Quaternion.identity);
    }
}
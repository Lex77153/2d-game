using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    public GameObject bombPrefab;        
    public float spawnInterval = 3f;     
    public float minX = -16f;            
    public float maxX = 16f;              
    public float spawnY = -1f;            

    void Start()
    {
        InvokeRepeating(nameof(SpawnBomb), 1f, spawnInterval);
    }

    void SpawnBomb()
    {
        if (bombPrefab == null) return;
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0f);
        Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
    }
    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnBomb));
    }
}
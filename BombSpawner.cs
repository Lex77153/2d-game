using UnityEngine;

/// <summary>
/// Сыплет бомбы сверху. Интервал, зона и высота спавна задаются в LevelData (LevelRunner заполняет поля),
/// в самом скрипте чисел баланса нет.
/// </summary>
public class BombSpawner : MonoBehaviour
{
    [Header("Префаб (если пусто — берётся из ItemStats)")]
    public GameObject bombPrefab;

    // заполняет LevelRunner из LevelData
    [HideInInspector] public float startDelay;
    [HideInInspector] public float spawnInterval;
    [HideInInspector] public float minX;
    [HideInInspector] public float maxX;
    [HideInInspector] public float spawnY;

    const float SafeMinInterval = 0.05f;   // защита от нуля в данных: InvokeRepeating не любит интервал 0

    void Start()
    {
        if (bombPrefab == null) bombPrefab = GameData.Items.bombPrefab;
        InvokeRepeating(nameof(SpawnBomb), startDelay, Mathf.Max(SafeMinInterval, spawnInterval));
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

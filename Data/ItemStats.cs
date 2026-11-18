using UnityEngine;

/// <summary>Значения предметов: монета, сердце, бомба. Здесь же префабы, которые создаёт LevelData.</summary>
[CreateAssetMenu(fileName = "ItemStats", menuName = "Samurai/Item Stats")]
public class ItemStats : ScriptableObject
{
    [Header("Префабы")]
    public GameObject coinPrefab;
    public GameObject heartPrefab;
    public GameObject bombPrefab;

    [Header("Монета")]
    [Tooltip("Сколько очков даёт одна монета")]
    public int coinValue = 1;

    [Header("Сердце")]
    public int heartHeal = 1;
    [Tooltip("Сколько секунд лежит сердце, выпавшее из врага")]
    public float heartLifetime = 5f;
    [Tooltip("Мигать начинает за (lifetime - это значение) секунд до исчезновения")]
    public float heartBlinkStartTime = 3f;
    public float heartBlinkInterval = 0.2f;

    [Header("Бомба")]
    public float bombLifetime = 5f;
    public float bombBlinkStartTime = 3f;
    public float bombBlinkInterval = 0.15f;
    public float bombRadius = 1.5f;
    public int bombDamage = 1;
}

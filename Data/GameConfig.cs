using UnityEngine;

/// <summary>Общие настройки игры: звук, частота кадров, имя файла сохранения.</summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Samurai/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Производительность")]
    public int targetFrameRate = 60;

    [Header("Звук")]
    [Tooltip("Громкость музыки при ползунке 100 %")]
    public float musicBaseVolume = 0.28f;
    [Tooltip("Громкость музыки на паузе при ползунке 100 %")]
    public float musicDuckVolume = 0.1f;
    public float defaultMusicVolume = 0.8f;
    public float defaultSfxVolume = 1f;
    [Tooltip("Шаг кнопок «−» и «+» в настройках")]
    public float volumeStep = 0.1f;

    [Header("Урон по умолчанию")]
    [Tooltip("Урон для вызова PlayerController.TakeDamage() без параметра")]
    public int defaultHitDamage = 1;

    [Header("Интерфейс")]
    public float levelIntroSeconds = 2.5f;

    [Header("Сохранение")]
    public string saveFileName = "save.json";
}

using UnityEngine;

/// <summary>Баланс босса-демона. Скрипт Boss читает все числа боя отсюда.</summary>
[CreateAssetMenu(fileName = "BossStats", menuName = "Samurai/Boss Stats")]
public class BossStats : ScriptableObject
{
    [Header("Здоровье и размер")]
    public int maxHealth = 10;
    public float bossScale = 2f;
    [Tooltip("Фаза 2 (ярость) начинается, когда здоровье <= maxHealth * это значение")]
    public float enrageHealthFraction = 0.5f;
    public float hitStaggerTime = 0.2f;

    [Header("Старт боя")]
    [Tooltip("На сколько метров правее игрока появляется босс")]
    public float spawnDistanceFromPlayer = 9f;
    [Tooltip("Отступ от краёв арены, дальше которого босс не ходит")]
    public float arenaMargin = 1.5f;
    [Tooltip("Если земля под боссом не найдена: половина ширины арены, м")]
    public float fallbackArenaHalfWidth = 12f;
    [Tooltip("Рёв в начале: босс неуязвим, секунд")]
    public float introTime = 1.8f;

    [Header("Ходьба")]
    public float walkSpeed = 1.5f;
    public float enragedWalkSpeed = 2.7f;
    public float walkTimeMin = 2f;
    public float walkTimeMax = 3.2f;
    public float enragedWalkTimeMin = 1.2f;
    public float enragedWalkTimeMax = 2f;
    [Tooltip("Ближе этой дистанции босс перестаёт идти")]
    public float stopDistance = 0.9f;

    [Header("Меч (cleave)")]
    public int contactDamage = 1;
    public int cleaveDamage = 1;
    [Tooltip("Длина удара = reachPerScale * bossScale")]
    public float reachPerScale = 1.05f;
    public float bodyHeightPerScale = 1.06f;
    [Tooltip("Удар начинается, когда дистанция <= reach * это значение")]
    public float meleeTriggerFactor = 0.92f;
    public float cleaveWindupFrameTime = 0.075f;
    public float cleaveStrikeFrameTime = 0.06f;
    public float cleaveReturnFrameTime = 0.08f;
    public float cleaveCooldown = 1.4f;

    [Header("Фаза 2")]
    [Tooltip("Во сколько раз быстрее атаки в фазе 2 (множитель времени)")]
    public float enragedTimeFactor = 0.78f;
    public float recoverTime = 0.95f;
    public float enragedRecoverTime = 0.55f;
    [Tooltip("Шанс, что дальняя атака — огненный дождь (иначе веер)")]
    public float rainChance = 0.4f;
    public float enragedRainChance = 0.55f;

    [Header("Веер огненных шаров")]
    public int fanCount = 3;
    public int enragedFanCount = 5;
    public float fanSpread = 0.25f;
    public float enragedFanSpread = 0.5f;
    public float fanSpeed = 6f;
    public float enragedFanSpeed = 7f;
    public int fanDamage = 1;
    public float fanProjectileLife = 6f;
    public float fanWindupFrameTime = 0.06f;
    public float fanReturnFrameTime = 0.06f;

    [Header("Огненный дождь")]
    public int rainColumns = 3;
    public int enragedRainColumns = 5;
    public int rainDamage = 1;
    public float rainStartDelay = 0.4f;
    public float rainWarningTime = 1f;
    public float rainBlastTime = 0.5f;
    public float rainPause = 2f;
    public float rainRange = 7f;
    public float rainMinSpacing = 1.6f;
    public float rainColumnWidth = 1.1f;
    public float rainHitWidth = 0.9f;
}

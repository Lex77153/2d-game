using UnityEngine;

/// <summary>Тип врага: по нему берётся нужный EnemyStats из Resources/Data/Enemies.</summary>
public enum EnemyKind
{
    Patroller = 0,   // «Патрульный» — ближний бой (скрипт Enemy)
    Shinobi = 1,     // «Синоби» — метает сюрикены (скрипт EnemyShooter)
    Charger = 2      // «Таран» — рывок (скрипт EnemyCharger)
}

/// <summary>
/// ВСЕ числа баланса одного типа врага. Скрипты Enemy / EnemyShooter / EnemyCharger
/// читают их отсюда, в самих скриптах чисел баланса нет.
/// Значения в полях ниже — это значения «по умолчанию» для нового ассета.
/// </summary>
[CreateAssetMenu(fileName = "EnemyStats", menuName = "Samurai/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Префаб (его создаёт LevelData, когда спавнит этого врага)")]
    public GameObject prefab;

    [Header("Здоровье, урон, дроп")]
    public int maxHealth = 1;
    [Tooltip("Сколько сердец отнимает удар врага")]
    public int damage = 1;
    public int coinDrop = 10;
    [Range(0f, 1f)] public float heartDropChance = 0.4f;
    public float dropForce = 3f;

    [Header("Движение")]
    public float patrolRadius = 3f;
    public float patrolSpeed = 1.3f;
    public float chaseSpeed = 2.4f;

    [Header("Зрение (Vision)")]
    [Tooltip("Максимальная дальность зрения, м")]
    public float visionRange = 5f;
    public float viewAngle = 160f;
    public float sightHeight = 2f;
    public float eyeHeight = 0.2f;
    public float closeSenseRange = 1f;
    public float loseSightTime = 1f;

    [Header("Атака")]
    public float attackRange = 1f;
    [Tooltip("Если игрок выше/ниже на столько — ближний удар не начинается")]
    public float attackReachHeight = 1f;
    [Tooltip("ЗАМАХ (Wind-up): время предупреждения до удара, с")]
    public float windUpTime = 0.5f;
    public float attackCooldown = 1.3f;
    public float strikeTime = 0.25f;
    public float recoverTime = 0.3f;
    public float firstAttackDelayMin = 0.3f;
    public float firstAttackDelayMax = 1f;
    public bool requireOnScreen = true;
    public bool contactDamage = false;
    public float contactHurtCooldown = 0.5f;

    [Header("Реакция на удар игрока")]
    public float hurtStagger = 0.25f;
    public float knockbackX = 3f;
    public float knockbackY = 3f;

    [Header("Только Синоби (стрелок)")]
    public float aimLockTime = 0.2f;
    public float projectileSpeed = 7f;
    public float projectileLife = 4f;
    public float keepDistance = 2.5f;
    public float retreatSpeed = 2.4f;
    public float retreatExtraDistance = 0.8f;
    public float retreatMaxTime = 1.2f;
    public float fireTime = 0.3f;
    public float reactionTime = 0.25f;
    public float aimLoseTime = 0.3f;
    public float aimCancelCooldown = 0.4f;

    [Header("Только Таран (рывок)")]
    public float chargeSpeed = 9f;
    public float chargeMaxTime = 1.1f;
    public float chargeBlockedCheckTime = 0.25f;
    public float stunTime = 1.6f;
    [Tooltip("Оглушение у обрыва = stunTime * этот коэффициент")]
    public float cliffStunFactor = 0.6f;
    public float chargeHeightTolerance = 1.2f;
    public float laneThickness = 0.5f;
}

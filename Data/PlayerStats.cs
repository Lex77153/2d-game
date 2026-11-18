using UnityEngine;

/// <summary>Баланс героя: здоровье, скорость, прыжок, удар мечом.</summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Samurai/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Здоровье")]
    public int maxHealth = 3;
    public float invulnerabilityDuration = 1.5f;
    public float blinkInterval = 0.15f;
    [Tooltip("Через сколько секунд после смерти уровень перезапускается")]
    public float deathRestartDelay = 3f;

    [Header("Движение")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float jumpForce = 6f;

    [Header("Атака")]
    public int attackDamage = 1;
    public float attackRange = 0.5f;
    public float attackCooldown = 1f;
}

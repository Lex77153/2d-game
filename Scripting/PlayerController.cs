using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Unity Input System

public class PlayerController : MonoBehaviour
{
    public GameObject loseObject;
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Invulnerability Settings")]
    public float invulnerabilityDuration = 1.5f;
    public float blinkInterval = 0.15f;
    private bool isInvincible = false;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float jumpForce = 8f;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float attackCooldown = 1f;
    private float nextAttackTime = 0f;
    public LayerMask enemyLayers;

    [Header("Audio Settings")]
    public AudioClip attackSound;
    private AudioSource audioSource;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;   // клавиатура (Input System, Send Messages)
    private Vector2 currentMove; // итоговое движение: экранный стик имеет приоритет над клавиатурой
    private bool isGrounded;
    private bool isDead = false;

    // --- для HUD и других скриптов ---
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public bool IsDead { get { return isDead; } }
    /// <summary>+1 — смотрит вправо, -1 — влево.</summary>
    public int Facing { get { return (spriteRenderer != null && spriteRenderer.flipX) ? -1 : 1; } }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
    }

    // --- Методы от PlayerInput (Behavior: Send Messages) ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed) TryJump();
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed) TryAttack();
    }

    // Общая логика: её вызывают и клавиатура (OnJump/OnAttack), и экранные кнопки (TouchInput)
    void TryJump()
    {
        if (isDead || !isGrounded || Time.timeScale <= 0f) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        GameAudio.Play("jump", 0.6f);
    }

    void TryAttack()
    {
        if (isDead || Time.timeScale <= 0f) return;
        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    bool IsWalkingSlowly()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
    }

    void Update()
    {
        if (isDead) return;
        if (Time.timeScale <= 0f) return; // пауза

        // Экранные кнопки и стик (мобильное управление)
        if (TouchInput.ConsumeJump()) TryJump();
        if (TouchInput.ConsumeAttack()) TryAttack();
        currentMove = Mathf.Abs(TouchInput.Move.x) > 0.01f ? TouchInput.Move : moveInput;

        // Разворот спрайта и точки атаки по направлению движения
        if (currentMove.x > 0.01f)
        {
            spriteRenderer.flipX = false;
            if (attackPoint != null) attackPoint.localPosition = new Vector3(Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, 0);
        }
        else if (currentMove.x < -0.01f)
        {
            spriteRenderer.flipX = true;
            if (attackPoint != null) attackPoint.localPosition = new Vector3(-Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, 0);
        }

        float animSpeed = Mathf.Abs(currentMove.x) * (IsWalkingSlowly() ? 0.3f : 1.0f);
        anim.SetFloat("Speed", animSpeed);

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        anim.SetBool("isJumping", !isGrounded);
        anim.SetFloat("velocityY", rb.linearVelocity.y);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Отладка (только в редакторе / dev-сборке): H — урон, K — смерть
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.hKey.wasPressedThisFrame) TakeDamage();
            if (kb.kKey.wasPressedThisFrame) Die();
        }
#endif
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float currentSpeed = IsWalkingSlowly() ? walkSpeed : runSpeed;
        rb.linearVelocity = new Vector2(currentMove.x * currentSpeed, rb.linearVelocity.y);
    }

    void Attack()
    {
        anim.SetTrigger("Attack");

        // Звук удара мечом (из инспектора, а если не назначен — из Resources/Audio)
        GameAudio.Play(attackSound, "slicing-sword-strike", 1f);

        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        HashSet<UnityEngine.Object> already = new HashSet<Object>();
        foreach (Collider2D col in hits)
        {
            Enemy enemyScript = col.GetComponentInParent<Enemy>();
            if (enemyScript != null && already.Add(enemyScript))
            {
                enemyScript.TakeDamage(1);
                continue;
            }

            Boss boss = col.GetComponentInParent<Boss>();
            if (boss != null && already.Add(boss))
            {
                boss.TakeDamage(1);
            }
        }
    }

    public void TakeDamage()
    {
        if (isDead || isInvincible) return;

        currentHealth--;
        anim.SetTrigger("Hurt");
        GameAudio.Play("hurt", 0.9f);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvincible = true;
        float timer = 0f;

        while (timer < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = (color.a == 1f) ? 0.3f : 1f;
                spriteRenderer.color = color;
            }
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        isInvincible = false;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;
        GameAudio.Play("hurt", 1f);
        anim.SetTrigger("Die");
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (SurvivalTimer.instance != null)
        {
            SurvivalTimer.instance.LoseGame();
        }
        else
        {
            if (loseObject != null)
            {
                loseObject.SetActive(true);
            }
        }

        StartCoroutine(RestartLevelRoutine());
    }

    IEnumerator RestartLevelRoutine()
    {
        // Смерть -> перезапуск ТЕКУЩЕГО уровня
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}

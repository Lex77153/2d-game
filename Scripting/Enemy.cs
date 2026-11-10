using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// БАЗА всех врагов + тип 1 «Патрульный» (Fighter).
///
/// Общее для всех врагов (живёт здесь, чтобы не копировать код):
///  * реестр живых врагов (для портала и HUD), здоровье, дроп монет/сердец, смерть;
///  * зрение: дистанция + угол обзора + Raycast на линию видимости (стены блокируют взгляд);
///  * «честная» атака: видимое и слышимое предупреждение (красная вспышка, знак «!», звук) и кулдаун;
///  * враг не начинает атаку, пока он за пределами экрана;
///  * удар игрока прерывает замах.
///
/// Тип 1 — «Патрульный»: Patrol → Chase → WindUp → Strike → Recover.
/// Тип 2 — EnemyShooter (Синоби), тип 3 — EnemyCharger (Таран): наследуют базу и переопределяют Think().
/// Все числа настройки (скорость, дальность, замах, кулдаун...) — публичные поля, видны в инспекторе.
/// </summary>
public class Enemy : MonoBehaviour
{
    public static Collider2D portalColliderToActivate;

    /// <summary>Уровни, где портал открывается только после победы над ВСЕМИ врагами.</summary>
    public static bool IsEnemyLevel(string sceneName)
    {
        return sceneName == "Level1" || sceneName == "Level3";
    }

    [Header("Здоровье и дроп")]
    public int health = 1;
    public GameObject coinPrefab;
    public GameObject heartPrefab;
    public int coinCount = 3;
    [Range(0f, 1f)]
    public float heartDropChance = 0.4f;
    public float dropForce = 8f;

    [Header("Движение")]
    public float patrolRadius = 3f;
    public float patrolSpeed = 1.3f;
    public float chaseSpeed = 2.4f;

    [Header("Зрение (Raycast)")]
    [Tooltip("Максимальная дальность зрения")]
    public float sightRange = 5f;
    [Tooltip("Полный угол обзора перед врагом, градусы")]
    [Range(20f, 360f)]
    public float viewAngle = 160f;
    [Tooltip("Если игрок выше/ниже на столько — враг его не видит")]
    public float sightHeight = 2f;
    [Tooltip("Высота глаз над центром тела (откуда идёт луч)")]
    public float eyeHeight = 0.2f;
    [Tooltip("В этом радиусе враг чувствует игрока даже за спиной (если нет стены)")]
    public float closeSenseRange = 1.0f;
    [Tooltip("Сколько секунд враг помнит игрока после потери из виду")]
    public float loseSightTime = 1.0f;
    [Tooltip("Рисовать жёлтый луч зрения (видно в Scene/Game с включёнными Gizmos)")]
    public bool showDebugRays = true;

    [Header("Атака (честная)")]
    public float attackRange = 1.0f;
    [Tooltip("ЗАМАХ: время предупреждения до удара/выстрела/рывка (по заданию 0.3–0.8 с)")]
    public float windUpTime = 0.5f;
    [Tooltip("КУЛДАУН между атаками")]
    public float attackCooldown = 1.3f;
    public float strikeTime = 0.25f;
    public float recoverTime = 0.3f;
    [Tooltip("Не начинать атаку, пока враг за экраном")]
    public bool requireOnScreen = true;
    [Tooltip("Урон при простом касании тела (по умолчанию выключен — все атаки только с замахом)")]
    public bool contactDamage = false;

    [Header("Реакция на удар игрока")]
    public float hurtStagger = 0.25f;
    public float knockbackX = 3f;
    public float knockbackY = 3f;

    // реестр живых врагов (не ломается при перезапуске сцены)
    static readonly List<Enemy> alive = new List<Enemy>();
    public static int enemiesRemaining { get { return alive.Count; } }

    // ---- общее состояние для наследников ----
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Collider2D selfCol;
    protected Transform player;
    protected PlayerController playerCtrl;
    Collider2D playerCol;

    protected Sprite[] idle, walk, attack, hurt, dead, curClip;
    protected bool useSheets;
    protected Vector2 spritePivot;
    float clipT;

    protected bool isDead;
    protected int facing = 1;          // +1 вправо, -1 влево (арт нарисован лицом ВПРАВО)
    protected float homeX;
    protected float desiredVx;
    protected float atkTimer;
    protected float stateT;
    protected float turnLock;
    protected float lostT;             // сколько секунд игрока не видно
    protected float staggerT;
    protected float halfW = 0.15f;
    protected Color baseColor = Color.white;

    // цель (обновляется каждый кадр)
    protected bool playerAlive;
    protected float dx, dy;
    protected Vector2 targetPos;

    // предупреждение
    bool telegraphing;
    float flashT;
    float nextContactHurt;
    GameObject warnMark;
    GameObject lineGo;
    SpriteRenderer lineSr;

    // состояния «Патрульного»
    enum PState { Patrol, Chase, WindUp, Strike, Recover }
    PState ps = PState.Patrol;

    // ---------------- настройка для наследников ----------------

    protected virtual string SpriteFolder { get { return "Enemy"; } }
    protected virtual string AttackSheet { get { return "Attack_1"; } }
    protected virtual int FrameW { get { return 128; } }
    protected virtual int FrameH { get { return 128; } }

    protected virtual void LoadSprites()
    {
        string f = SpriteFolder;
        idle = SpriteLib.Strip(f + "/Idle", FrameW, FrameH, spritePivot);
        walk = SpriteLib.Strip(f + "/Walk", FrameW, FrameH, spritePivot);
        attack = SpriteLib.Strip(f + "/" + AttackSheet, FrameW, FrameH, spritePivot);
        hurt = SpriteLib.Strip(f + "/Hurt", FrameW, FrameH, spritePivot);
        dead = SpriteLib.Strip(f + "/Dead", FrameW, FrameH, spritePivot);
    }

    protected virtual void OnAwake() { }
    protected virtual void OnStart() { }

    void Awake()
    {
        alive.Add(this);
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        selfCol = GetComponent<Collider2D>();

        // ноги на уровне нижней границы коллайдера -> pivot по Y считаем из размера коллайдера
        float footH = 0.405f;
        BoxCollider2D bc = selfCol as BoxCollider2D;
        if (bc != null)
        {
            footH = bc.size.y * 0.5f - bc.offset.y;
            halfW = bc.size.x * 0.5f;
        }
        spritePivot = new Vector2(0.5f, footH * 100f / FrameH);

        LoadSprites();
        useSheets = sr != null && idle != null && idle.Length > 0 && walk != null && walk.Length > 0 && attack != null && attack.Length > 0;
        if (useSheets)
        {
            Animator an = GetComponent<Animator>();
            if (an == null) an = GetComponentInChildren<Animator>();
            if (an != null) an.enabled = false;
        }
        OnAwake();
    }

    void Start()
    {
        homeX = transform.position.x;
        facing = Random.value < 0.5f ? -1 : 1;
        atkTimer = Random.Range(0.3f, 1f);
        FindPlayer();
        OnStart();
    }

    void OnDestroy()
    {
        alive.Remove(this);
        if (lineGo != null) Destroy(lineGo);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
            playerCol = p.GetComponent<Collider2D>();
        }
    }

    // ---------------- главный цикл ----------------

    void Update()
    {
        if (isDead)
        {
            Animate(dead, 8f, false);
            return;
        }
        if (player == null) FindPlayer();

        // упал в пропасть (ниже Killzone) — считаем побеждённым, иначе портал навсегда останется закрытым
        if (transform.position.y < -12f)
        {
            Die();
            return;
        }

        float dt = Time.deltaTime;
        atkTimer -= dt;
        turnLock -= dt;
        stateT += dt;
        if (flashT > 0f) flashT -= dt;

        playerAlive = player != null && playerCtrl != null && !playerCtrl.IsDead;
        if (playerAlive)
        {
            // центр тела игрока (pivot игрока стоит на ногах, поэтому берём центр коллайдера)
            targetPos = playerCol != null ? (Vector2)playerCol.bounds.center : (Vector2)player.position + new Vector2(0f, 0.4f);
            dx = targetPos.x - transform.position.x;
            dy = targetPos.y - transform.position.y;
        }
        else
        {
            dx = 0f;
            dy = 0f;
        }

        desiredVx = 0f;
        if (staggerT > 0f) staggerT -= dt;
        else Think();

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        if (isDead) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }
        if (staggerT > 0f) return;   // во время оглушения работает отбрасывание
        rb.linearVelocity = new Vector2(desiredVx, rb.linearVelocity.y);
    }

    // ---------------- ИИ типа 1: «Патрульный» ----------------

    void GoP(PState s)
    {
        ps = s;
        stateT = 0f;
    }

    protected virtual void Think()
    {
        bool sees = CanSeePlayer();
        if (sees) lostT = 0f; else lostT += Time.deltaTime;

        switch (ps)
        {
            case PState.Patrol:
                if (sees) { GoP(PState.Chase); break; }
                PatrolWalk(patrolSpeed);
                break;

            case PState.Chase:
                if (!sees && lostT > loseSightTime) { GoP(PState.Patrol); break; }
                FacePlayer();
                bool inReach = playerAlive && Mathf.Abs(dx) <= attackRange && Mathf.Abs(dy) < 1f;
                if (inReach && sees && atkTimer <= 0f && ReadyToAttack())
                {
                    // ЗАМАХ: враг замирает, мигает красным, над головой «!», слышен сигнал
                    BeginTelegraph();
                    GoP(PState.WindUp);
                    break;
                }
                if (Mathf.Abs(dx) > attackRange * 0.7f && GroundAhead() && !WallAhead())
                    desiredVx = facing * chaseSpeed;
                break;

            case PState.WindUp:
                // стоит и предупреждает; за это время игрок успевает отойти или ударить первым
                if (stateT >= windUpTime)
                {
                    EndTelegraph();
                    HitInFront();          // удар идёт В ТУ СТОРОНУ, КУДА СМОТРИТ враг
                    GoP(PState.Strike);
                }
                break;

            case PState.Strike:
                if (stateT >= strikeTime)
                {
                    atkTimer = attackCooldown;
                    GoP(PState.Recover);
                }
                break;

            case PState.Recover:
                if (stateT >= recoverTime) GoP(PState.Chase);
                break;
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (ps == PState.Strike) Animate(attack, 10f, false);
        else if (ps == PState.WindUp) ShowFrame(attack, 0);
        else if (Mathf.Abs(desiredVx) > 0.05f) Animate(walk, 10f, true);
        else Animate(idle, 6f, true);
    }

    protected virtual void OnHurt()
    {
        ps = PState.Chase;
        stateT = 0f;
        atkTimer = Mathf.Max(atkTimer, attackCooldown * 0.5f);
    }

    protected virtual bool CanHurtOnContact() { return contactDamage; }

    void HitInFront()
    {
        if (!playerAlive) return;
        Vector2 center = (Vector2)transform.position + new Vector2(facing * attackRange * 0.5f, 0f);
        Collider2D[] cs = Physics2D.OverlapBoxAll(center, new Vector2(attackRange + 0.3f, 1.0f), 0f);
        foreach (Collider2D c in cs)
        {
            if (c.CompareTag("Player"))
            {
                PlayerController pc = c.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage();
                break;
            }
        }
    }

    // ---------------- общие помощники ИИ ----------------

    protected void FacePlayer()
    {
        if (playerAlive && Mathf.Abs(dx) > 0.05f) facing = dx >= 0f ? 1 : -1;
    }

    protected void PatrolWalk(float speed)
    {
        desiredVx = facing * speed;
        float off = transform.position.x - homeX;
        bool end = (facing > 0 && off >= patrolRadius) || (facing < 0 && off <= -patrolRadius);
        if (turnLock <= 0f && (end || WallAhead() || !GroundAhead()))
        {
            facing = -facing;
            turnLock = 0.4f;
            desiredVx = 0f;
        }
    }

    protected Vector2 EyePos
    {
        get { return (Vector2)transform.position + new Vector2(0f, eyeHeight); }
    }

    /// <summary>
    /// Зрение: игрок ближе sightRange, в конусе viewAngle (или совсем рядом),
    /// и между глазом и игроком нет твёрдой стены (Raycast).
    /// </summary>
    protected bool CanSeePlayer()
    {
        if (!playerAlive) return false;
        Vector2 eye = EyePos;
        Vector2 to = targetPos - eye;
        float d = to.magnitude;
        if (d > sightRange) return false;
        if (Mathf.Abs(dy) > sightHeight) return false;
        if (d > closeSenseRange)
        {
            // угол между направлением взгляда и направлением на игрока
            if (Vector2.Angle(new Vector2(facing, 0f), to) > viewAngle * 0.5f) return false;
        }
        bool clear = HasLineOfSight(eye, targetPos);
        if (showDebugRays) Debug.DrawLine(eye, targetPos, clear ? Color.yellow : Color.gray);
        return clear;
    }

    /// <summary>Raycast от «origin» до «to»: false, если на пути есть твёрдая стена/земля.</summary>
    protected bool HasLineOfSight(Vector2 origin, Vector2 to)
    {
        Vector2 d = to - origin;
        float len = d.magnitude;
        if (len < 0.01f) return true;
        return FreeDistance(origin, d / len, len) >= len - 0.001f;
    }

    /// <summary>Сколько можно пройти лучом от «origin» по «dir», пока не упрёшься в твёрдое (не дальше max).</summary>
    protected float FreeDistance(Vector2 origin, Vector2 dir, float max)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, max);
        float best = max;
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject && IsSolid(h.collider) && h.distance < best)
                best = h.distance;
        }
        return best;
    }

    /// <summary>Враг в кадре камеры (атаковать из-за экрана нельзя).</summary>
    protected bool OnScreen()
    {
        Camera c = Camera.main;
        if (c == null) return true;
        Vector3 v = c.WorldToViewportPoint(transform.position);
        return v.z > 0f && v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f;
    }

    protected bool ReadyToAttack()
    {
        return !requireOnScreen || OnScreen();
    }

    static bool IsSolid(Collider2D c)
    {
        if (c == null || c.isTrigger) return false;
        if (c.CompareTag("Player")) return false;
        int l = c.gameObject.layer;
        string n = LayerMask.LayerToName(l);
        return n != "Enemy" && n != "Player" && n != "Coin" && n != "Heart" && n != "Bomb" && n != "Portal";
    }

    protected bool GroundAt(int dir)
    {
        float probe = Mathf.Max(0.4f, halfW + 0.1f);
        Vector2 o = (Vector2)transform.position + new Vector2(dir * probe, -0.3f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(o, Vector2.down, 0.7f);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject && IsSolid(h.collider)) return true;
        }
        return false;
    }

    protected bool WallAt(int dir)
    {
        float probe = Mathf.Max(0.45f, halfW + 0.2f);
        Vector2 o = (Vector2)transform.position + new Vector2(0f, -0.15f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(o, new Vector2(dir, 0f), probe);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject && IsSolid(h.collider)) return true;
        }
        return false;
    }

    protected bool GroundAhead() { return GroundAt(facing); }
    protected bool WallAhead() { return WallAt(facing); }

    // ---------------- предупреждение (телеграф) ----------------

    /// <summary>Замах: красное мигание + «!» над головой + звуковой сигнал.</summary>
    protected void BeginTelegraph()
    {
        telegraphing = true;
        if (warnMark == null) BuildWarnMark();
        warnMark.SetActive(true);
        GameAudio.Play("warn", 0.8f);
    }

    protected void EndTelegraph()
    {
        telegraphing = false;
        if (warnMark != null) warnMark.SetActive(false);
        HideLine();
    }

    void BuildWarnMark()
    {
        warnMark = new GameObject("WarnMark");
        warnMark.transform.SetParent(transform, false);
        float top = selfCol != null ? selfCol.bounds.extents.y : 0.4f;
        warnMark.transform.localPosition = new Vector3(0f, top + 0.35f, 0f);
        AddBar(warnMark.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.1f, 0.26f, 1f));
        AddBar(warnMark.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.1f, 0.1f, 1f));
    }

    static void AddBar(Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject g = new GameObject("Bar");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        SpriteRenderer r = g.AddComponent<SpriteRenderer>();
        r.sprite = SpriteLib.White();
        r.color = new Color(1f, 0.15f, 0.1f, 1f);
        r.sortingOrder = 30;
    }

    /// <summary>Красная полоса-предупреждение (линия прицела стрелка / дорожка рывка тарана).</summary>
    protected void ShowLine(Vector2 origin, Vector2 dir, float length, float thickness, float alpha)
    {
        if (lineGo == null)
        {
            lineGo = new GameObject("TelegraphLine");
            lineSr = lineGo.AddComponent<SpriteRenderer>();
            lineSr.sprite = SpriteLib.White();
            lineSr.sortingOrder = 25;
        }
        lineGo.SetActive(true);
        dir.Normalize();
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        lineGo.transform.position = new Vector3(origin.x + dir.x * length * 0.5f, origin.y + dir.y * length * 0.5f, 0f);
        lineGo.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        lineGo.transform.localScale = new Vector3(length, thickness, 1f);
        lineSr.color = new Color(1f, 0.15f, 0.1f, alpha);
    }

    protected void HideLine()
    {
        if (lineGo != null) lineGo.SetActive(false);
    }

    // ---------------- картинка ----------------

    void UpdateVisuals()
    {
        if (sr == null) return;

        // цвет: вспышка урона > мигание замаха > обычный
        if (flashT > 0f)
        {
            sr.color = new Color(1f, 0.45f, 0.45f, 1f);
        }
        else if (telegraphing)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 32f);
            sr.color = Color.Lerp(baseColor, new Color(1f, 0.2f, 0.15f, 1f), 0.35f + 0.5f * k);
        }
        else
        {
            sr.color = baseColor;
        }

        sr.flipX = facing < 0;
        if (!useSheets) return;

        if (staggerT > 0f && hurt != null && hurt.Length > 0) { Animate(hurt, 12f, false); return; }
        UpdateAnimation();
    }

    protected void Animate(Sprite[] clip, float fps, bool loop, int startFrame = 0)
    {
        if (clip == null || clip.Length == 0 || sr == null) return;
        if (clip != curClip) { curClip = clip; clipT = 0f; }
        clipT += Time.deltaTime;
        int i = startFrame + (int)(clipT * fps);
        i = loop ? i % clip.Length : Mathf.Min(i, clip.Length - 1);
        sr.sprite = clip[i];
    }

    /// <summary>Неподвижная поза (например, поднятый меч во время замаха).</summary>
    protected void ShowFrame(Sprite[] clip, int index)
    {
        if (clip == null || clip.Length == 0 || sr == null) return;
        curClip = null;
        sr.sprite = clip[Mathf.Clamp(index, 0, clip.Length - 1)];
    }

    // ---------------- урон / смерть ----------------

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;
        GameAudio.Play("thud", 0.7f);
        flashT = 0.12f;
        if (health <= 0)
        {
            Die();
            return;
        }

        // удар игрока прерывает замах; враг отлетает и на миг оглушён
        EndTelegraph();
        staggerT = hurtStagger;
        if (player != null && rb != null)
        {
            float dir = transform.position.x >= player.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * knockbackX, knockbackY);
        }
        OnHurt();
    }

    void Die()
    {
        isDead = true;
        EndTelegraph();
        alive.Remove(this);
        Debug.Log("Враг повержен. Осталось врагов: " + alive.Count);

        // Портал открываем, когда побеждены ВСЕ враги (Level1 и Level3)
        if (IsEnemyLevel(SceneManager.GetActiveScene().name) && alive.Count <= 0)
        {
            GameObject portal = GameObject.FindWithTag("Portal");
            if (portal != null)
            {
                Transform centerTrigger = portal.transform.Find("CenterTrigger");
                if (centerTrigger != null)
                {
                    Collider2D triggerCol = centerTrigger.GetComponent<Collider2D>();
                    if (triggerCol != null)
                    {
                        triggerCol.enabled = true;
                        GameAudio.Play("portal", 0.8f);
                        Debug.Log("Все враги побеждены! Портал активирован.");
                    }
                }
            }
        }

        // Монетки
        Vector3 spawnCenter = transform.position + new Vector3(0, 0.8f, 0);
        if (coinPrefab != null)
        {
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
                GameObject coin = Instantiate(coinPrefab, spawnCenter + (Vector3)randomOffset, Quaternion.identity);
                Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
                if (coinRb != null)
                {
                    Vector2 dropDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(1.5f, 3f)).normalized;
                    coinRb.AddForce(dropDirection * dropForce, ForceMode2D.Impulse);
                }
            }
        }

        // Сердечки (лечение)
        if (heartPrefab != null && Random.value <= heartDropChance)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
            GameObject heart = Instantiate(heartPrefab, spawnCenter + (Vector3)randomOffset, Quaternion.identity);
            Rigidbody2D heartRb = heart.GetComponent<Rigidbody2D>();
            if (heartRb != null)
            {
                Vector2 dropDirection = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(1.5f, 2.5f)).normalized;
                heartRb.AddForce(dropDirection * (dropForce * 0.8f), ForceMode2D.Impulse);
            }
        }

        // враг падает: отключаем столкновения и показываем анимацию смерти
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        if (dead == null || dead.Length == 0) { Destroy(gameObject); return; }
        Destroy(gameObject, 0.7f);
    }

    // ---------------- касание ----------------

    void TryContactHurt(GameObject other)
    {
        if (isDead || !CanHurtOnContact()) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextContactHurt) return;
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage();
            nextContactHurt = Time.time + 0.5f;
        }
    }

    void OnCollisionStay2D(Collision2D collision) { TryContactHurt(collision.gameObject); }
    void OnTriggerEnter2D(Collider2D collision) { TryContactHurt(collision.gameObject); }

    // ---------------- гизмо в редакторе (для видео и настройки) ----------------

    void OnDrawGizmosSelected()
    {
        float f = Application.isPlaying ? facing : 1f;
        Vector3 p = transform.position + new Vector3(0f, eyeHeight, 0f);
        float half = viewAngle * 0.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Vector3 fwd = new Vector3(f, 0f, 0f) * sightRange;
        Gizmos.DrawLine(p, p + Quaternion.Euler(0f, 0f, half) * fwd);
        Gizmos.DrawLine(p, p + Quaternion.Euler(0f, 0f, -half) * fwd);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(f * attackRange * 0.5f, 0f, 0f), attackRange * 0.5f);

        float hx = Application.isPlaying ? homeX : transform.position.x;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(hx - patrolRadius, transform.position.y, 0f), new Vector3(hx + patrolRadius, transform.position.y, 0f));
    }
}

using UnityEngine;

/// <summary>
/// Тип 3 — «Таран» (кабан). Мой собственный враг: не бьёт и не стреляет, а делает рывок.
/// Состояния: Patrol → WindUp (скребёт копытом, красная «дорожка» показывает путь рывка)
///            → Charge (бежит по прямой, ранит при касании)
///            → Stunned (врезался в стену: оглушён, самое время бить) или Recover (выдохся) → Patrol.
/// Честность: замах windUpTime (0.3–0.8 с), направление рывка фиксируется, после рывка кулдаун,
/// вне рывка касание безопасно. Не атакует из-за экрана и не рвётся вниз в пропасть.
/// Числа настройки: windUpTime, chargeSpeed, chargeMaxTime, stunTime, attackCooldown...
/// </summary>
public class EnemyCharger : Enemy
{
    [Header("Таран")]
    public float chargeSpeed = 9f;
    [Tooltip("Рывок длится не дольше этого времени")]
    public float chargeMaxTime = 1.1f;
    [Tooltip("Сколько он оглушён после удара о стену (у обрыва — 60% от этого)")]
    public float stunTime = 1.6f;
    [Tooltip("Рвётся в атаку, только если игрок примерно на его высоте")]
    public float chargeHeightTolerance = 1.2f;
    [Tooltip("Толщина красной дорожки-предупреждения")]
    public float laneThickness = 0.5f;

    enum S { Patrol, WindUp, Charge, Stunned, Recover }
    S st = S.Patrol;
    int chargeDir = 1;
    float curStun;          // сколько будет длиться текущее оглушение
    Sprite[] stun;

    protected override string SpriteFolder { get { return "Charger"; } }
    protected override string AttackSheet { get { return "Windup"; } }   // скребёт копытом
    protected override int FrameW { get { return 112; } }
    protected override int FrameH { get { return 72; } }

    protected override void OnAwake()
    {
        stun = SpriteLib.Strip(SpriteFolder + "/Stun", FrameW, FrameH, spritePivot);
    }

    void Go(S s)
    {
        st = s;
        stateT = 0f;
    }

    protected override bool CanHurtOnContact()
    {
        return st == S.Charge || contactDamage;
    }

    protected override void Think()
    {
        switch (st)
        {
            case S.Patrol:
                {
                    bool sees = CanSeePlayer();
                    if (sees && atkTimer <= 0f && ReadyToAttack() && Mathf.Abs(dy) < chargeHeightTolerance)
                    {
                        int dir = dx >= 0f ? 1 : -1;
                        if (!WallAt(dir) && GroundAt(dir))
                        {
                            facing = dir;
                            chargeDir = dir;
                            BeginTelegraph();
                            Go(S.WindUp);
                            break;
                        }
                    }
                    PatrolWalk(patrolSpeed);
                    break;
                }

            case S.WindUp:
                {
                    // стоит на месте; красная дорожка растёт и показывает, куда он побежит
                    Vector2 dirV = new Vector2(chargeDir, 0f);
                    Vector2 origin = (Vector2)transform.position + new Vector2(chargeDir * (halfW + 0.05f), -0.1f);
                    float lane = FreeDistance(origin, dirV, chargeSpeed * chargeMaxTime);
                    float k = Mathf.Clamp01(stateT / Mathf.Max(0.01f, windUpTime));
                    ShowLine(origin, dirV, lane, laneThickness, 0.12f + 0.3f * k);
                    if (stateT >= windUpTime)
                    {
                        EndTelegraph();
                        Go(S.Charge);
                    }
                    break;
                }

            case S.Charge:
                {
                    facing = chargeDir;
                    desiredVx = chargeDir * chargeSpeed;
                    HitPlayerWithBody();

                    bool wall = WallAt(chargeDir);
                    bool blocked = rb != null && stateT > 0.25f && Mathf.Abs(rb.linearVelocity.x) < 1f;
                    if (wall || blocked)
                    {
                        // врезался в стену — оглушён надолго
                        GameAudio.Play("thud", 1f);
                        atkTimer = attackCooldown;
                        curStun = stunTime;
                        Go(S.Stunned);
                    }
                    else if (!GroundAt(chargeDir))
                    {
                        // чуть не упал с обрыва — затормозил и оглушён ненадолго
                        GameAudio.Play("thud", 0.7f);
                        atkTimer = attackCooldown;
                        curStun = stunTime * 0.6f;
                        Go(S.Stunned);
                    }
                    else if (stateT >= chargeMaxTime)
                    {
                        // выдохся
                        atkTimer = attackCooldown;
                        Go(S.Recover);
                    }
                    break;
                }

            case S.Stunned:
                if (stateT >= curStun) Go(S.Recover);
                break;

            case S.Recover:
                if (stateT >= recoverTime) Go(S.Patrol);
                break;
        }
    }

    // рывок ранит, если тело таранa задело игрока
    void HitPlayerWithBody()
    {
        if (!playerAlive || selfCol == null) return;
        Bounds b = selfCol.bounds;
        Collider2D[] cs = Physics2D.OverlapBoxAll(b.center, new Vector2(b.size.x + 0.1f, b.size.y + 0.05f), 0f);
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

    protected override void UpdateAnimation()
    {
        switch (st)
        {
            case S.WindUp:
                Animate(attack, 8f, true);
                break;
            case S.Charge:
                Animate(walk, 16f, true);
                break;
            case S.Stunned:
                Animate(stun, 5f, true);
                break;
            default:
                if (Mathf.Abs(desiredVx) > 0.05f) Animate(walk, 8f, true);
                else Animate(idle, 5f, true);
                break;
        }
    }

    protected override void OnHurt()
    {
        // удар прерывает замах и рывок; если он оглушён — остаётся оглушённым
        if (st != S.Stunned) Go(S.Recover);
        atkTimer = Mathf.Max(atkTimer, attackCooldown * 0.5f);
    }
}

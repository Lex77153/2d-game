using UnityEngine;

/// <summary>
/// Тип 2 — «Синоби» (стрелок). Стоит на расстоянии и метает сюрикены.
/// Состояния: Patrol → Reload (заметил, ждёт) → Aim (прицел = замах) → Fire → Reload,
/// а если игрок слишком близко — Retreat (отступает).
/// Зрение: Raycast с учётом стен (спрячься за столб — и он тебя потеряет).
/// Честность: красная линия прицела показывает траекторию; последние aimLockTime секунд линия
/// замирает (можно успеть увернуться); после выстрела — кулдаун attackCooldown.
/// Числа настройки: windUpTime (время прицела), attackCooldown, projectileSpeed, keepDistance...
/// </summary>
public class EnemyShooter : Enemy
{
    [Header("Стрелок (Синоби)")]
    [Tooltip("За сколько секунд до выстрела линия прицела замирает (можно увернуться)")]
    public float aimLockTime = 0.2f;
    public float projectileSpeed = 7f;
    [Tooltip("Ближе этой дистанции Синоби отступает")]
    public float keepDistance = 2.5f;
    public float retreatSpeed = 2.4f;
    [Tooltip("Сколько длится бросок (анимация)")]
    public float fireTime = 0.3f;
    [Tooltip("Минимальная пауза после того, как игрока заметили, до начала прицела")]
    public float reactionTime = 0.25f;

    enum S { Patrol, Reload, Aim, Fire, Retreat }
    S st = S.Patrol;
    Vector2 aimDir = Vector2.right;
    float lostAim;

    protected override string SpriteFolder { get { return "Shinobi"; } }
    protected override string AttackSheet { get { return "Attack_2"; } }   // кадр 0 — замах, кадр 1 — бросок

    void Go(S s)
    {
        st = s;
        stateT = 0f;
    }

    Vector2 Muzzle()
    {
        return (Vector2)transform.position + new Vector2(facing * 0.4f, 0.15f);
    }

    protected override void Think()
    {
        bool sees = CanSeePlayer();
        if (sees) lostT = 0f; else lostT += Time.deltaTime;

        switch (st)
        {
            case S.Patrol:
                if (sees)
                {
                    FacePlayer();
                    atkTimer = Mathf.Max(atkTimer, reactionTime);
                    Go(S.Reload);
                    break;
                }
                PatrolWalk(patrolSpeed);
                break;

            case S.Reload:
                if (!sees)
                {
                    if (lostT > loseSightTime) Go(S.Patrol);
                    break;
                }
                FacePlayer();
                if (Mathf.Abs(dx) < keepDistance && CanRetreat())
                {
                    Go(S.Retreat);
                    break;
                }
                if (atkTimer <= 0f && ReadyToAttack())
                {
                    lostAim = 0f;
                    BeginTelegraph();
                    Go(S.Aim);
                }
                break;

            case S.Aim:
                if (sees) lostAim = 0f; else lostAim += Time.deltaTime;
                if (!playerAlive || lostAim > 0.3f)
                {
                    // игрок спрятался за стеной — выстрел отменяется
                    EndTelegraph();
                    atkTimer = 0.4f;
                    Go(S.Reload);
                    break;
                }
                bool locked = stateT >= windUpTime - aimLockTime;
                if (!locked)
                {
                    FacePlayer();
                    aimDir = (targetPos - Muzzle()).normalized;
                }
                // красная линия прицела до первой стены; в фазе «замка» — ярче и толще
                Vector2 m = Muzzle();
                float len = FreeDistance(m, aimDir, sightRange + 2f);
                float alpha = locked ? 0.9f : 0.3f + 0.25f * Mathf.Sin(Time.time * 18f);
                ShowLine(m, aimDir, len, locked ? 0.09f : 0.05f, alpha);

                if (stateT >= windUpTime)
                {
                    EndTelegraph();
                    GameAudio.Play("slicing-sword-strike", 0.45f);
                    BossFireball.SpawnShuriken(m, aimDir, projectileSpeed);
                    Go(S.Fire);
                }
                break;

            case S.Fire:
                if (stateT >= fireTime)
                {
                    atkTimer = attackCooldown;
                    Go(S.Reload);
                }
                break;

            case S.Retreat:
                if (!sees && lostT > loseSightTime) { Go(S.Patrol); break; }
                FacePlayer();
                if (Mathf.Abs(dx) >= keepDistance + 0.8f || stateT > 1.2f || !CanRetreat())
                {
                    Go(S.Reload);
                    break;
                }
                desiredVx = -facing * retreatSpeed;
                break;
        }
    }

    // есть ли за спиной пол и нет ли стены
    bool CanRetreat()
    {
        return GroundAt(-facing) && !WallAt(-facing);
    }

    protected override void UpdateAnimation()
    {
        if (st == S.Aim) ShowFrame(attack, 0);
        else if (st == S.Fire) Animate(attack, 9f, false, 1);
        else if (Mathf.Abs(desiredVx) > 0.05f) Animate(walk, 10f, true);
        else Animate(idle, 6f, true);
    }

    protected override void OnHurt()
    {
        st = S.Reload;
        stateT = 0f;
        atkTimer = Mathf.Max(atkTimer, attackCooldown * 0.5f);
    }
}

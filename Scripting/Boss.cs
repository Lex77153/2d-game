using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Босс-демон (Level 3).
///  * ВСЕГДА смотрит на игрока и идёт ЛИЦОМ к нему (арт демона нарисован лицом ВЛЕВО, поэтому flipX = смотрит вправо).
///  * Бьёт мечом (cleave) строго в сторону, куда смотрит; перед ударом показывает красную зону (телеграф).
///  * Дальняя атака 1: веер огненных шаров в сторону игрока (в фазе 2 шире).
///  * Дальняя атака 2: огненный дождь — красные колонны с «!», потом падает огонь.
///  * Окно для контратаки: после каждой атаки пауза (Recover).
///  * HP-бар сверху экрана, 10 HP, фаза 2 ниже 50 %.
/// Скрипт вешается на объект демона автоматически (GameBootstrap) или вручную.
/// </summary>
public class Boss : MonoBehaviour
{
    [Header("Статы")]
    public int maxHealth = 10;
    public float bossScale = 2f;
    public float walkSpeed = 1.5f;
    public float enragedWalkSpeed = 2.7f;

    SpriteRenderer sr;
    BoxCollider2D body;
    Transform player;
    PlayerController playerCtrl;

    Sprite[] idle, walk, cleave, hit, death;
    Sprite[] clip;
    float clipT;
    float clipFps = 8f;
    bool manual;

    int health;
    int facing = -1;               // +1 смотрит вправо, -1 влево
    bool enraged, dead, invulnerable;
    float groundY, minX, maxX;
    float hitT, flashT, cleaveCd;
    Color baseColor = Color.white;

    // телеграф удара
    GameObject telegraph;
    SpriteRenderer telegraphSr;
    bool telegraphOn;

    // HUD
    Image hpFill;
    TextMeshProUGUI hpText;

    // размеры в мировых единицах
    float Reach { get { return 1.05f * bossScale; } }          // длина удара мечом
    float BodyH { get { return 1.06f * bossScale; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Animator an = GetComponent<Animator>();
        if (an != null) an.enabled = false;

        // ноги демона на нижней границе кадра (160 px высотой, ноги на 159-й строке)
        Vector2 pv = new Vector2(0.5f, 1f / 160f);
        idle = SpriteLib.Strip("Boss/demon", 288, 160, pv, 6, 0);
        walk = SpriteLib.Strip("Boss/demon", 288, 160, pv, 12, 1);
        cleave = SpriteLib.Strip("Boss/demon", 288, 160, pv, 15, 2);
        hit = SpriteLib.Strip("Boss/demon", 288, 160, pv, 5, 3);
        death = SpriteLib.Strip("Boss/demon", 288, 160, pv, 22, 4);

        if (idle.Length == 0)
        {
            Debug.LogError("Boss: не найден Resources/Boss/demon.png");
            enabled = false;
            return;
        }

        transform.localScale = new Vector3(bossScale, bossScale, 1f);

        // слой Enemy, чтобы меч игрока (enemyLayers) его видел
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) gameObject.layer = enemyLayer;

        body = GetComponent<BoxCollider2D>();
        if (body == null) body = gameObject.AddComponent<BoxCollider2D>();
        body.isTrigger = true;
        body.size = new Vector2(0.62f, 1.0f);
        body.offset = new Vector2(0f, 0.5f);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        health = maxHealth;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCtrl = p.GetComponent<PlayerController>();
        }

        FindGround();
        if (player != null)
        {
            float x = Mathf.Clamp(player.position.x + 9f, minX, maxX);
            transform.position = new Vector3(x, groundY, 0f);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, groundY, 0f);
        }

        CreateTelegraph();
        CreateHUD();
        UpdateBar();
        StartCoroutine(Brain());
    }

    void FindGround()
    {
        groundY = transform.position.y;
        minX = -12f; maxX = 12f;
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + 6f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 40f);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject == gameObject || h.collider.CompareTag("Player")) continue;
            groundY = h.point.y;
            Bounds b = h.collider.bounds;
            minX = b.min.x + 1.5f;
            maxX = b.max.x - 1.5f;
            break;
        }
    }

    // ---------------- анимация ----------------

    void SetLoop(Sprite[] c, float fps)
    {
        manual = false;
        if (c != clip) { clip = c; clipT = 0f; }
        clipFps = fps;
    }

    void ShowFrame(Sprite[] arr, int i)
    {
        manual = true;
        if (arr == null || arr.Length == 0) return;
        sr.sprite = arr[Mathf.Clamp(i, 0, arr.Length - 1)];
    }

    void Update()
    {
        if (dead) return;

        if (!manual && clip != null && clip.Length > 0)
        {
            clipT += Time.deltaTime;
            sr.sprite = clip[(int)(clipT * clipFps) % clip.Length];
        }

        // ГЛАВНОЕ: арт демона смотрит ВЛЕВО -> когда он смотрит вправо, зеркалим.
        sr.flipX = facing > 0;

        hitT -= Time.deltaTime;
        cleaveCd -= Time.deltaTime;

        // вспышка при попадании / красный оттенок в фазе 2
        flashT -= Time.deltaTime;
        if (flashT > 0f) sr.color = Color.red;
        else sr.color = enraged ? new Color(1f, 0.62f, 0.55f, 1f) : baseColor;

        // телеграф мигает
        if (telegraphOn && telegraphSr != null)
        {
            float a = 0.18f + 0.22f * Mathf.Abs(Mathf.Sin(Time.time * 12f));
            telegraphSr.color = new Color(1f, 0.15f, 0.1f, a);
        }

        // урон при касании тела
        if (!invulnerable && playerCtrl != null && !playerCtrl.IsDead)
        {
            Vector2 contactSize = new Vector2(body.bounds.size.x * 0.7f, body.bounds.size.y * 0.9f);
            Collider2D[] cs = Physics2D.OverlapBoxAll(body.bounds.center, contactSize, 0f);
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
    }

    // ---------------- помощники ----------------

    void FacePlayer()
    {
        if (player == null) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) facing = dx >= 0f ? 1 : -1;
    }

    float DistToPlayer()
    {
        return player == null ? 99f : Mathf.Abs(player.position.x - transform.position.x);
    }

    void Move(float dir, float speed)
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + dir * speed * Time.deltaTime, minX, maxX);
        p.y = groundY;
        transform.position = p;
    }

    float K { get { return enraged ? 0.78f : 1f; } }   // в фазе 2 атаки быстрее

    // ---------------- мозг (машина состояний) ----------------

    IEnumerator Brain()
    {
        // Интро: рёв, босс неуязвим, игрок успевает приготовиться
        invulnerable = true;
        GameAudio.Play("roar", 0.9f);
        SetLoop(idle, 10f);
        float t = 0f;
        while (t < 1.8f) { FacePlayer(); t += Time.deltaTime; yield return null; }
        invulnerable = false;

        while (!dead)
        {
            yield return WalkPhase();
        }
    }

    IEnumerator WalkPhase()
    {
        float walkTime = enraged ? Random.Range(1.2f, 2.0f) : Random.Range(2.0f, 3.2f);
        float t = 0f;
        while (t < walkTime && !dead)
        {
            t += Time.deltaTime;
            FacePlayer();                                  // смотрит на игрока
            float dist = DistToPlayer();

            if (dist <= Reach * 0.92f && cleaveCd <= 0f)
            {
                yield return Cleave();
                yield return Recover();
                yield break;
            }

            if (hitT > 0f)
            {
                SetLoop(hit, 18f);
            }
            else if (dist > 0.9f)
            {
                // идёт ЛИЦОМ к игроку (facing уже в его сторону)
                SetLoop(walk, enraged ? 16f : 11f);
                Move(facing, enraged ? enragedWalkSpeed : walkSpeed);
            }
            else
            {
                SetLoop(idle, 8f);
            }
            yield return null;
        }
        if (dead) yield break;

        // дальняя атака
        bool rain = Random.value < (enraged ? 0.55f : 0.4f);
        if (rain) yield return Rain(); else yield return Fan();
        yield return Recover();
    }

    IEnumerator Recover()
    {
        // окно для контратаки
        SetLoop(idle, 8f);
        float t = 0f;
        float dur = enraged ? 0.55f : 0.95f;
        while (t < dur && !dead) { FacePlayer(); t += Time.deltaTime; yield return null; }
    }

    // ----- удар мечом: ТОЛЬКО в сторону, куда смотрит демон -----
    IEnumerator Cleave()
    {
        FacePlayer();
        int dir = facing;                 // фиксируем направление на время удара
        telegraphOn = true;

        for (int i = 0; i <= 8; i++)       // замах
        {
            ShowFrame(cleave, i);
            PlaceTelegraph(dir);
            yield return new WaitForSeconds(0.075f * K);
        }
        telegraphOn = false;
        if (telegraph != null) telegraph.SetActive(false);

        for (int i = 9; i <= 10; i++)      // сам удар
        {
            ShowFrame(cleave, i);
            if (i == 9)
            {
                GameAudio.Play("thud", 0.9f);
                DoCleaveHit(dir);
            }
            yield return new WaitForSeconds(0.06f);
        }
        for (int i = 11; i <= 14; i++)     // возврат
        {
            ShowFrame(cleave, i);
            yield return new WaitForSeconds(0.08f * K);
        }
        cleaveCd = 1.4f * K;
    }

    void DoCleaveHit(int dir)
    {
        Vector2 center = new Vector2(transform.position.x + dir * Reach * 0.5f, groundY + BodyH * 0.5f);
        Vector2 size = new Vector2(Reach, BodyH);
        Collider2D[] cs = Physics2D.OverlapBoxAll(center, size, 0f);
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

    // ----- веер огненных шаров в сторону игрока -----
    IEnumerator Fan()
    {
        FacePlayer();
        for (int i = 0; i <= 9; i++)
        {
            ShowFrame(cleave, i);
            if (i == 9) SpawnFan();
            yield return new WaitForSeconds(0.075f * K * 0.8f);
        }
        for (int i = 10; i <= 14; i++)
        {
            ShowFrame(cleave, i);
            yield return new WaitForSeconds(0.06f);
        }
    }

    void SpawnFan()
    {
        if (player == null) return;
        GameAudio.Play("fire", 0.7f);
        Vector2 mouth = new Vector2(transform.position.x + facing * 0.5f * bossScale, groundY + 0.75f * bossScale);
        Vector2 target = (Vector2)player.position + Vector2.up * 0.3f;
        float baseAngle = Mathf.Atan2(target.y - mouth.y, target.x - mouth.x);
        int n = enraged ? 5 : 3;                          // в фазе 2 веер шире
        float spread = enraged ? 0.5f : 0.25f;
        float speed = enraged ? 7f : 6f;
        for (int i = 0; i < n; i++)
        {
            float a = baseAngle + (n == 1 ? 0f : ((float)i / (n - 1) - 0.5f) * 2f * spread);
            BossFireball.Spawn(mouth, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed);
        }
    }

    // ----- огненный дождь: колонна с «!», потом падает огонь -----
    IEnumerator Rain()
    {
        FacePlayer();
        GameAudio.Play("roar", 0.7f);
        SetLoop(idle, 12f);
        yield return new WaitForSeconds(0.4f);

        int n = enraged ? 5 : 3;
        List<float> xs = new List<float>();
        float px = player != null ? player.position.x : transform.position.x;
        xs.Add(Mathf.Clamp(px, minX, maxX));
        for (int i = 1; i < n; i++)
        {
            float x = 0f;
            for (int tries = 0; tries < 20; tries++)
            {
                x = Mathf.Clamp(px + Random.Range(-7f, 7f), minX, maxX);
                bool ok = true;
                foreach (float q in xs) if (Mathf.Abs(q - x) < 1.6f) ok = false;
                if (ok) break;
            }
            xs.Add(x);
        }
        GameAudio.Play("warn", 0.6f);
        foreach (float x in xs) StartCoroutine(RainColumn(x));
        yield return new WaitForSeconds(2.0f);
    }

    IEnumerator RainColumn(float x)
    {
        // предупреждение: красная колонна + «!»
        GameObject col = new GameObject("RainWarn");
        col.transform.position = new Vector3(x, groundY + 6f, 0f);
        col.transform.localScale = new Vector3(1.1f, 12f, 1f);
        SpriteRenderer csr = col.AddComponent<SpriteRenderer>();
        csr.sprite = SpriteLib.White();
        csr.sortingOrder = 5;

        GameObject mark = new GameObject("RainMark");
        mark.transform.position = new Vector3(x, groundY + 3.2f, 0f);
        TextMeshPro tm = mark.AddComponent<TextMeshPro>();
        tm.text = "!";
        tm.fontSize = 14;
        tm.fontStyle = FontStyles.Bold;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = new Color(1f, 0.92f, 0.3f, 1f);
        tm.sortingOrder = 30;

        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            float a = 0.18f + 0.22f * Mathf.Abs(Mathf.Sin(t * 10f));
            csr.color = new Color(1f, 0.15f, 0.1f, a);
            mark.transform.position = new Vector3(x, groundY + 3.2f + Mathf.Sin(t * 14f) * 0.12f, 0f);
            yield return null;
        }
        Destroy(mark);

        // падает огонь
        GameAudio.Play("explosion-of-a-failed-experiment", 0.45f);
        csr.color = new Color(1f, 0.55f, 0.1f, 0.9f);
        col.transform.localScale = new Vector3(0.9f, 12f, 1f);

        GameObject boom = new GameObject("RainBoom");
        boom.transform.position = new Vector3(x, groundY + 0.6f, 0f);
        boom.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        SpriteRenderer bsr = boom.AddComponent<SpriteRenderer>();
        bsr.sprite = SpriteLib.Whole("FX/explosion");
        bsr.sortingOrder = 6;

        bool damaged = false;
        float f = 0f;
        while (f < 0.5f)
        {
            f += Time.deltaTime;
            float a = 1f - f / 0.5f * 0.6f;
            csr.color = new Color(1f, 0.55f, 0.1f, a);
            if (!damaged && playerCtrl != null && !playerCtrl.IsDead)
            {
                Collider2D[] cs = Physics2D.OverlapBoxAll(new Vector2(x, groundY + 5f), new Vector2(0.9f, 10f), 0f);
                foreach (Collider2D c in cs)
                {
                    if (c.CompareTag("Player"))
                    {
                        PlayerController pc = c.GetComponent<PlayerController>();
                        if (pc != null) pc.TakeDamage();
                        damaged = true;
                        break;
                    }
                }
            }
            yield return null;
        }
        Destroy(col);
        Destroy(boom);
    }

    // ---------------- телеграф удара ----------------

    void CreateTelegraph()
    {
        telegraph = new GameObject("BossTelegraph");
        telegraphSr = telegraph.AddComponent<SpriteRenderer>();
        telegraphSr.sprite = SpriteLib.White();
        telegraphSr.sortingOrder = sr.sortingOrder - 1;
        telegraph.SetActive(false);
    }

    void PlaceTelegraph(int dir)
    {
        if (telegraph == null) return;
        telegraph.SetActive(true);
        telegraph.transform.position = new Vector3(transform.position.x + dir * Reach * 0.5f, groundY + BodyH * 0.5f, 0f);
        telegraph.transform.localScale = new Vector3(Reach, BodyH, 1f);
    }

    // ---------------- урон по боссу ----------------

    public void TakeDamage(int damage)
    {
        if (dead || invulnerable) return;
        health -= damage;
        flashT = 0.12f;
        GameAudio.Play("thud", 0.9f);
        UpdateBar();

        if (health <= 0)
        {
            dead = true;
            StopAllCoroutines();
            StartCoroutine(DieRoutine());
            return;
        }

        if (!enraged && health <= maxHealth / 2)
        {
            enraged = true;                      // фаза 2
            GameAudio.Play("roar", 1f);
            UpdateBar();
        }
        hitT = 0.2f;
    }

    IEnumerator DieRoutine()
    {
        telegraphOn = false;
        if (telegraph != null) telegraph.SetActive(false);
        body.enabled = false;
        foreach (BossFireball f in FindObjectsByType<BossFireball>(FindObjectsSortMode.None)) Destroy(f.gameObject);
        GameAudio.Play("roar", 1f);
        sr.color = Color.white;

        manual = true;
        for (int i = 0; i < death.Length; i++)
        {
            sr.sprite = death[i];
            sr.flipX = facing > 0;
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1.0f);

        GameAudio.StopMusic();
        GameAudio.Play("win", 1f);
        WinScreen.Show();
    }

    // ---------------- HP-бар ----------------

    void CreateHUD()
    {
        // Полоска здоровья босса живёт внутри мобильного HUD (SafeArea), на месте подписи уровня.
        // Если HUD по какой-то причине нет — запасной вариант со своим Canvas.
        Transform parent;
        Vector2 barPos = Vector2.zero;
        HudController hud = HudController.Current;
        if (hud != null && hud.infoSlot != null)
        {
            hud.ShowLevelLabel(false);
            parent = hud.infoSlot;
        }
        else
        {
            GameObject cgo = new GameObject("BossHUD");
            Canvas canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            CanvasScaler scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            parent = cgo.transform;
            barPos = new Vector2(0f, -160f);
        }

        GameObject bg = new GameObject("BarBG", typeof(RectTransform));
        bg.transform.SetParent(parent, false);
        Image bgi = bg.AddComponent<Image>();
        bgi.sprite = SpriteLib.White();
        bgi.color = new Color(0.08f, 0.04f, 0.13f, 0.92f);
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 1f);
        brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = barPos;
        brt.sizeDelta = new Vector2(900f, 68f);

        GameObject fill = new GameObject("BarFill", typeof(RectTransform));
        fill.transform.SetParent(bg.transform, false);
        hpFill = fill.AddComponent<Image>();
        hpFill.sprite = SpriteLib.White();
        hpFill.color = new Color(0.85f, 0.12f, 0.15f, 1f);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        hpFill.fillAmount = 1f;
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(6f, 6f);
        frt.offsetMax = new Vector2(-6f, -6f);

        GameObject txt = new GameObject("BarText", typeof(RectTransform));
        txt.transform.SetParent(bg.transform, false);
        hpText = txt.AddComponent<TextMeshProUGUI>();
        UIKit.StyleText(hpText, "", 40f, Color.white, TextAlignmentOptions.Center);
        RectTransform trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void UpdateBar()
    {
        if (hpFill != null) hpFill.fillAmount = Mathf.Clamp01((float)health / maxHealth);
        if (hpText != null) hpText.text = "DEMON  " + Mathf.Max(0, health) + " / " + maxHealth + (enraged ? "  (ENRAGED)" : "");
    }

    void OnDestroy()
    {
        if (telegraph != null) Destroy(telegraph);
    }
}

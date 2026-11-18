using UnityEngine;
using TMPro;

/// <summary>
/// Строит уровень из LevelData. ОДИН и тот же код работает для любого уровня: он берёт data-ассет
/// для текущей сцены (GameData.LevelForScene) и накладывает его на сцену:
///   * враги (EnemySpawn) — вручную расставленные в сцене заменяются врагами из списка;
///   * дополнительные платформы, монеты, сердца (PlatformSpawn, PickupSpawn);
///   * декор и подсказки (PropSpawn, SignSpawn), фоновые частицы (AmbientKind);
///   * бомбы, скорость движущихся платформ, старт героя, надпись в начале;
/// и считает время прохождения. Когда уровень пройден, вызывается CompleteCurrent():
/// бонус за скорость, рекорд, открытие следующего уровня и запись save.json.
/// Запускается из GameBootstrap после загрузки каждой игровой сцены.
/// </summary>
public class LevelRunner : MonoBehaviour
{
    public static LevelRunner Current { get; private set; }

    public LevelData Data { get; private set; }
    /// <summary>Сколько секунд игрового времени прошло с начала уровня.</summary>
    public float Elapsed { get; private set; }

    const float HintFontSize = 3.2f;

    int startCoins;
    bool completed;
    Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = null; }

    public static void Begin(string sceneName)
    {
        LevelData data = GameData.LevelForScene(sceneName);
        if (data == null) return;
        GameObject go = new GameObject("LevelRunner");
        LevelRunner runner = go.AddComponent<LevelRunner>();
        runner.Setup(data, sceneName);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Update()
    {
        if (!completed) Elapsed += Time.deltaTime;   // на паузе deltaTime = 0, время стоит
    }

    // ------------------------------------------------------------ конец уровня

    /// <summary>Уровень пройден: бонус за время, рекорд, открытие следующего уровня, запись на диск.</summary>
    public static void CompleteCurrent()
    {
        LevelRunner r = Current;
        if (r == null || r.Data == null || r.completed) return;
        r.completed = true;

        LevelData d = r.Data;
        int bonus = 0;
        if (d.parTimeSeconds > 0f && d.timeBonusPerSecond > 0)
        {
            float secondsUnderPar = d.parTimeSeconds - r.Elapsed;
            bonus = Mathf.Clamp(Mathf.FloorToInt(secondsUnderPar * d.timeBonusPerSecond), 0, d.maxTimeBonus);
        }

        CoinManager cm = CoinManager.instance;
        if (cm != null && bonus > 0) cm.AddCoin(bonus);
        int total = cm != null ? cm.coinCount : r.startCoins;
        int levelScore = Mathf.Max(0, total - r.startCoins);

        SaveSystem.CompleteLevel(d.levelNumber, levelScore, total, r.Elapsed);
        Debug.Log("LevelRunner: уровень " + d.levelNumber + " пройден за " + r.Elapsed.ToString("F1") + " c, очки " + levelScore + " (бонус за время " + bonus + "). Прогресс сохранён.");
    }

    // ------------------------------------------------------------ построение

    void Setup(LevelData data, string sceneName)
    {
        Current = this;
        Data = data;
        startCoins = CoinManager.banked;
        root = new GameObject("LevelFromData").transform;

        PlacePlayer();
        SpawnEnemies();
        ApplySceneNumbers();
        SpawnPlatforms(sceneName);
        SpawnPickups();
        SpawnProps();
        SpawnSigns();
        AmbientFX.Create(data.ambient, data.ambientCount);

        if (!string.IsNullOrEmpty(data.introText))
        {
            HudController hud = HudController.Current;
            if (hud != null) hud.Toast(data.introText, GameData.Config.levelIntroSeconds);
        }
    }

    void PlacePlayer()
    {
        if (!Data.overridePlayerStart) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        p.transform.position = new Vector3(Data.playerStart.x, Data.playerStart.y, p.transform.position.z);
    }

    void SpawnEnemies()
    {
        if (Data.replaceSceneEnemies)
        {
            // врагов, поставленных в сцене руками, убираем: теперь их место и тип определяет LevelData
            Enemy[] old = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < old.Length; i++) DestroyImmediate(old[i].gameObject);
        }

        for (int i = 0; i < Data.enemies.Count; i++)
        {
            EnemySpawn s = Data.enemies[i];
            EnemyStats stats = GameData.Enemy(s.kind);
            if (stats.prefab == null)
            {
                Debug.LogError("LevelRunner: у EnemyStats для " + s.kind + " не назначен prefab — враг не создан.");
                continue;
            }
            GameObject go = Instantiate(stats.prefab, new Vector3(s.x, s.y, 0f), Quaternion.identity);
            go.name = s.kind.ToString() + " " + (i + 1);
        }
    }

    void ApplySceneNumbers()
    {
        // движущиеся платформы
        if (Data.movingPlatformSpeed > 0f)
        {
            MovingPlatform[] movers = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
            for (int i = 0; i < movers.Length; i++) movers[i].speed = Data.movingPlatformSpeed;
        }

        // бомбы (уровни на выживание)
        BombSpawner spawner = FindAnyObjectByType<BombSpawner>();
        if (spawner != null)
        {
            if (Data.bombsEnabled)
            {
                spawner.startDelay = Data.bombStartDelay;
                spawner.spawnInterval = Data.bombInterval;
                spawner.minX = Data.bombMinX;
                spawner.maxX = Data.bombMaxX;
                spawner.spawnY = Data.bombSpawnY;
            }
            else
            {
                spawner.enabled = false;
            }
        }
    }

    void SpawnPlatforms(string sceneName)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        for (int i = 0; i < Data.platforms.Count; i++)
        {
            PlatformSpawn p = Data.platforms[i];
            float w = Mathf.Max(0.5f, p.width);
            float h = Mathf.Max(0.2f, p.thickness);
            Vector3 center = new Vector3(p.x, p.top - h * 0.5f, 0f);

            GameObject go = new GameObject("Platform " + (i + 1));
            go.transform.SetParent(root, false);
            go.transform.position = center;
            if (groundLayer >= 0) go.layer = groundLayer;   // на слое Ground герой может от них прыгать
            go.AddComponent<LevelDataPlatform>();            // метка: босс не принимает уступ за землю арены

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);

            // картинка: земля с травой из TerrainPainter (если текстуры нет — однотонный прямоугольник)
            GameObject art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
            Sprite sprite = TerrainPainter.Build(w, h);
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            else
            {
                sr.sprite = SpriteLib.White();
                sr.color = new Color(0.30f, 0.20f, 0.15f, 1f);
                art.transform.localScale = new Vector3(w, h, 1f);
            }

            WorldStyler.DressPlatform(sceneName, center, w, h, root);
        }
    }

    void SpawnPickups()
    {
        ItemStats items = GameData.Items;
        for (int i = 0; i < Data.pickups.Count; i++)
        {
            PickupSpawn p = Data.pickups[i];
            GameObject prefab = p.kind == PickupKind.Heart ? items.heartPrefab : items.coinPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("LevelRunner: в ItemStats нет префаба для " + p.kind);
                continue;
            }

            GameObject go = Instantiate(prefab, new Vector3(p.x, p.y, 0f), Quaternion.identity, root);

            // предмет висит в воздухе: без гравитации и без твёрдого коллайдера (остаётся только триггер подбора)
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }
            Collider2D[] cols = go.GetComponents<Collider2D>();
            for (int k = 0; k < cols.Length; k++) if (!cols[k].isTrigger) cols[k].enabled = false;

            if (p.kind == PickupKind.Heart)
            {
                Heart heart = go.GetComponent<Heart>();
                if (heart != null) heart.permanent = true;   // лежит, пока не подберут
            }
        }
    }

    void SpawnProps()
    {
        for (int i = 0; i < Data.props.Count; i++)
        {
            PropSpawn p = Data.props[i];
            if (string.IsNullOrEmpty(p.prop)) continue;
            WorldStyler.PlaceProp(root, p.prop, p.x, p.y, p.order);
        }
    }

    // подсказка в мире: текст на тёмной плашке
    void SpawnSigns()
    {
        for (int i = 0; i < Data.signs.Count; i++)
        {
            SignSpawn s = Data.signs[i];
            if (string.IsNullOrEmpty(s.text)) continue;

            GameObject go = new GameObject("Hint " + (i + 1));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(s.x, s.y, 0f);

            TextMeshPro tm = go.AddComponent<TextMeshPro>();
            tm.text = s.text;
            tm.fontSize = HintFontSize;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = UIKit.Cream;
            tm.sortingOrder = 11;
            tm.rectTransform.sizeDelta = new Vector2(40f, 3f);   // широкая рамка — текст не переносится

            Vector2 size = tm.GetPreferredValues(s.text);
            if (size.x < 0.5f) size = new Vector2(s.text.Length * 0.28f, 0.4f);

            GameObject plate = new GameObject("Plate");
            plate.transform.SetParent(go.transform, false);
            plate.transform.localScale = new Vector3(size.x + 0.7f, size.y + 0.35f, 1f);
            SpriteRenderer psr = plate.AddComponent<SpriteRenderer>();
            psr.sprite = SpriteLib.White();
            psr.color = new Color(0.08f, 0.04f, 0.14f, 0.72f);
            psr.sortingOrder = 10;
        }
    }
}

using UnityEngine;

/// <summary>
/// Оформление мира во время запуска (без ручной расстановки в редакторе):
///  * закатное небо, солнце, горы и силуэты деревьев с параллаксом;
///  * платформы (белые квадраты) одеваются в землю с травой из набора Village Props;
///  * на платформах расставляется декор: трава, кусты, камни, ящики, заборы, деревья.
/// Декор не имеет коллайдеров и лежит за героем, поэтому на геймплей не влияет.
/// </summary>
public static class WorldStyler
{
    static readonly Color PropTint = new Color(0.95f, 0.89f, 0.96f, 1f);
    static readonly string[] Tufts = { "tuft_1", "tuft_2", "tuft_3", "tuft_4", "tuft_5", "tuft_6" };
    static readonly string[] Bushes = { "bush_a", "bush_b", "bush_c" };
    static readonly string[] Rocks = { "rock_a", "rock_b" };
    static readonly string[] Crates = { "crate", "crate_small", "barrel", "hay_bale", "pumpkins" };

    // ============================================================ МЕНЮ

    public static void StyleMenu()
    {
        Camera cam = Camera.main;
        SetupCamera(cam);
        Transform root = new GameObject("MenuWorld").transform;

        BuildBackdrop(cam, root, new Vector3(0f, -0.4f, 0f), 6.5f);

        // земля: полоса из тайлсета (если текстура недоступна — тёмная плита)
        float groundTop = -3.1f;
        Sprite ground = TerrainPainter.Build(46f, 3.4f);
        if (ground != null) Quad(root, "Ground", ground, new Vector3(0f, groundTop - 1.7f, 0f), Vector3.one, Color.white, -10);
        else Quad(root, "Ground", SpriteLib.White(), new Vector3(0f, -4.4f, 0f), new Vector3(46f, 2.6f, 1f), new Color(0.12f, 0.07f, 0.17f, 1f), -10);

        // деревенский декор по краям (в центре — кнопки меню)
        Prop(root, "tree_big", -9.2f, groundTop, -9);
        Prop(root, "bush_a", -6.9f, groundTop, -6);
        Prop(root, "well", -7.8f, groundTop, -7);
        Prop(root, "fence", -1.4f, groundTop, -8);
        Prop(root, "lamp_pole", 1.3f, groundTop, -8);
        Prop(root, "lantern", 1.3f, groundTop + 2.6f, -7);
        Prop(root, "hay_pile", 6.7f, groundTop, -6);
        Prop(root, "tree_small", 9.0f, groundTop, -9);
        Prop(root, "scarecrow", 10.4f, groundTop, -8);
        Prop(root, "bush_b", 8.0f, groundTop, -6);
        Prop(root, "pumpkins", -3.0f, groundTop, -5);
        Prop(root, "banner", -11.0f, groundTop + 0.1f, -8);
        System.Random rng = new System.Random(7);
        for (int i = 0; i < 26; i++)
        {
            float x = -12f + (float)rng.NextDouble() * 24f;
            Prop(root, Tufts[rng.Next(Tufts.Length)], x, groundTop - 0.03f, -5);
        }

        // герой слева и демон справа (смотрит на героя)
        GameObject hero = Quad(root, "MenuHero", null, new Vector3(-4.2f, groundTop, 0f), new Vector3(3f, 3f, 1f), Color.white, 0);
        SpriteAnimator ha = hero.AddComponent<SpriteAnimator>();
        ha.frames = SpriteLib.Strip("Hero/Idle", 128, 128, new Vector2(0.5f, 1f / 128f));
        ha.fps = 7f;

        GameObject demon = Quad(root, "MenuDemon", null, new Vector3(4.4f, groundTop, 0f), new Vector3(2.8f, 2.8f, 1f), Color.white, 0);
        SpriteAnimator da = demon.AddComponent<SpriteAnimator>();
        da.frames = SpriteLib.Strip("Boss/demon", 288, 160, new Vector2(0.5f, 1f / 160f), 6, 0);
        da.fps = 7f;
    }

    // ============================================================ УРОВНИ

    public static void StyleLevel(string scene)
    {
        Camera cam = Camera.main;
        SetupCamera(cam);
        Transform root = new GameObject("LevelBackdrop").transform;
        BuildBackdrop(cam, root, new Vector3(7f, -0.5f, 0f), 5.5f);
        PaintPlatforms(scene);
    }

    static void SetupCamera(Camera cam)
    {
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.09f, 0.28f, 1f);
    }

    // ------------------------------------------------------------ небо, солнце, горы, деревья

    static void BuildBackdrop(Camera cam, Transform parent, Vector3 sunPos, float sunSize)
    {
        // небо и солнце привязаны к камере и закрывают экран при любом соотношении сторон
        GameObject sky = new GameObject("Sky");
        sky.transform.SetParent(parent, false);
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        sky.transform.position = new Vector3(c.x, c.y, 0f);
        sky.AddComponent<CameraAnchor>();

        Color[] bands =
        {
            new Color(1.00f, 0.70f, 0.40f, 1f),
            new Color(0.97f, 0.55f, 0.40f, 1f),
            new Color(0.84f, 0.40f, 0.43f, 1f),
            new Color(0.66f, 0.28f, 0.47f, 1f),
            new Color(0.46f, 0.20f, 0.46f, 1f),
            new Color(0.32f, 0.15f, 0.40f, 1f),
            new Color(0.22f, 0.11f, 0.34f, 1f),
        };
        Sprite white = SpriteLib.White();
        for (int i = 0; i < bands.Length; i++)
            Quad(sky.transform, "Band" + i, white, new Vector3(0f, -6f + i * 2f, 0f), new Vector3(120f, 2.1f, 1f), bands[i], -30);
        Quad(sky.transform, "Sun", SpriteLib.Circle(), sunPos, new Vector3(sunSize, sunSize, 1f), new Color(1f, 0.93f, 0.62f, 1f), -29);

        // горы и деревья — слои параллакса
        AddSpriteLayer(cam, parent, "MountainsFar", MountainSprite(11, 24f, 8f, 0.62f, 0.34f), 0.93f, 24f, new Color(0.36f, 0.19f, 0.48f, 1f), -26, -5.2f);
        AddSpriteLayer(cam, parent, "MountainsNear", MountainSprite(29, 24f, 6f, 0.58f, 0.40f), 0.84f, 24f, new Color(0.25f, 0.14f, 0.37f, 1f), -24, -5.2f);
        AddTreeLayer(cam, parent, 0.68f, new Color(0.19f, 0.10f, 0.29f, 1f), -22, -5.0f);
    }

    static void AddSpriteLayer(Camera cam, Transform parent, string name, Sprite sprite, float factor, float wrap, Color color, int order, float bottom)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        g.transform.position = new Vector3(c.x, c.y + bottom, 0f);
        ParallaxLayer pl = g.AddComponent<ParallaxLayer>();
        pl.factor = factor;
        pl.wrapWidth = wrap;
        for (int i = -2; i <= 2; i++)
        {
            GameObject t = new GameObject("Tile" + i);
            t.transform.SetParent(g.transform, false);
            t.transform.localPosition = new Vector3(i * wrap, 0f, 0f);
            SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
        }
    }

    static void AddTreeLayer(Camera cam, Transform parent, float factor, Color color, int order, float bottom)
    {
        float wrap = 20f;
        GameObject g = new GameObject("TreeSilhouettes");
        g.transform.SetParent(parent, false);
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        g.transform.position = new Vector3(c.x, c.y + bottom, 0f);
        ParallaxLayer pl = g.AddComponent<ParallaxLayer>();
        pl.factor = factor;
        pl.wrapWidth = wrap;

        string[] names = { "tree_big", "tree_small", "bush_a", "tree_small", "tree_big", "bush_c" };
        float[] xs = { -8.5f, -4.0f, -1.0f, 3.0f, 6.5f, 9.0f };
        for (int i = -2; i <= 2; i++)
        {
            for (int k = 0; k < names.Length; k++)
            {
                Sprite s = VillageArt.Get(names[k]);
                if (s == null) continue;
                GameObject t = new GameObject(names[k]);
                t.transform.SetParent(g.transform, false);
                t.transform.localPosition = new Vector3(i * wrap + xs[k], 0f, 0f);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                sr.color = color;
                sr.sortingOrder = order;
            }
        }
    }

    /// <summary>Бесшовный силуэт гор (синусы с целым числом периодов на ширину тайла). Низ спрайта — опора.</summary>
    static Sprite MountainSprite(int seed, float tileW, float height, float baseFrac, float amp)
    {
        int w = Mathf.RoundToInt(tileW * 32f);
        int h = Mathf.RoundToInt(height * 32f);
        Color32[] px = new Color32[w * h];
        System.Random rng = new System.Random(seed);
        float p1 = (float)rng.NextDouble() * 6.28f;
        float p2 = (float)rng.NextDouble() * 6.28f;
        float p3 = (float)rng.NextDouble() * 6.28f;
        Color32 solid = new Color32(255, 255, 255, 255);
        for (int x = 0; x < w; x++)
        {
            float u = (float)x / w * Mathf.PI * 2f;
            float v = Mathf.Sin(u * 2f + p1) * 0.5f + Mathf.Sin(u * 5f + p2) * 0.28f + Mathf.Sin(u * 11f + p3) * 0.12f;
            int top = Mathf.Clamp(Mathf.RoundToInt((baseFrac + v * amp) * h), 4, h);
            for (int y = 0; y < top; y++) px[y * w + x] = solid;
        }
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels32(px);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0f), 32f);
    }

    // ------------------------------------------------------------ платформы

    static bool IsPlainSquare(SpriteRenderer sr)
    {
        if (sr.sprite == null) return false;
        if (sr.sprite.name.Contains("Square")) return true;
        return sr.sprite.texture != null && sr.sprite.texture.name.Contains("Square");
    }

    static void PaintPlatforms(string scene)
    {
        Transform decorRoot = new GameObject("Decor").transform;
        SpriteRenderer[] all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i];
            if (!IsPlainSquare(sr)) continue;
            Collider2D col = sr.GetComponent<Collider2D>();
            if (col == null || col.isTrigger) continue;

            Transform t = sr.transform;

            // платформы лежат ровно, а боковые стены в сценах повёрнуты на 90° — обрабатываем оба случая
            float z = Mathf.Repeat(t.eulerAngles.z, 360f);
            bool flat = z < 1f || z > 359f;
            bool quarter = Mathf.Abs(z - 90f) < 1f || Mathf.Abs(z - 270f) < 1f;
            if (!flat && !quarter) continue;

            Vector3 ls = t.lossyScale;
            Vector2 baseSize = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)sr.sprite.bounds.size : sr.size;
            float sx = Mathf.Abs(ls.x) * baseSize.x;   // размер вдоль локальной оси X
            float sy = Mathf.Abs(ls.y) * baseSize.y;   // размер вдоль локальной оси Y
            float w = quarter ? sy : sx;                // размер в мире (у повёрнутой стены ширина и высота меняются местами)
            float h = quarter ? sx : sy;
            if (w < 0.1f || h < 0.1f) continue;

            Sprite art = TerrainPainter.Build(w, h);
            if (art == null)
            {
                sr.color = new Color(0.30f, 0.20f, 0.15f, 1f); // запасной вариант: однотонная земля
                continue;
            }

            Vector3 center = t.position;
            int order = sr.sortingOrder;
            int layerId = sr.sortingLayerID;
            bool moving = t.GetComponent<MovingPlatform>() != null;

            GameObject child = new GameObject("TerrainArt");
            if (quarter)
            {
                // повёрнутая стена неподвижна: рисуем её земляную картинку отдельно, ровно, в мировых координатах
                child.transform.SetParent(decorRoot, false);
                child.transform.position = center;
            }
            else
            {
                // обычная платформа: картинка — дочерний объект (едет вместе с движущейся платформой), масштаб родителя компенсируем
                child.transform.SetParent(t, false);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
            }
            SpriteRenderer cr = child.AddComponent<SpriteRenderer>();
            cr.sprite = art;
            cr.sortingLayerID = layerId;
            cr.sortingOrder = order;
            sr.enabled = false; // старый белый квадрат больше не рисуем (коллайдер остаётся)

            if (!moving) Dress(scene, center, w, h, decorRoot);
        }
    }

    static void Dress(string scene, Vector3 center, float w, float h, Transform root)
    {
        float top = center.y + h * 0.5f;
        float left = center.x - w * 0.5f + 0.35f;
        float right = center.x + w * 0.5f - 0.35f;
        if (right <= left) return;

        System.Random rng = new System.Random(Mathf.RoundToInt(center.x * 100f) * 31 + Mathf.RoundToInt(center.y * 100f));
        bool boss = scene == "LevelBoss";

        // трава
        int tufts = Mathf.Clamp(Mathf.RoundToInt(w * 0.9f), 1, 26);
        for (int i = 0; i < tufts; i++)
            Prop(root, Tufts[rng.Next(Tufts.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -1);

        if (boss)
        {
            if (w >= 4f && rng.NextDouble() < 0.5) Prop(root, Rocks[rng.Next(Rocks.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -1);
            return;
        }

        if (w >= 2.2f && rng.NextDouble() < 0.6)
            Prop(root, Bushes[rng.Next(Bushes.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -1);
        if (w >= 2.5f && rng.NextDouble() < 0.3)
            Prop(root, Rocks[rng.Next(Rocks.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -1);

        if (scene == "Level1" || scene == "Level3")
        {
            if (w >= 3f && rng.NextDouble() < 0.4) Prop(root, Crates[rng.Next(Crates.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -2);
            if (w >= 4.5f && rng.NextDouble() < 0.3) Prop(root, "fence", Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -2);
            if (w >= 3f && rng.NextDouble() < 0.2) Prop(root, "sunflowers", Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -2);
            if (w >= 7f && rng.NextDouble() < 0.5) Prop(root, rng.NextDouble() < 0.5 ? "tree_small" : "tree_big", Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -4);
        }
        else if (w >= 3f && rng.NextDouble() < 0.3)
        {
            Prop(root, Crates[rng.Next(Crates.Length)], Mathf.Lerp(left, right, (float)rng.NextDouble()), top - 0.03f, -2);
        }
    }

    // ------------------------------------------------------------ для LevelRunner (детали из LevelData)

    /// <summary>Поставить предмет декора (имя из VillageArt) на землю в точке (x, y).</summary>
    public static void PlaceProp(Transform root, string name, float x, float y, int order)
    {
        Prop(root, name, x, y, order);
    }

    /// <summary>Трава и кусты на платформе, которую LevelRunner создал из LevelData.</summary>
    public static void DressPlatform(string scene, Vector3 center, float w, float h, Transform root)
    {
        Dress(scene, center, w, h, root);
    }

    // ------------------------------------------------------------ мелочи

    static void Prop(Transform root, string name, float x, float y, int order)
    {
        VillageArt.Spawn(name, new Vector3(x, y, 0f), order, PropTint, root);
    }

    static GameObject Quad(Transform parent, string name, Sprite s, Vector3 pos, Vector3 scale, Color c, int order)
    {
        GameObject g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        return g;
    }
}

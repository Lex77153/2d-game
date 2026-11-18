using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Декор из набора «Pixel Art Platformer – Village Props» (Cainos).
/// Лист Resources/Village/props.png режется на отдельные предметы по готовой таблице прямоугольников
/// (x, y — от ЛЕВОГО ВЕРХНЕГО угла листа, в пикселях). Масштаб набора — 32 пикселя на юнит,
/// это совпадает с размером героя (его коллайдер ~0.8 юнита).
/// </summary>
public static class VillageArt
{
    public const float PPU = 32f;

    static readonly Dictionary<string, RectInt> Rects = new Dictionary<string, RectInt>
    {
        { "tree_big", new RectInt(832, 447, 153, 162) },
        { "tree_small", new RectInt(693, 471, 120, 138) },
        { "bush_a", new RectInt(356, 580, 57, 29) },
        { "bush_b", new RectInt(452, 573, 89, 36) },
        { "bush_c", new RectInt(576, 572, 95, 37) },
        { "well", new RectInt(32, 165, 87, 91) },
        { "fence", new RectInt(254, 95, 68, 33) },
        { "crate", new RectInt(42, 19, 45, 45) },
        { "crate_small", new RectInt(127, 29, 35, 35) },
        { "barrel", new RectInt(195, 29, 27, 35) },
        { "signpost", new RectInt(388, 11, 59, 53) },
        { "signboard", new RectInt(480, 18, 32, 46) },
        { "hay_pile", new RectInt(192, 254, 62, 34) },
        { "hay_bale", new RectInt(284, 259, 41, 29) },
        { "pumpkins", new RectInt(576, 97, 60, 31) },
        { "scarecrow", new RectInt(704, 230, 64, 90) },
        { "sunflowers", new RectInt(899, 257, 58, 63) },
        { "banner", new RectInt(639, 226, 32, 60) },
        { "lantern", new RectInt(969, 6, 13, 18) },
        { "lamp_pole", new RectInt(941, 32, 16, 97) },
        { "stall", new RectInt(387, 320, 116, 96) },
        { "bench", new RectInt(453, 89, 85, 39) },
        { "grave_a", new RectInt(511, 145, 35, 47) },
        { "grave_b", new RectInt(579, 147, 27, 45) },
        { "grave_cross", new RectInt(643, 149, 27, 43) },
        { "statue", new RectInt(703, 115, 59, 77) },
        { "stump", new RectInt(800, 165, 31, 27) },
        { "rock_a", new RectInt(874, 358, 46, 26) },
        { "rock_b", new RectInt(934, 369, 21, 15) },
        { "wall", new RectInt(733, 629, 136, 43) },
        { "logs", new RectInt(163, 335, 95, 49) },
        { "wheat_a", new RectInt(514, 485, 59, 59) },
        { "wheat_b", new RectInt(580, 487, 56, 57) },
        { "tuft_1", new RectInt(391, 470, 17, 10) },
        { "tuft_2", new RectInt(421, 469, 23, 11) },
        { "tuft_3", new RectInt(390, 500, 19, 12) },
        { "tuft_4", new RectInt(455, 501, 17, 11) },
        { "tuft_5", new RectInt(391, 530, 20, 14) },
        { "tuft_6", new RectInt(418, 535, 29, 9) },
    };

    static Texture2D sheet;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>Спрайт предмета; точка опоры — низ по центру (удобно ставить «на землю»).</summary>
    public static Sprite Get(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s) && s != null) return s;

        if (sheet == null)
        {
            sheet = Resources.Load<Texture2D>("Village/props");
            if (sheet != null) sheet.filterMode = FilterMode.Point;
        }
        RectInt r;
        if (sheet == null || !Rects.TryGetValue(name, out r))
        {
            cache[name] = null;
            return null;
        }
        Rect rect = new Rect(r.x, sheet.height - r.y - r.height, r.width, r.height);
        s = Sprite.Create(sheet, rect, new Vector2(0.5f, 0f), PPU);
        s.name = "village_" + name;
        cache[name] = s;
        return s;
    }

    /// <summary>Создаёт объект с предметом. pos — точка на земле под предметом.</summary>
    public static SpriteRenderer Spawn(string name, Vector3 pos, int order, Color tint, Transform parent)
    {
        Sprite s = Get(name);
        if (s == null) return null;
        GameObject g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.position = pos;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = tint;
        sr.sortingOrder = order;
        return sr;
    }
}

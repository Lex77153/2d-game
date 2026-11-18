using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// «Одевает» платформы в землю из набора Village Props (TX Tileset Ground).
/// Для каждой платформы собирается текстура нужного размера из кусочков 96x96 «блока»:
/// трава сверху, камни по краям, тёмная земля внутри. Углы и края не растягиваются —
/// повторяется только середина, поэтому пиксели остаются квадратными.
/// </summary>
public static class TerrainPainter
{
    const int PxPerUnit = 32;
    const int Cap = 16;     // ширина левого / правого края в пикселях
    const int TopRows = 32; // высота «травяной» полосы

    static Color32[] src;
    static int srcW;
    static int srcH;
    static bool loaded;
    static bool failed;
    static readonly Dictionary<long, Sprite> cache = new Dictionary<long, Sprite>();

    static bool Load()
    {
        if (loaded) return !failed;
        loaded = true;
        Texture2D t = Resources.Load<Texture2D>("Village/ground_readable");
        if (t == null)
        {
            failed = true;
            return false;
        }
        try
        {
            src = t.GetPixels32();
            srcW = t.width;
            srcH = t.height;
        }
        catch (UnityException)
        {
            Debug.LogWarning("TerrainPainter: текстура ground_readable не читается (проверьте импорт). Платформы останутся однотонными.");
            failed = true;
            return false;
        }
        return true;
    }

    /// <summary>Спрайт земли размером worldW x worldH юнитов (32 пикселя на юнит) или null, если текстура недоступна.</summary>
    public static Sprite Build(float worldW, float worldH)
    {
        if (!Load()) return null;

        int pw = Mathf.Max(8, Mathf.RoundToInt(worldW * PxPerUnit));
        int ph = Mathf.Max(8, Mathf.RoundToInt(worldH * PxPerUnit));
        long key = ((long)pw << 32) | (long)ph;
        Sprite cached;
        if (cache.TryGetValue(key, out cached)) return cached;

        // у тонких платформ нижний край тоньше, чтобы осталась трава
        int bottom = ph < 40 ? Mathf.Clamp(ph / 3, 3, 8) : 8;

        Color32[] dst = new Color32[pw * ph];
        for (int y = 0; y < ph; y++)
        {
            int sy = MapY(y, ph, bottom);
            int srcRow = (srcH - 1 - sy) * srcW;   // строки в Unity идут снизу вверх
            int dstRow = (ph - 1 - y) * pw;
            for (int x = 0; x < pw; x++)
            {
                int sx = MapX(x, pw);
                dst[dstRow + x] = src[srcRow + sx];
            }
        }

        Texture2D tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(dst);
        tex.Apply(false, true);

        Sprite s = Sprite.Create(tex, new Rect(0f, 0f, pw, ph), new Vector2(0.5f, 0.5f), PxPerUnit);
        s.name = "terrain_" + pw + "x" + ph;
        cache[key] = s;
        return s;
    }

    // y и x здесь считаются от ЛЕВОГО ВЕРХНЕГО угла, как в картинке
    static int MapY(int y, int ph, int bottom)
    {
        if (ph < 40)
        {
            if (y < ph - bottom) return y;
            return (96 - bottom) + (y - (ph - bottom));
        }
        if (y < TopRows) return y;
        if (y >= ph - bottom) return (96 - bottom) + (y - (ph - bottom));
        return 32 + ((y - TopRows) % 32);
    }

    static int MapX(int x, int pw)
    {
        int cap = Mathf.Min(Cap, pw / 2);          // у узких стен левый и правый край делят ширину пополам
        if (x < cap) return x;
        if (x >= pw - cap) return 96 - (pw - x);   // последние колонки блока (правый край)
        return 32 + ((x - cap) % 32);
    }
}

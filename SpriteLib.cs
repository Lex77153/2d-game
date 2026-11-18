using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Помощник со спрайтами: режет горизонтальные полосы/листы из Resources на кадры
/// (без ручной нарезки в инспекторе) и создаёт простые процедурные спрайты.
/// </summary>
public static class SpriteLib
{
    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
    static Sprite white;
    static Sprite circle;

    /// <summary>
    /// Загружает Resources/<path> и режет на кадры fw x fh. row — номер строки сверху (для листа с несколькими анимациями).
    /// pivot — нормализованная точка опоры (0.5, y) где y — доля высоты кадра, на которой стоят ноги.
    /// </summary>
    public static Sprite[] Strip(string path, int fw, int fh, Vector2 pivot, int count = -1, int row = 0)
    {
        string key = path + "|" + fw + "|" + fh + "|" + row + "|" + count + "|" + pivot.x + "|" + pivot.y;
        Sprite[] res;
        if (cache.TryGetValue(key, out res)) return res;

        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("SpriteLib: нет текстуры Resources/" + path);
            res = new Sprite[0];
            cache[key] = res;
            return res;
        }
        tex.filterMode = FilterMode.Point;
        int n = tex.width / fw;
        if (count > 0 && count < n) n = count;
        res = new Sprite[n];
        for (int i = 0; i < n; i++)
        {
            Rect r = new Rect(i * fw, tex.height - (row + 1) * fh, fw, fh);
            res[i] = Sprite.Create(tex, r, pivot, 100f);
            res[i].name = path + "_" + row + "_" + i;
        }
        cache[key] = res;
        return res;
    }

    /// <summary>Один кадр (обрезка прямоугольника) из текстуры.</summary>
    public static Sprite Crop(string path, Rect rect)
    {
        string key = "crop|" + path + "|" + rect;
        Sprite[] res;
        if (cache.TryGetValue(key, out res)) return res.Length > 0 ? res[0] : null;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) { cache[key] = new Sprite[0]; return null; }
        tex.filterMode = FilterMode.Point;
        Sprite s = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
        cache[key] = new Sprite[] { s };
        return s;
    }

    /// <summary>Целая текстура как один спрайт.</summary>
    public static Sprite Whole(string path, float ppu = 100f)
    {
        string key = "whole|" + path;
        Sprite[] res;
        if (cache.TryGetValue(key, out res)) return res.Length > 0 ? res[0] : null;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) { cache[key] = new Sprite[0]; return null; }
        tex.filterMode = FilterMode.Point;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        cache[key] = new Sprite[] { s };
        return s;
    }

    /// <summary>Белый квадрат размером 1x1 юнит (центр в середине) — для колонн, телеграфа, полос.</summary>
    public static Sprite White()
    {
        if (white == null)
        {
            Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Point;
            white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        return white;
    }

    /// <summary>Мягкий круг диаметром 1 юнит — для огненных шаров и солнца.</summary>
    public static Sprite Circle()
    {
        if (circle == null)
        {
            int s = 64;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((1f - d) * 6f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            circle = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
        return circle;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Маленький «UI-кит»: палитра, загрузка спрайтов из Resources/UIKit и фабрики кнопок, текста, панелей.
/// Один и тот же код строит интерфейс и в игре, и в редакторе (Samurai ▸ Bake Mobile UI Into Scenes).
/// </summary>
public static class UIKit
{
    // ---------- палитра (закат: пурпур + оранжевый + золото) ----------
    public static readonly Color Ink    = new Color32(20, 10, 32, 255);
    public static readonly Color Cream  = new Color32(250, 232, 200, 255);
    public static readonly Color Gold   = new Color32(255, 209, 59, 255);
    public static readonly Color Orange = new Color32(245, 130, 40, 255);
    public static readonly Color Dim    = new Color(0.06f, 0.02f, 0.12f, 0.74f);

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static Material outlineMat;

    /// <summary>Толщина 9-slice рамки в пикселях для спрайтов, которые растягиваются (Image.Type.Sliced).</summary>
    public static Vector4 Border(string name)
    {
        if (name == "panel") return new Vector4(28f, 28f, 28f, 28f);
        if (name.StartsWith("btn_")) return new Vector4(24f, 24f, 24f, 24f);
        return Vector4.zero;
    }

    /// <summary>Спрайт из Resources/UIKit по имени файла (без .png).</summary>
    public static Sprite Spr(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s) && s != null) return s;
        s = Resources.Load<Sprite>("UIKit/" + name);
        if (s == null)
        {
            // запасной путь: если текстура не импортировалась как Sprite — режем сами
            Texture2D t = Resources.Load<Texture2D>("UIKit/" + name);
            if (t != null)
            {
                t.filterMode = FilterMode.Point;
                s = Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Border(name));
            }
            else
            {
                Debug.LogWarning("UIKit: нет спрайта Resources/UIKit/" + name);
            }
        }
        cache[name] = s;
        return s;
    }

    // ---------- создание объектов ----------

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.layer = LayerMask.NameToLayer("UI");
        g.transform.SetParent(parent, false);
        return g.GetComponent<RectTransform>();
    }

    /// <summary>Растянуть на весь родитель.</summary>
    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Привязка к точке (угол / край / центр) + позиция от неё + размер.</summary>
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Image AddImage(RectTransform rt, Sprite s, Color c, bool sliced, bool raycast)
    {
        Image img = rt.gameObject.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = raycast;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    /// <summary>Кнопка с подменой спрайта при нажатии (без «залипающих» состояний на тачскрине).</summary>
    public static Button AddButton(RectTransform rt, string normalSprite, string pressedSprite, bool sliced)
    {
        Sprite n = Spr(normalSprite);
        Image img = AddImage(rt, n, Color.white, sliced, true);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        SpriteState st = new SpriteState();
        st.pressedSprite = Spr(pressedSprite);
        st.highlightedSprite = n;
        st.selectedSprite = n;
        st.disabledSprite = n;
        b.spriteState = st;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        return b;
    }

    public static TextMeshProUGUI AddText(RectTransform rt, string text, float size, Color col, TextAlignmentOptions align)
    {
        TextMeshProUGUI t = rt.gameObject.GetComponent<TextMeshProUGUI>();
        if (t == null) t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(t, text, size, col, align);
        return t;
    }

    /// <summary>Единый стиль текста: жирный, с тёмным контуром — читается на любом фоне.</summary>
    public static void StyleText(TextMeshProUGUI t, string text, float size, Color col, TextAlignmentOptions align)
    {
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = col;
        t.alignment = align;
        t.raycastTarget = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.enableAutoSizing = false;
        if (outlineMat == null) outlineMat = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
        if (outlineMat != null) t.fontSharedMaterial = outlineMat;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    /// <summary>Destroy в игре, DestroyImmediate в редакторе (для запекания UI в сцену).</summary>
    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    public static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}

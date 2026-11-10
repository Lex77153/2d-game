using UnityEngine;

/// <summary>
/// Подгоняет RectTransform под Screen.safeArea (вырезы, скруглённые углы, жесты iOS).
/// Всё, что должно быть нажимаемым или читаемым, кладём внутрь этой панели.
/// Работает и в Device Simulator, и на реальном телефоне, и в Edit-режиме.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafe;
    int lastW, lastH;

    void OnEnable() { Apply(); }

    void Update()
    {
        if (Screen.safeArea != lastSafe || Screen.width != lastW || Screen.height != lastH) Apply();
    }

    void Apply()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        lastSafe = safe;
        lastW = Screen.width;
        lastH = Screen.height;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

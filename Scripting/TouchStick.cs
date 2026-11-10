using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Экранный стик (слева внизу). Платформер ходит только по X, поэтому берём горизонталь.</summary>
public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform knob;
    public float radius = 105f;     // максимальный сдвиг шарика в единицах Canvas
    public float deadZone = 0.2f;   // мёртвая зона, чтобы герой не дрейфовал

    RectTransform area;

    void Awake() { area = (RectTransform)transform; }

    public void OnPointerDown(PointerEventData e) { Handle(e); }
    public void OnDrag(PointerEventData e) { Handle(e); }
    public void OnPointerUp(PointerEventData e) { Release(); }
    void OnDisable() { Release(); }

    void Handle(PointerEventData e)
    {
        if (area == null) area = (RectTransform)transform;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out local)) return;

        Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
        if (knob != null) knob.anchoredPosition = v * radius;

        float ax = Mathf.Abs(v.x);
        float x = 0f;
        if (ax > deadZone) x = Mathf.Sign(v.x) * Mathf.Clamp01((ax - deadZone) / (0.8f - deadZone));
        TouchInput.Move = new Vector2(x, 0f);
    }

    void Release()
    {
        TouchInput.Move = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
}

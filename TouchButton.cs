using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Круглая кнопка действия (прыжок / удар). Реагирует сразу на касание, без задержки.</summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Kind { Jump, Attack }

    public Kind kind;
    public Sprite normal;
    public Sprite pressed;

    Image img;

    void Awake() { img = GetComponent<Image>(); }

    public void OnPointerDown(PointerEventData e)
    {
        if (img != null && pressed != null) img.sprite = pressed;
        if (kind == Kind.Jump) TouchInput.QueueJump();
        else TouchInput.QueueAttack();
    }

    public void OnPointerUp(PointerEventData e) { ShowNormal(); }
    void OnDisable() { ShowNormal(); }

    void ShowNormal()
    {
        if (img == null) img = GetComponent<Image>();
        if (img != null && normal != null) img.sprite = normal;
    }
}

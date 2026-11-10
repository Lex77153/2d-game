using UnityEngine;

/// <summary>Зацикленная покадровая анимация для декораций (меню).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    SpriteRenderer sr;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime;
        sr.sprite = frames[(int)(t * fps) % frames.Length];
    }
}

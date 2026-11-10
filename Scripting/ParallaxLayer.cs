using UnityEngine;

/// <summary>
/// Слой фона с параллаксом: едет за камерой с коэффициентом factor
/// (1 — прилип к камере, как бесконечно далёкое небо; 0 — стоит на месте, как мир).
/// Слой состоит из одинаковых тайлов, поэтому периодически «перескакивает» на ширину тайла и не заканчивается.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    public float factor = 0.8f;
    public float wrapWidth = 24f;

    Vector3 origin;
    Vector3 camOrigin;
    Camera cam;
    bool ready;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 c = cam.transform.position;
        if (!ready)
        {
            origin = transform.position;
            camOrigin = c;
            ready = true;
        }

        float x = origin.x + (c.x - camOrigin.x) * factor;
        float y = origin.y + (c.y - camOrigin.y) * factor;
        if (wrapWidth > 0.01f) x += wrapWidth * Mathf.Round((c.x - x) / wrapWidth);
        transform.position = new Vector3(x, y, origin.z);
    }
}

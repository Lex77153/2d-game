using UnityEngine;

/// <summary>Держит объект (небо, солнце) в центре камеры — фон всегда закрывает весь экран при любом соотношении сторон.</summary>
[DefaultExecutionOrder(100)]
public class CameraAnchor : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x, c.y, transform.position.z);
    }
}

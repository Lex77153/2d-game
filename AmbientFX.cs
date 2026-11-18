using UnityEngine;

/// <summary>
/// Фоновые частицы уровня: светлячки, угольки или листья. Рисуются простыми спрайтами и
/// всё время «обёртываются» вокруг камеры, поэтому хватает пары десятков штук на весь уровень.
/// Создаётся из LevelData (поля ambient и ambientCount).
/// </summary>
public class AmbientFX : MonoBehaviour
{
    class Mote
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 pos;
        public Vector2 vel;
        public float phase;
        public float wobble;
        public float spin;
        public float baseAlpha;
        public Color color;
    }

    const float Margin = 1.5f;   // частицы живут чуть шире экрана, чтобы не появлялись у самого края

    Mote[] motes;
    Camera cam;

    public static void Create(AmbientKind kind, int count)
    {
        if (kind == AmbientKind.None || count <= 0) return;
        GameObject go = new GameObject("AmbientFX");
        AmbientFX fx = go.AddComponent<AmbientFX>();
        fx.Init(kind, count);
    }

    void Init(AmbientKind kind, int count)
    {
        cam = Camera.main;
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        float hw = HalfWidth() + Margin;
        float hh = HalfHeight() + Margin;

        Sprite dot = SpriteLib.Circle();
        Sprite square = SpriteLib.White();

        motes = new Mote[count];
        for (int i = 0; i < count; i++)
        {
            Mote m = new Mote();
            GameObject g = new GameObject("Mote");
            g.transform.SetParent(transform, false);
            m.t = g.transform;
            m.sr = g.AddComponent<SpriteRenderer>();
            m.sr.sortingOrder = 6;
            m.pos = new Vector2(c.x + Random.Range(-hw, hw), c.y + Random.Range(-hh, hh));
            m.phase = Random.Range(0f, 6.28f);

            float size;
            switch (kind)
            {
                case AmbientKind.Embers:
                    m.sr.sprite = dot;
                    size = Random.Range(0.06f, 0.14f);
                    m.color = new Color(1f, Random.Range(0.35f, 0.65f), 0.12f, 1f);
                    m.vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.4f));
                    m.wobble = 0.6f;
                    m.baseAlpha = Random.Range(0.5f, 1f);
                    break;
                case AmbientKind.Leaves:
                    m.sr.sprite = square;
                    size = Random.Range(0.08f, 0.14f);
                    m.color = new Color(Random.Range(0.75f, 0.95f), Random.Range(0.4f, 0.6f), 0.2f, 1f);
                    m.vel = new Vector2(Random.Range(0.8f, 1.6f), Random.Range(-0.9f, -0.5f));
                    m.wobble = 0.9f;
                    m.spin = Random.Range(-120f, 120f);
                    m.baseAlpha = 0.85f;
                    break;
                default: // Fireflies
                    m.sr.sprite = dot;
                    size = Random.Range(0.10f, 0.18f);
                    m.color = new Color(1f, 0.92f, 0.45f, 1f);
                    m.vel = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.15f, 0.15f));
                    m.wobble = 0.7f;
                    m.baseAlpha = Random.Range(0.6f, 1f);
                    break;
            }
            m.t.localScale = new Vector3(size, size, 1f);
            m.sr.color = m.color;
            m.t.position = new Vector3(m.pos.x, m.pos.y, 0f);
            motes[i] = m;
        }
    }

    float HalfHeight() { return cam != null ? cam.orthographicSize : 5f; }
    float HalfWidth() { return cam != null ? cam.orthographicSize * cam.aspect : 9f; }

    void LateUpdate()
    {
        if (motes == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 c = cam.transform.position;
        float hw = HalfWidth() + Margin;
        float hh = HalfHeight() + Margin;
        float dt = Time.deltaTime;
        float time = Time.time;

        for (int i = 0; i < motes.Length; i++)
        {
            Mote m = motes[i];
            float sway = Mathf.Sin(time * 0.9f + m.phase) * m.wobble;
            float bob = Mathf.Cos(time * 0.7f + m.phase) * m.wobble * 0.3f;
            m.pos += (m.vel + new Vector2(sway * 0.4f, bob)) * dt;

            // вернуть частицу в рамку вокруг камеры (работает при любом смещении камеры)
            float x = c.x + Mathf.Repeat(m.pos.x - c.x + hw, 2f * hw) - hw;
            float y = c.y + Mathf.Repeat(m.pos.y - c.y + hh, 2f * hh) - hh;
            m.pos = new Vector2(x, y);
            m.t.position = new Vector3(x, y, 0f);

            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2f + m.phase * 3f);
            Color col = m.color;
            col.a = m.baseAlpha * (0.35f + 0.65f * pulse);
            m.sr.color = col;
            if (m.spin != 0f) m.t.Rotate(0f, 0f, m.spin * dt);
        }
    }
}

using UnityEngine;

/// <summary>
/// Снаряд: летит по прямой, наносит урон игроку, исчезает о землю.
/// Spawn — огненный шар босса; SpawnShuriken — вращающийся сюрикен Синоби (EnemyShooter).
/// </summary>
public class BossFireball : MonoBehaviour
{
    Vector2 dir;
    float speed;
    float life = 6f;
    bool spin;

    public static BossFireball Spawn(Vector2 pos, Vector2 direction, float speed)
    {
        GameObject go = new GameObject("BossFireball");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        // внешнее свечение
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLib.Circle();
        sr.color = new Color(1f, 0.45f, 0.1f, 0.95f);
        sr.sortingOrder = 8;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // горячее ядро
        GameObject core = new GameObject("Core");
        core.transform.SetParent(go.transform, false);
        core.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer csr = core.AddComponent<SpriteRenderer>();
        csr.sprite = SpriteLib.Circle();
        csr.color = new Color(1f, 0.9f, 0.4f, 1f);
        csr.sortingOrder = 9;

        // искры позади шара
        GameObject tail = new GameObject("Spark");
        tail.transform.SetParent(go.transform, false);
        tail.transform.localPosition = new Vector3(-0.6f, 0f, 0f);
        tail.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        SpriteRenderer tsr = tail.AddComponent<SpriteRenderer>();
        tsr.sprite = SpriteLib.Crop("FX/spark1", new Rect(0, 0, 40, 30));
        tsr.sortingOrder = 7;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BossFireball f = go.AddComponent<BossFireball>();
        f.dir = direction.normalized;
        f.speed = speed;
        go.transform.right = new Vector3(f.dir.x, f.dir.y, 0f);
        return f;
    }

    static Sprite shurikenSprite;

    // четырёхлучевая звезда, нарисованная кодом (без новых файлов)
    static Sprite ShurikenSprite()
    {
        if (shurikenSprite == null)
        {
            int size = 24;
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x - c) / c);
                    float ny = Mathf.Abs((y - c) / c);
                    float v = Mathf.Pow(nx, 0.75f) + Mathf.Pow(ny, 0.75f);
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    Color col = Color.clear;
                    if (v <= 1f && d > 0.18f)
                        col = v > 0.8f ? new Color(0.88f, 0.9f, 0.96f, 1f) : new Color(0.22f, 0.24f, 0.30f, 1f);
                    px[y * size + x] = col;
                }
            }
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Point;
            shurikenSprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        return shurikenSprite;
    }

    /// <summary>Сюрикен Синоби: вращается, летит по прямой, исчезает о стену, бьёт игрока на 1 сердце.</summary>
    public static BossFireball SpawnShuriken(Vector2 pos, Vector2 direction, float speed)
    {
        GameObject go = new GameObject("Shuriken");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        SpriteRenderer ssr = go.AddComponent<SpriteRenderer>();
        ssr.sprite = ShurikenSprite();
        ssr.sortingOrder = 8;

        CircleCollider2D scol = go.AddComponent<CircleCollider2D>();
        scol.isTrigger = true;
        scol.radius = 0.45f;

        Rigidbody2D srb = go.AddComponent<Rigidbody2D>();
        srb.bodyType = RigidbodyType2D.Kinematic;

        BossFireball f = go.AddComponent<BossFireball>();
        f.dir = direction.normalized;
        f.speed = speed;
        f.spin = true;
        f.life = 4f;
        return f;
    }

    void Update()
    {
        if (spin) transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.TakeDamage();
            Destroy(gameObject);
            return;
        }
        // монеты, сердечки и портал снаряд не останавливают
        string ln = LayerMask.LayerToName(other.gameObject.layer);
        if (ln == "Coin" || ln == "Heart" || ln == "Portal") return;

        // земля / стены (не триггеры, не враги, не босс)
        if (!other.isTrigger && other.GetComponentInParent<Boss>() == null && other.GetComponentInParent<Enemy>() == null)
        {
            Destroy(gameObject);
        }
    }
}

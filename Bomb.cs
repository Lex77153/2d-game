using UnityEngine;
using System.Collections;

public class Bomb : MonoBehaviour
{
    // числа бомбы лежат в ItemStats (Resources/Data/ItemStats); здесь рабочие копии
    [HideInInspector] public float totalLifetime;
    [HideInInspector] public float blinkStartTime;
    [HideInInspector] public float blinkInterval;
    [HideInInspector] public float explosionRadius;
    [HideInInspector] public int damage;

    [Header("Слой игрока")]
    public LayerMask playerLayer;

    [Header("Звуки")]
    public AudioClip explosionSound;     

    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private bool hasExploded = false;

    void Start()
    {
        ItemStats items = GameData.Items;
        totalLifetime = items.bombLifetime;
        blinkStartTime = items.bombBlinkStartTime;
        blinkInterval = items.bombBlinkInterval;
        explosionRadius = items.bombRadius;
        damage = items.bombDamage;

        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        StartCoroutine(BombRoutine());
    }

    IEnumerator BombRoutine()
    {
        yield return new WaitForSeconds(blinkStartTime);
        float blinkDuration = totalLifetime - blinkStartTime;
        float timer = 0f;

        while (timer < blinkDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        if (anim != null)
        {
            anim.SetTrigger("Explode");
        }
    }
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        GameAudio.Play(explosionSound, "explosion-of-a-failed-experiment", 1f);

        Collider2D hitPlayer = Physics2D.OverlapCircle(transform.position, explosionRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController player = hitPlayer.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }

        Debug.Log("Бомба взорвалась и нанесла урон!");
    }

    public void DestroyBomb()
    {
        Destroy(gameObject);
    }
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;   // радиус берётся из ItemStats во время игры
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
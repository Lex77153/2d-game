using UnityEngine;
using System.Collections;

public class Bomb : MonoBehaviour
{
    [Header("Настройки времени")]
    public float totalLifetime = 5f;     
    public float blinkStartTime = 3f;   

    [Header("Настройки урона")]
    public float explosionRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("Звуки")]
    public AudioClip explosionSound;     

    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private bool hasExploded = false;

    void Start()
    {
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

            yield return new WaitForSeconds(0.15f); 
            timer += 0.15f;
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
                player.TakeDamage();
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
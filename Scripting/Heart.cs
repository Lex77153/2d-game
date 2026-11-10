using UnityEngine;
using System.Collections;

public class Heart : MonoBehaviour
{
    public int healAmount = 1;
    private bool isCollected = false;

    [Header("Таймер жизни")]
    public float lifetime = 5f;       
    public float blinkStartTime = 3f; 
    private SpriteRenderer spriteRenderer;

    [Header("Звук")]
    public AudioClip healSound;       
    public float soundVolume = 1.0f;  

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Destroy(gameObject, lifetime);

        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        yield return new WaitForSeconds(lifetime - blinkStartTime);
        while (true)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                isCollected = true;
                player.Heal(healAmount);
                GameAudio.Play(healSound, "freesound_community-healpop-46004", soundVolume);

                Destroy(gameObject);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public class Heart : MonoBehaviour
{
    // числа сердца лежат в ItemStats (Resources/Data/ItemStats)
    [HideInInspector] public int healAmount;
    private bool isCollected = false;

    [HideInInspector] public float lifetime;
    [HideInInspector] public float blinkStartTime;
    [HideInInspector] public float blinkInterval;
    /// <summary>Сердца, расставленные в LevelData, лежат, пока их не подберут (не исчезают и не мигают).</summary>
    [HideInInspector] public bool permanent;
    private SpriteRenderer spriteRenderer;

    [Header("Звук")]
    public AudioClip healSound;       
    public float soundVolume = 1.0f;  

    void Awake()
    {
        healAmount = GameData.Items.heartHeal;   // уже готово, даже если подобрали в самый первый кадр
    }

    void Start()
    {
        ItemStats items = GameData.Items;
        healAmount = items.heartHeal;
        lifetime = items.heartLifetime;
        blinkStartTime = items.heartBlinkStartTime;
        blinkInterval = items.heartBlinkInterval;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (permanent) return;
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
            yield return new WaitForSeconds(blinkInterval);
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
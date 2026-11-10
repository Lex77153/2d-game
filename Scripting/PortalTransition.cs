using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTransition : MonoBehaviour
{
    [Header("Следующая сцена")]
    public string nextSceneName = "Level2"; // имя сцены, куда ведёт портал

    private bool used = false;
    private SpriteRenderer portalSprite;
    private Collider2D trigger;
    private float nextHintTime;

    private void Awake()
    {
        portalSprite = GetComponentInParent<SpriteRenderer>();
        trigger = GetComponent<Collider2D>();
    }

    // На Level1 и Level3 портал пускает только после победы над ВСЕМИ врагами
    private bool EnemiesBlock()
    {
        return Enemy.IsEnemyLevel(SceneManager.GetActiveScene().name) && Enemy.enemiesRemaining > 0;
    }

    private bool IsOpen()
    {
        if (EnemiesBlock()) return false;
        return trigger == null || trigger.enabled;
    }

    private void Update()
    {
        // все враги побеждены (в том числе упали в пропасть) — гарантированно открываем портал
        if (trigger != null && !trigger.enabled && Enemy.IsEnemyLevel(SceneManager.GetActiveScene().name) && Enemy.enemiesRemaining == 0)
            trigger.enabled = true;

        // пока портал закрыт, он тёмный и полупрозрачный
        if (portalSprite != null)
            portalSprite.color = IsOpen() ? Color.white : new Color(0.45f, 0.45f, 0.6f, 0.7f);
    }

    private void OnTriggerEnter2D(Collider2D collision) { TryEnter(collision, true); }
    private void OnTriggerStay2D(Collider2D collision) { TryEnter(collision, false); }

    private void TryEnter(Collider2D collision, bool showHint)
    {
        if (used) return;
        // проверяем, что в портал зашёл именно игрок
        if (!collision.CompareTag("Player")) return;

        if (EnemiesBlock())
        {
            if (showHint && Time.unscaledTime >= nextHintTime)
            {
                nextHintTime = Time.unscaledTime + 1.5f;
                HudController hud = HudController.Current;
                if (hud != null) hud.Toast("DEFEAT ALL ENEMIES FIRST!  (" + Enemy.enemiesRemaining + " left)", 2f);
            }
            return;
        }

        used = true;
        CoinManager.Bank(); // монеты переходят на следующий уровень
        Debug.Log("Игрок вошёл в портал! Загружаем сцену: " + nextSceneName);
        GameAudio.Play("portal", 0.9f);
        SceneManager.LoadScene(nextSceneName);
    }
}

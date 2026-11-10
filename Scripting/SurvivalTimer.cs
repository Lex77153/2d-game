using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SurvivalTimer : MonoBehaviour
{
    public static SurvivalTimer instance;

    [Header("Настройки выживания")]
    public float goal = 40f; // 40 секунд выживания
    private float t;
    private bool isGameOver = false;

    [Header("UI Элементы")]
    public GameObject winObject;
    public GameObject loseObject;
    public TextMeshProUGUI countdownText;

    [Header("Портал")]
    public GameObject portalEx; // Ссылка на главный объект Portal_ex

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if (winObject == null)
        {
            GameObject found = GameObject.Find("WinText");
            if (found != null) winObject = found;
        }
        if (loseObject == null)
        {
            GameObject found = GameObject.Find("Lose text");
            if (found != null) loseObject = found;
        }
        if (winObject != null) winObject.SetActive(false);
        if (loseObject != null) loseObject.SetActive(false);

        if (countdownText == null)
        {
            GameObject textObj = GameObject.Find("SurvivalCountdownText");
            if (textObj != null) countdownText = textObj.GetComponent<TextMeshProUGUI>();
        }

        // Принудительно выключаем коллайдер портала в самом начале игры
        SetPortalActive(false);
    }

    void Update()
    {
        // таймер выживания работает только на Level2
        if (SceneManager.GetActiveScene().name != "Level2") return;
        if (isGameOver) return;

        t += Time.deltaTime;
        UpdateCountdownUI();

        if (t >= goal)
        {
            WinGame();
        }
    }

    void UpdateCountdownUI()
    {
        if (countdownText != null)
        {
            float timeLeft = goal - t;
            if (timeLeft < 0f) timeLeft = 0f;
            countdownText.text = "SURVIVE  " + timeLeft.ToString("F1") + "s";
        }
    }

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("Время выживания истекло! Открываем портал и останавливаем бомбы.");

        if (countdownText != null)
        {
            countdownText.text = "PORTAL OPEN!";
        }

        // 1. Включаем коллайдер портала
        SetPortalActive(true);

        // 2. Останавливаем спавн бомб (используем FindAnyObjectByType для современных версий Unity)
        BombSpawner spawner = FindAnyObjectByType<BombSpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
        }
    }

    // Метод для включения/выключения коллайдера портала
    void SetPortalActive(bool active)
    {
        if (portalEx == null)
        {
            GameObject foundPortal = GameObject.FindWithTag("Portal");
            if (foundPortal != null) portalEx = foundPortal;
        }

        if (portalEx != null)
        {
            Transform centerTrigger = portalEx.transform.Find("CenterTrigger");
            if (centerTrigger != null)
            {
                Collider2D col = centerTrigger.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = active;
                    Debug.Log("Состояние коллайдера портала изменено на: " + active);
                }
            }
        }
    }

    public void LoseGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("Сработал метод LoseGame() на втором уровне!");

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (winObject != null) winObject.SetActive(false);

        if (loseObject != null)
        {
            loseObject.SetActive(true);
        }
    }

    private System.Collections.IEnumerator RestartRoutine()
    {
        // Ждем 4 реальных секунды (даже если timeScale изменен)
        yield return new WaitForSecondsRealtime(4f);

        Time.timeScale = 1f;

        // Перезагружаем текущую активную сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
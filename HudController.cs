using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Живая часть HUD: сердца по здоровью героя, счёт монет, окно паузы, клавиша Esc / кнопка «Назад» на Android.
/// Все ссылки заполняет MobileUI при построении интерфейса (или они сохранены в сцене после запекания).
/// </summary>
public class HudController : MonoBehaviour
{
    public static HudController Current;

    [Header("Заполняет MobileUI")]
    public RectTransform hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public TextMeshProUGUI scoreValue;
    public TextMeshProUGUI levelText;
    public GameObject levelPlate;        // подложка с названием уровня
    public GameObject timerPlate;        // подложка таймера выживания (Level 2)
    public GameObject timerText;         // сам текст таймера (его выключает SurvivalTimer при поражении)
    public RectTransform infoSlot;       // сюда босс кладёт свою полоску здоровья
    public GameObject pauseRoot;         // затемнение + окно паузы (выключено по умолчанию)
    public GameObject pauseButton;

    readonly List<Image> heartImages = new List<Image>();
    PlayerController player;
    int shownHp = -1;
    int shownMax = -1;
    int shownEnemies = -2;
    bool isLevel1;
    TextMeshProUGUI toast;
    float toastUntil;

    void Awake() { Current = this; }

    void OnEnable() { GameFlow.PausedChanged += OnPausedChanged; }
    void OnDisable() { GameFlow.PausedChanged -= OnPausedChanged; }
    void OnDestroy() { if (Current == this) Current = null; }

    void Start()
    {
        isLevel1 = Enemy.IsEnemyLevel(SceneManager.GetActiveScene().name);   // Level1 и Level3: считаем врагов
        if (pauseRoot != null) pauseRoot.SetActive(false);

        // подключаем счёт к менеджеру монет (текст мог лежать в старом Canvas)
        if (CoinManager.instance != null && scoreValue != null)
        {
            CoinManager.instance.coinText = scoreValue;
            CoinManager.instance.AddCoin(0);
        }
    }

    void Update()
    {
        // Esc на ПК / системная кнопка «Назад» на Android
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
        {
            if (player == null || !player.IsDead) GameFlow.TogglePause();
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }

        if (player != null)
        {
            RefreshHearts(player.CurrentHealth, player.MaxHealth);
            if (pauseButton != null && pauseButton.activeSelf == player.IsDead) pauseButton.SetActive(!player.IsDead);
        }

        // Level 1: считаем врагов — портал откроется, когда их не останется
        if (isLevel1 && levelText != null)
        {
            int left = Enemy.enemiesRemaining;
            if (left != shownEnemies)
            {
                shownEnemies = left;
                levelText.text = left > 0 ? "ENEMIES LEFT: " + left : "PORTAL OPEN!";
            }
        }

        // короткое сообщение по центру пропадает само
        if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil)
            toast.gameObject.SetActive(false);

        // подложка таймера исчезает вместе с его текстом
        if (timerPlate != null && timerText != null && timerPlate.activeSelf != timerText.activeSelf)
            timerPlate.SetActive(timerText.activeSelf);
    }

    void OnPausedChanged(bool paused)
    {
        if (pauseRoot != null) pauseRoot.SetActive(paused);
    }

    void RefreshHearts(int hp, int max)
    {
        if (hearts == null || max <= 0) return;
        if (hp == shownHp && max == shownMax) return;
        shownHp = hp;
        shownMax = max;

        // пересобираем ряд, если изменилось максимальное здоровье
        if (heartImages.Count != max)
        {
            for (int i = hearts.childCount - 1; i >= 0; i--) UIKit.Kill(hearts.GetChild(i).gameObject);
            heartImages.Clear();
            for (int i = 0; i < max; i++)
            {
                RectTransform r = UIKit.NewRect("Heart" + (i + 1), hearts);
                UIKit.Place(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 105f, 0f), new Vector2(95f, 90f));
                heartImages.Add(UIKit.AddImage(r, heartFull, Color.white, false, false));
            }
        }
        for (int i = 0; i < heartImages.Count; i++)
            heartImages[i].sprite = i < hp ? heartFull : heartEmpty;
    }

    /// <summary>Короткая подсказка по центру экрана (например, «убейте всех врагов»).</summary>
    public void Toast(string message, float seconds)
    {
        if (toast == null)
        {
            RectTransform r = UIKit.NewRect("Toast", transform);
            UIKit.Place(r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 120f));
            toast = UIKit.AddText(r, message, 56f, UIKit.Gold, TextAlignmentOptions.Center);
        }
        toast.text = message;
        toast.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + seconds;
    }

    /// <summary>Босс прячет подпись уровня и ставит на её место свою полоску здоровья.</summary>
    public void ShowLevelLabel(bool show)
    {
        GameObject target = levelPlate != null ? levelPlate : (levelText != null ? levelText.gameObject : null);
        if (target != null) target.SetActive(show);
    }
}

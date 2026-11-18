using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;

    // Монеты, накопленные к началу текущего уровня. Переходят на следующий уровень через портал.
    // При смерти / Restart уровень грузится заново с этим значением, а новая игра начинается с нуля.
    public static int banked = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { banked = 0; }

    /// <summary>Новая игра: счёт с нуля.</summary>
    public static void ResetRun() { banked = 0; }

    /// <summary>Уровень пройден: запоминаем монеты для следующего уровня.</summary>
    public static void Bank()
    {
        if (instance != null) banked = instance.coinCount;
    }

    [Header("Настройки")]
    public int coinCount = 0;

    [Header("UI Элементы")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI winText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            coinCount = banked; // продолжаем с монетами, собранными на прошлых уровнях
        }
        else
        {
            // Дубликат: убираем только компонент, а не весь объект
            // (на объекте может лежать текст интерфейса)
            Destroy(this);
        }
    }

    private void Start()
    {
        if (instance != this) return; // дубликат на сцене не трогает текст счёта
        UpdateUI();
        if (winText != null)
        {
            winText.gameObject.SetActive(false);
        }
    }

    public void AddCoin(int amount)
    {
        coinCount += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = coinCount.ToString(); // подпись «SCORE» и иконка монеты — в HUD
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Экраны главного меню, которые строятся кодом поверх запечённого интерфейса:
///  * выбор уровня (замки и рекорды берутся из save.json);
///  * настройки: громкость музыки и звука (сохраняется сразу при изменении) и сброс прогресса с подтверждением;
///  * короткое сообщение, если файл сохранения был повреждён.
/// Размеры — в единицах Canvas 1920x1080, все кнопки не меньше 120x120, всё лежит внутри SafeArea.
/// </summary>
public static class MenuScreens
{
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);

    static GameObject levelSelectRoot;
    static GameObject settingsRoot;
    static GameObject confirmRoot;
    static TextMeshProUGUI musicValue;
    static TextMeshProUGUI sfxValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        levelSelectRoot = null;
        settingsRoot = null;
        confirmRoot = null;
        musicValue = null;
        sfxValue = null;
    }

    // ================================================================ главное меню

    /// <summary>Вызывается GameBootstrap после загрузки MainMenu.</summary>
    public static void InstallMainMenu()
    {
        MobileUIRoot ui = Object.FindFirstObjectByType<MobileUIRoot>();
        if (ui == null) return;

        RectTransform play = UIKit.FindDeep(ui.transform, "PlayButton") as RectTransform;
        RectTransform quit = UIKit.FindDeep(ui.transform, "QuitButton") as RectTransform;
        if (play == null || quit == null) return;

        // PLAY теперь открывает выбор уровня
        UIAction playAction = play.GetComponent<UIAction>();
        if (playAction != null) playAction.kind = UIAction.Kind.LevelSelect;

        // PLAY / SETTINGS / QUIT друг под другом
        play.anchoredPosition = new Vector2(0f, -10f);
        quit.anchoredPosition = new Vector2(0f, -370f);
        if (UIKit.FindDeep(ui.transform, "SettingsButton") == null)
            MakeButton(play.parent, "SettingsButton", "SETTINGS", "btn_dark", "btn_dark_down", true, new Vector2(0f, -190f), new Vector2(640f, 150f), 72f, ShowSettings);

        RefreshBestScoreLabel(play.parent);

        if (SaveSystem.LastWarning != null)
        {
            ShowNotice(SaveSystem.LastWarning, 5f);
            SaveSystem.ClearWarning();
        }
    }

    static void RefreshBestScoreLabel(Transform parent)
    {
        Transform old = UIKit.FindDeep(parent, "BestScore");
        if (old != null) UIKit.Kill(old.gameObject);

        int best = SaveSystem.Data.bestScore;
        if (best <= 0) return;
        RectTransform r = UIKit.NewRect("BestScore", parent);
        UIKit.Place(r, BC, BC, new Vector2(0f, 30f), new Vector2(900f, 80f));
        UIKit.AddText(r, "BEST SCORE: " + best, 52f, UIKit.Gold, TextAlignmentOptions.Center);
    }

    // ================================================================ выбор уровня

    public static void ShowLevelSelect()
    {
        Close(ref levelSelectRoot);
        Close(ref settingsRoot);

        GameObject rootGo;
        RectTransform safe = CreateOverlay("LevelSelectCanvas", 90, out rootGo);
        levelSelectRoot = rootGo;

        RectTransform window = CreateWindow(safe, new Vector2(1600f, 940f));
        Label(window, "Title", "SELECT LEVEL", 100f, UIKit.Gold, new Vector2(0f, 380f), new Vector2(1400f, 130f), TextAlignmentOptions.Center);
        Label(window, "Best", "BEST SCORE: " + SaveSystem.Data.bestScore, 50f, UIKit.Cream, new Vector2(0f, 290f), new Vector2(1000f, 80f), TextAlignmentOptions.Center);

        System.Collections.Generic.List<LevelData> levels = GameData.Levels;
        int n = levels.Count;
        float gap = 40f;
        float cardW = Mathf.Min(340f, (1480f - (n - 1) * gap) / Mathf.Max(1, n));
        float totalW = n * cardW + (n - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;
        for (int i = 0; i < n; i++)
            BuildLevelCard(window, levels[i], new Vector2(startX + i * (cardW + gap), 10f), new Vector2(cardW, 430f));

        MakeButton(window, "BackButton", "BACK", "btn_dark", "btn_dark_down", true, new Vector2(0f, -360f), new Vector2(520f, 140f), 72f, CloseLevelSelect);
    }

    static void BuildLevelCard(RectTransform parent, LevelData level, Vector2 pos, Vector2 size)
    {
        bool unlocked = SaveSystem.IsUnlocked(level.levelNumber);
        LevelRecord record = SaveSystem.GetRecord(level.levelNumber);
        int number = level.levelNumber;

        RectTransform card = UIKit.NewRect("Level" + number, parent);
        UIKit.Place(card, C, C, pos, size);
        Button button = UIKit.AddButton(card, unlocked ? "btn_orange" : "btn_dark", unlocked ? "btn_orange_down" : "btn_dark_down", true);
        if (unlocked)
        {
            button.onClick.AddListener(() => GameFlow.StartLevel(number));
        }
        else
        {
            button.interactable = false;
            card.GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.62f, 1f);   // закрытый уровень — тусклый
        }

        bool boss = level.goal == LevelGoal.DefeatBoss;
        Label(card, "Number", boss ? "BOSS" : number.ToString(), boss ? 110f : 150f, unlocked ? UIKit.Cream : new Color(0.75f, 0.75f, 0.8f, 1f), new Vector2(0f, 110f), new Vector2(size.x - 20f, 170f), TextAlignmentOptions.Center);
        Label(card, "Name", level.displayName, 40f, UIKit.Cream, new Vector2(0f, -30f), new Vector2(size.x - 20f, 110f), TextAlignmentOptions.Center);

        string status;
        Color statusColor = UIKit.Gold;
        if (!unlocked)
        {
            status = "LOCKED";
            statusColor = new Color(0.75f, 0.75f, 0.8f, 1f);
        }
        else if (record != null)
        {
            status = "BEST " + record.bestScore + (record.bestTime > 0f ? "\n" + FormatTime(record.bestTime) : "");
        }
        else
        {
            status = "NEW";
        }
        Label(card, "Status", status, 40f, statusColor, new Vector2(0f, -140f), new Vector2(size.x - 20f, 110f), TextAlignmentOptions.Center);
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    static void CloseLevelSelect()
    {
        Close(ref levelSelectRoot);
    }

    // ================================================================ настройки

    public static void ShowSettings()
    {
        Close(ref levelSelectRoot);
        Close(ref settingsRoot);

        GameObject rootGo;
        RectTransform safe = CreateOverlay("SettingsCanvas", 90, out rootGo);
        settingsRoot = rootGo;

        RectTransform window = CreateWindow(safe, new Vector2(1100f, 940f));
        Label(window, "Title", "SETTINGS", 104f, UIKit.Gold, new Vector2(0f, 350f), new Vector2(900f, 130f), TextAlignmentOptions.Center);

        BuildVolumeRow(window, "MUSIC", 160f, true);
        BuildVolumeRow(window, "SOUND", -20f, false);

        MakeButton(window, "ResetButton", "RESET PROGRESS", "btn_dark", "btn_dark_down", true, new Vector2(0f, -190f), new Vector2(760f, 150f), 64f, ShowConfirmReset);
        MakeButton(window, "BackButton", "BACK", "btn_orange", "btn_orange_down", true, new Vector2(0f, -370f), new Vector2(520f, 130f), 72f, CloseSettings);
    }

    static void BuildVolumeRow(RectTransform window, string title, float y, bool music)
    {
        Label(window, title + "Label", title, 64f, UIKit.Cream, new Vector2(-340f, y), new Vector2(320f, 100f), TextAlignmentOptions.Left);
        TextMeshProUGUI value = Label(window, title + "Value", "", 64f, UIKit.Gold, new Vector2(100f, y), new Vector2(200f, 100f), TextAlignmentOptions.Center);
        MakeButton(window, title + "Minus", "-", "round_dark", "round_dark_down", false, new Vector2(-90f, y), new Vector2(130f, 130f), 90f, () => ChangeVolume(music, -1));
        MakeButton(window, title + "Plus", "+", "round_dark", "round_dark_down", false, new Vector2(290f, y), new Vector2(130f, 130f), 90f, () => ChangeVolume(music, 1));
        if (music) musicValue = value; else sfxValue = value;
        RefreshVolumeTexts();
    }

    static void ChangeVolume(bool music, int direction)
    {
        SaveData d = SaveSystem.Data;
        float step = GameData.Config.volumeStep;
        float m = d.musicVol;
        float s = d.sfxVol;
        if (music) m = Mathf.Clamp01(Mathf.Round((m + direction * step) * 100f) / 100f);
        else s = Mathf.Clamp01(Mathf.Round((s + direction * step) * 100f) / 100f);

        SaveSystem.SetVolumes(m, s);   // применяет громкость и СРАЗУ записывает save.json
        RefreshVolumeTexts();
    }

    static void RefreshVolumeTexts()
    {
        SaveData d = SaveSystem.Data;
        if (musicValue != null) musicValue.text = Mathf.RoundToInt(d.musicVol * 100f) + "%";
        if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(d.sfxVol * 100f) + "%";
    }

    static void CloseSettings()
    {
        Close(ref confirmRoot);
        Close(ref settingsRoot);
    }

    // ================================================================ сброс прогресса

    static void ShowConfirmReset()
    {
        Close(ref confirmRoot);

        GameObject rootGo;
        RectTransform safe = CreateOverlay("ConfirmResetCanvas", 110, out rootGo);
        confirmRoot = rootGo;

        RectTransform window = CreateWindow(safe, new Vector2(1000f, 640f));
        Label(window, "Title", "RESET PROGRESS?", 88f, UIKit.Gold, new Vector2(0f, 200f), new Vector2(900f, 120f), TextAlignmentOptions.Center);
        Label(window, "Text", "Best score and unlocked levels\nwill be erased.", 48f, UIKit.Cream, new Vector2(0f, 40f), new Vector2(900f, 160f), TextAlignmentOptions.Center);
        MakeButton(window, "YesButton", "YES, RESET", "btn_orange", "btn_orange_down", true, new Vector2(-245f, -190f), new Vector2(430f, 150f), 60f, DoReset);
        MakeButton(window, "NoButton", "NO", "btn_dark", "btn_dark_down", true, new Vector2(245f, -190f), new Vector2(430f, 150f), 72f, CloseConfirm);
    }

    static void CloseConfirm()
    {
        Close(ref confirmRoot);
    }

    static void DoReset()
    {
        SaveSystem.ResetProgress();   // новое чистое сохранение (громкость остаётся) и запись на диск
        CloseConfirm();
        CloseSettings();

        MobileUIRoot ui = Object.FindFirstObjectByType<MobileUIRoot>();
        if (ui != null)
        {
            Transform play = UIKit.FindDeep(ui.transform, "PlayButton");
            if (play != null) RefreshBestScoreLabel(play.parent);
        }
        ShowNotice("PROGRESS RESET", 2.5f);
    }

    // ================================================================ сообщение

    /// <summary>Короткая плашка внизу экрана; не блокирует нажатия и исчезает сама.</summary>
    public static void ShowNotice(string text, float seconds)
    {
        GameObject cgo = new GameObject("NoticeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        cgo.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform safe = UIKit.NewRect("SafeArea", cgo.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaPanel>();

        RectTransform plate = UIKit.NewRect("Plate", safe);
        UIKit.Place(plate, BC, BC, new Vector2(0f, 140f), new Vector2(1500f, 110f));
        UIKit.AddImage(plate, UIKit.Spr("panel"), Color.white, true, false);

        RectTransform label = UIKit.NewRect("Text", plate);
        UIKit.Stretch(label);
        UIKit.AddText(label, text, 46f, UIKit.Cream, TextAlignmentOptions.Center);

        Object.Destroy(cgo, seconds);
    }

    // ================================================================ общие куски

    static void Close(ref GameObject go)
    {
        if (go != null) UIKit.Kill(go);
        go = null;
    }

    /// <summary>Полноэкранный слой: затемнение + SafeArea. Как в WinScreen.</summary>
    static RectTransform CreateOverlay(string name, int sortingOrder, out GameObject rootGo)
    {
        UIKit.EnsureEventSystem();

        rootGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        rootGo.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform dim = UIKit.NewRect("Dim", rootGo.transform);
        UIKit.Stretch(dim);
        UIKit.AddImage(dim, UIKit.Spr("white"), UIKit.Dim, false, true);   // перехватывает нажатия под окном

        RectTransform safe = UIKit.NewRect("SafeArea", rootGo.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaPanel>();
        return safe;
    }

    static RectTransform CreateWindow(RectTransform parent, Vector2 size)
    {
        RectTransform window = UIKit.NewRect("Window", parent);
        UIKit.Place(window, C, C, Vector2.zero, size);
        UIKit.AddImage(window, UIKit.Spr("panel"), Color.white, true, true);
        return window;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float fontSize, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, C, C, pos, box);
        return UIKit.AddText(r, text, fontSize, color, align);
    }

    static Button MakeButton(Transform parent, string name, string label, string normal, string pressed, bool sliced, Vector2 pos, Vector2 size, float fontSize, System.Action onClick)
    {
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, C, C, pos, size);
        Button b = UIKit.AddButton(r, normal, pressed, sliced);
        b.onClick.AddListener(() =>
        {
            GameFlow.Click();
            if (onClick != null) onClick();
        });

        RectTransform t = UIKit.NewRect("Label", r);
        UIKit.Stretch(t);
        UIKit.AddText(t, label, fontSize, UIKit.Cream, TextAlignmentOptions.Center);
        return b;
    }
}

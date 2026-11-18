using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Строит мобильный интерфейс: Главное меню, HUD (сердца, счёт, пауза, стик, прыжок, удар) и окно паузы.
///
/// Правила, которые соблюдает каждый экран:
///  * Canvas Scaler = Scale With Screen Size, 1920 x 1080, Match = 1 (по высоте, игра в ландшафте);
///  * всё нажимаемое и читаемое лежит внутри панели SafeArea (вырезы / углы экрана);
///  * якоря привязаны к углам и краям, а не к абсолютным пикселям;
///  * все кнопки не меньше 120 x 120 единиц Canvas, текст не меньше ~36.
///
/// Код работает и в игре, и в редакторе (Samurai ▸ Bake Mobile UI Into Scenes), поэтому метод Build идемпотентен.
/// </summary>
public static class MobileUI
{
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f);
    static readonly Vector2 MR = new Vector2(1f, 0.5f);

    public static void Build(string sceneName)
    {
        MobileUIRoot existing = Object.FindFirstObjectByType<MobileUIRoot>();
        if (existing != null)
        {
            // интерфейс уже запечён в сцену: убираем подпись, которой больше нет в меню
            Transform oldCredits = UIKit.FindDeep(existing.transform, "Credits");
            if (oldCredits != null) UIKit.Kill(oldCredits.gameObject);
            return;
        }
        UIKit.EnsureEventSystem();

        if (sceneName == "MainMenu") BuildMenu();
        else if (sceneName == "Level1" || sceneName == "Level2" || sceneName == "Level3" || sceneName == "LevelBoss") BuildGameplay(sceneName);
    }

    // ------------------------------------------------------------------ общие куски

    static RectTransform CreateRoot(out GameObject rootGo)
    {
        rootGo = new GameObject("MobileUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileUIRoot));
        rootGo.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform safe = UIKit.NewRect("SafeArea", rootGo.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaPanel>();
        return safe;
    }

    /// <summary>Старый Canvas из сцены (постоянный размер 800x600) — из него забираем нужные тексты, остальное удаляем.</summary>
    static Canvas FindLegacyCanvas()
    {
        Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Canvas c = all[i];
            if (c.transform.parent == null && c.GetComponent<MobileUIRoot>() == null) return c;
        }
        return null;
    }

    /// <summary>Переносит существующий текст (со всеми его скриптами) в новый интерфейс.</summary>
    static TextMeshProUGUI Adopt(Canvas legacy, string objName, Transform newParent)
    {
        if (legacy == null) return null;
        Transform t = UIKit.FindDeep(legacy.transform, objName);
        if (t == null) return null;
        t.SetParent(newParent, false);
        return t.GetComponent<TextMeshProUGUI>();
    }

    static RectTransform MenuButton(Transform parent, string name, string label, string normal, string pressed, UIAction.Kind kind, Vector2 pos, Vector2 size, float fontSize)
    {
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, C, C, pos, size);
        UIKit.AddButton(r, normal, pressed, true);
        r.gameObject.AddComponent<UIAction>().kind = kind;

        RectTransform t = UIKit.NewRect("Label", r);
        UIKit.Stretch(t);
        UIKit.AddText(t, label, fontSize, UIKit.Cream, TextAlignmentOptions.Center);
        return r;
    }

    static RectTransform Plate(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float alpha)
    {
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, anchor, anchor, pos, size);
        UIKit.AddImage(r, UIKit.Spr("panel"), new Color(1f, 1f, 1f, alpha), true, false);
        return r;
    }

    // ------------------------------------------------------------------ ГЛАВНОЕ МЕНЮ

    static void BuildMenu()
    {
        Canvas legacy = FindLegacyCanvas();
        GameObject rootGo;
        RectTransform safe = CreateRoot(out rootGo);

        RectTransform title = UIKit.NewRect("Title", safe);
        UIKit.Place(title, TC, TC, new Vector2(0f, -30f), new Vector2(1100f, 220f));
        TextMeshProUGUI titleText = UIKit.AddText(title, "SAMURAI", 200f, UIKit.Gold, TextAlignmentOptions.Center);
        titleText.enableAutoSizing = true; // на узких экранах надпись сама уменьшается и не вылезает за край
        titleText.fontSizeMin = 60f;
        titleText.fontSizeMax = 200f;

        RectTransform sub = UIKit.NewRect("Subtitle", safe);
        UIKit.Place(sub, TC, TC, new Vector2(0f, -235f), new Vector2(1500f, 120f));
        UIKit.AddText(sub, "DEMON GATE", 100f, new Color32(255, 150, 60, 255), TextAlignmentOptions.Center);

        MenuButton(safe, "PlayButton", "PLAY", "btn_orange", "btn_orange_down", UIAction.Kind.Play, new Vector2(0f, -70f), new Vector2(640f, 160f), 88f);
        MenuButton(safe, "QuitButton", "QUIT", "btn_dark", "btn_dark_down", UIAction.Kind.Quit, new Vector2(0f, -270f), new Vector2(640f, 150f), 72f);

        if (legacy != null) UIKit.Kill(legacy.gameObject);
    }

    // ------------------------------------------------------------------ HUD + ПАУЗА

    static string LevelTitle(string scene)
    {
        if (scene == "Level1") return "LEVEL 1";
        if (scene == "Level2") return "LEVEL 2";
        if (scene == "Level3") return "LEVEL 3";
        return "BOSS FIGHT";
    }

    static void BuildGameplay(string sceneName)
    {
        Canvas legacy = FindLegacyCanvas();
        GameObject rootGo;
        RectTransform safe = CreateRoot(out rootGo);

        RectTransform hudRt = UIKit.NewRect("Hud", safe);
        UIKit.Stretch(hudRt);
        HudController hud = hudRt.gameObject.AddComponent<HudController>();

        // ---- сердца (слева сверху) ----
        RectTransform hearts = UIKit.NewRect("Hearts", hudRt);
        UIKit.Place(hearts, TL, TL, new Vector2(30f, -30f), new Vector2(330f, 110f));
        hud.hearts = hearts;
        hud.heartFull = UIKit.Spr("heart_full");
        hud.heartEmpty = UIKit.Spr("heart_empty");

        // ---- счёт (по центру сверху): иконка монеты + подпись + число ----
        RectTransform plate = Plate(hudRt, "ScorePlate", TC, new Vector2(0f, -30f), new Vector2(460f, 116f), 0.94f);

        RectTransform coin = UIKit.NewRect("CoinIcon", plate);
        UIKit.Place(coin, ML, ML, new Vector2(34f, 0f), new Vector2(60f, 60f));
        UIKit.AddImage(coin, UIKit.Spr("icon_coin"), Color.white, false, false);

        RectTransform label = UIKit.NewRect("ScoreLabel", plate);
        UIKit.Place(label, ML, ML, new Vector2(110f, 0f), new Vector2(170f, 70f));
        UIKit.AddText(label, "SCORE", 42f, UIKit.Gold, TextAlignmentOptions.Left);

        // значение: берём существующий CoinText (на нём живёт CoinManager)
        TextMeshProUGUI score = Adopt(legacy, "CoinText", plate);
        if (score == null)
        {
            RectTransform v = UIKit.NewRect("ScoreValue", plate);
            score = v.gameObject.AddComponent<TextMeshProUGUI>();
        }
        UIKit.Place(score.rectTransform, MR, MR, new Vector2(-34f, 0f), new Vector2(170f, 90f));
        UIKit.StyleText(score, "0", 72f, UIKit.Cream, TextAlignmentOptions.Right);
        hud.scoreValue = score;

        // ---- подпись уровня / слот для полоски босса (под счётом) ----
        RectTransform info = UIKit.NewRect("InfoSlot", hudRt);
        UIKit.Place(info, TC, TC, new Vector2(0f, -160f), new Vector2(900f, 80f));
        hud.infoSlot = info;

        RectTransform levelPlate = Plate(info, "LevelPlate", C, Vector2.zero, new Vector2(560f, 76f), 0.8f);
        RectTransform levelLabel = UIKit.NewRect("LevelLabel", levelPlate);
        UIKit.Stretch(levelLabel);
        hud.levelText = UIKit.AddText(levelLabel, LevelTitle(sceneName), 48f, UIKit.Cream, TextAlignmentOptions.Center);
        hud.levelPlate = levelPlate.gameObject;

        // ---- таймер выживания (только Level 2) ----
        RectTransform timerPlate = null;
        TextMeshProUGUI timer = null;
        if (legacy != null && UIKit.FindDeep(legacy.transform, "SurvivalCountdownText") != null)
        {
            timerPlate = Plate(hudRt, "TimerPlate", TC, new Vector2(0f, -250f), new Vector2(560f, 84f), 0.8f);
            timer = Adopt(legacy, "SurvivalCountdownText", timerPlate);
            if (timer != null)
            {
                UIKit.Stretch(timer.rectTransform);
                UIKit.StyleText(timer, "SURVIVE", 52f, UIKit.Gold, TextAlignmentOptions.Center);
                hud.timerPlate = timerPlate.gameObject;
                hud.timerText = timer.gameObject;
            }
        }

        // ---- пауза (справа сверху) ----
        RectTransform pr = UIKit.NewRect("PauseButton", hudRt);
        UIKit.Place(pr, TR, TR, new Vector2(-30f, -24f), new Vector2(160f, 160f));
        UIKit.AddButton(pr, "round_dark", "round_dark_down", false);
        pr.gameObject.AddComponent<UIAction>().kind = UIAction.Kind.Pause;
        RectTransform pi = UIKit.NewRect("Icon", pr);
        UIKit.Place(pi, C, C, Vector2.zero, new Vector2(72f, 80f));
        UIKit.AddImage(pi, UIKit.Spr("icon_pause"), Color.white, false, false);
        hud.pauseButton = pr.gameObject;

        // ---- управление (внизу): стик слева, прыжок и удар справа ----
        RectTransform controls = UIKit.NewRect("Controls", hudRt);
        UIKit.Stretch(controls);

        RectTransform stick = UIKit.NewRect("MoveStick", controls);
        UIKit.Place(stick, BL, BL, new Vector2(40f, 40f), new Vector2(360f, 360f));
        UIKit.AddImage(stick, UIKit.Spr("stick_base"), Color.white, false, true);
        RectTransform knob = UIKit.NewRect("Knob", stick);
        UIKit.Place(knob, C, C, Vector2.zero, new Vector2(160f, 160f));
        UIKit.AddImage(knob, UIKit.Spr("stick_knob"), Color.white, false, false);
        TouchStick ts = stick.gameObject.AddComponent<TouchStick>();
        ts.knob = knob;
        ts.radius = 100f;

        RectTransform jump = UIKit.NewRect("JumpButton", controls);
        UIKit.Place(jump, BR, BR, new Vector2(-40f, 50f), new Vector2(208f, 208f));
        UIKit.AddImage(jump, UIKit.Spr("round_orange"), Color.white, false, true);
        TouchButton tj = jump.gameObject.AddComponent<TouchButton>();
        tj.kind = TouchButton.Kind.Jump;
        tj.normal = UIKit.Spr("round_orange");
        tj.pressed = UIKit.Spr("round_orange_down");
        RectTransform ji = UIKit.NewRect("Icon", jump);
        UIKit.Place(ji, C, C, new Vector2(0f, 4f), new Vector2(88f, 88f));
        UIKit.AddImage(ji, UIKit.Spr("icon_jump"), Color.white, false, false);

        RectTransform attack = UIKit.NewRect("AttackButton", controls);
        UIKit.Place(attack, BR, BR, new Vector2(-300f, 100f), new Vector2(176f, 176f));
        UIKit.AddImage(attack, UIKit.Spr("round_red"), Color.white, false, true);
        TouchButton ta = attack.gameObject.AddComponent<TouchButton>();
        ta.kind = TouchButton.Kind.Attack;
        ta.normal = UIKit.Spr("round_red");
        ta.pressed = UIKit.Spr("round_red_down");
        RectTransform ai = UIKit.NewRect("Icon", attack);
        UIKit.Place(ai, C, C, new Vector2(0f, 4f), new Vector2(96f, 96f));
        UIKit.AddImage(ai, UIKit.Spr("icon_sword"), Color.white, false, false);

        // ---- результат: поражение / победа (существующие тексты сцены) ----
        TextMeshProUGUI lose = Adopt(legacy, "Lose text", hudRt);
        if (lose != null)
        {
            UIKit.Place(lose.rectTransform, C, C, new Vector2(0f, 60f), new Vector2(1500f, 420f));
            UIKit.StyleText(lose, "DEFEATED\n<size=40%>Restarting...</size>", 190f, new Color32(240, 80, 80, 255), TextAlignmentOptions.Center);
        }
        TextMeshProUGUI win = Adopt(legacy, "WinText", hudRt);
        if (win != null)
        {
            UIKit.Place(win.rectTransform, C, C, new Vector2(0f, 60f), new Vector2(1500f, 300f));
            UIKit.StyleText(win, "YOU WIN!", 190f, UIKit.Gold, TextAlignmentOptions.Center);
        }

        // ---- окно паузы (поверх всего, выключено до нажатия на паузу) ----
        RectTransform pause = UIKit.NewRect("PauseOverlay", rootGo.transform);
        UIKit.Stretch(pause);

        RectTransform dim = UIKit.NewRect("Dim", pause);
        UIKit.Stretch(dim);
        UIKit.AddImage(dim, UIKit.Spr("white"), UIKit.Dim, false, true); // фон на весь экран, перехватывает касания

        RectTransform pauseSafe = UIKit.NewRect("SafeArea", pause);
        UIKit.Stretch(pauseSafe);
        pauseSafe.gameObject.AddComponent<SafeAreaPanel>();

        RectTransform window = UIKit.NewRect("Window", pauseSafe);
        UIKit.Place(window, C, C, Vector2.zero, new Vector2(780f, 900f));
        UIKit.AddImage(window, UIKit.Spr("panel"), Color.white, true, true);

        RectTransform ptitle = UIKit.NewRect("Title", window);
        UIKit.Place(ptitle, C, C, new Vector2(0f, 330f), new Vector2(700f, 130f));
        UIKit.AddText(ptitle, "PAUSED", 104f, UIKit.Gold, TextAlignmentOptions.Center);

        Vector2 bs = new Vector2(580f, 150f);
        MenuButton(window, "ResumeButton", "RESUME", "btn_orange", "btn_orange_down", UIAction.Kind.Resume, new Vector2(0f, 120f), bs, 72f);
        MenuButton(window, "RestartButton", "RESTART", "btn_dark", "btn_dark_down", UIAction.Kind.Restart, new Vector2(0f, -70f), bs, 72f);
        MenuButton(window, "MenuButton", "MAIN MENU", "btn_dark", "btn_dark_down", UIAction.Kind.Menu, new Vector2(0f, -260f), bs, 72f);

        hud.pauseRoot = pause.gameObject;
        pause.gameObject.SetActive(false);

        // старый Canvas (постоянный размер, кнопки на 800x600) больше не нужен
        if (legacy != null) UIKit.Kill(legacy.gameObject);
    }
}

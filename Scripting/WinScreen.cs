using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Экран победы после босса: «YOU WIN» + счёт + кнопки «Играть снова» и «В меню».</summary>
public static class WinScreen
{
    public static void Show()
    {
        UIKit.EnsureEventSystem();

        GameObject cgo = new GameObject("WinCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        // затемнение на весь экран
        RectTransform dim = UIKit.NewRect("Dim", cgo.transform);
        UIKit.Stretch(dim);
        UIKit.AddImage(dim, UIKit.Spr("white"), UIKit.Dim, false, true);

        // всё остальное — внутри SafeArea
        RectTransform safe = UIKit.NewRect("SafeArea", cgo.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaPanel>();

        RectTransform window = UIKit.NewRect("Window", safe);
        UIKit.Place(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 800f));
        UIKit.AddImage(window, UIKit.Spr("panel"), Color.white, true, true);

        RectTransform title = UIKit.NewRect("Title", window);
        UIKit.Place(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1000f, 220f));
        UIKit.AddText(title, "YOU WIN!", 190f, UIKit.Gold, TextAlignmentOptions.Center);

        int coins = CoinManager.instance != null ? CoinManager.instance.coinCount : 0;
        RectTransform sub = UIKit.NewRect("Sub", window);
        UIKit.Place(sub, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 100f));
        UIKit.AddText(sub, "The demon is defeated!   SCORE: " + coins, 56f, UIKit.Cream, TextAlignmentOptions.Center);

        MakeButton(window, "PlayAgainButton", "PLAY AGAIN", "btn_orange", "btn_orange_down", UIAction.Kind.Play, new Vector2(0f, -70f));
        MakeButton(window, "MenuButton", "MAIN MENU", "btn_dark", "btn_dark_down", UIAction.Kind.Menu, new Vector2(0f, -250f));
    }

    static void MakeButton(Transform parent, string name, string label, string normal, string pressed, UIAction.Kind kind, Vector2 pos)
    {
        RectTransform r = UIKit.NewRect(name, parent);
        UIKit.Place(r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(640f, 150f));
        UIKit.AddButton(r, normal, pressed, true);
        r.gameObject.AddComponent<UIAction>().kind = kind;

        RectTransform t = UIKit.NewRect("Label", r);
        UIKit.Stretch(t);
        UIKit.AddText(t, label, 72f, UIKit.Cream, TextAlignmentOptions.Center);
    }
}

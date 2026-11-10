using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматическая подготовка сцен при запуске (без ручной настройки в редакторе):
///  * музыка для меню / уровней / босса;
///  * мобильный интерфейс (меню, HUD, пауза) через MobileUI — если он не запечён в сцену;
///  * оформление мира: небо, горы, земля и декор из Village Props (WorldStyler);
///  * на сцене LevelBoss вешает скрипт Boss на демона;
///  * ландшафтная ориентация на телефоне.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameFlow.OnSceneLoaded();
        switch (scene.name)
        {
            case "MainMenu":
                GameAudio.PlayMusic("music_menu");
                WorldStyler.StyleMenu();
                MobileUI.Build(scene.name);
                break;
            case "Level1":
            case "Level2":
            case "Level3":
                GameAudio.PlayMusic("music_level");
                WorldStyler.StyleLevel(scene.name);
                MobileUI.Build(scene.name);
                break;
            case "LevelBoss":
                GameAudio.PlayMusic("music_boss");
                SetupBoss();
                WorldStyler.StyleLevel(scene.name);
                MobileUI.Build(scene.name);
                break;
        }
    }

    static void SetupBoss()
    {
        // демон уже стоит на сцене как спрайт — превращаем его в босса
        GameObject demon = GameObject.Find("demon_idle_1_0");
        if (demon == null)
        {
            foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (r.sprite != null && r.sprite.name.StartsWith("demon")) { demon = r.gameObject; break; }
            }
        }
        if (demon == null)
        {
            Debug.LogError("GameBootstrap: демон не найден на сцене LevelBoss");
            return;
        }
        if (demon.GetComponent<Boss>() == null) demon.AddComponent<Boss>();

        // лишний статичный спрайт демона в небе и портал на сцене босса не нужны
        GameObject extra = GameObject.Find("01_d_idle_0");
        if (extra != null) extra.SetActive(false);
        GameObject portal = GameObject.FindWithTag("Portal");
        if (portal != null) portal.SetActive(false);
    }
}

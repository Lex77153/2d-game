using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматическая подготовка сцен при запуске (без ручной настройки в редакторе):
///  * музыка для меню / уровней / босса;
///  * мобильный интерфейс (меню, HUD, пауза) через MobileUI — если он не запечён в сцену;
///  * оформление мира: небо, горы, земля и декор из Village Props (WorldStyler);
///  * на сцене с целью DefeatBoss вешает скрипт Boss на демона;
///  * для каждой игровой сцены запускает LevelRunner, который строит уровень из LevelData;
///  * ландшафтная ориентация на телефоне.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Application.targetFrameRate = GameData.Config.targetFrameRate;
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

        if (scene.name == "MainMenu")
        {
            GameAudio.PlayMusic("music_menu");
            WorldStyler.StyleMenu();
            MobileUI.Build(scene.name);
            MenuScreens.InstallMainMenu();   // выбор уровня, настройки, лучший счёт (поверх запечённого меню)
            return;
        }

        // игровая сцена? тогда для неё должен быть LevelData (Resources/Data/Levels)
        LevelData level = GameData.LevelForScene(scene.name);
        if (level == null) return;

        GameAudio.PlayMusic(level.musicTrack);
        if (level.goal == LevelGoal.DefeatBoss) SetupBoss();
        WorldStyler.StyleLevel(scene.name);
        MobileUI.Build(scene.name);
        LevelRunner.Begin(scene.name);       // враги, предметы, платформы, декор, цель — всё из LevelData
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

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Поток игры: старт, пауза, продолжить, рестарт, выход в меню.
/// Пауза = Time.timeScale 0 (физика, анимации и таймеры замирают, а UI продолжает работать).
/// </summary>
public static class GameFlow
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> PausedChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsPaused = false;
        PausedChanged = null;
    }

    /// <summary>Вызывается при загрузке каждой сцены: сбрасываем паузу и ввод.</summary>
    public static void OnSceneLoaded()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        TouchInput.Reset();
        GameAudio.DuckMusic(false);
    }

    public static void Click() { GameAudio.Play("click", 0.7f); }

    public static void Play()
    {
        Click();
        CoinManager.ResetRun();
        LoadScene("Level1");
    }

    public static void Quit()
    {
        Click();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public static void Pause()
    {
        if (IsPaused) return;
        SetPaused(true);
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        SetPaused(false);
    }

    public static void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public static void Restart()
    {
        Click();
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void ToMenu()
    {
        Click();
        CoinManager.ResetRun();
        LoadScene("MainMenu");
    }

    static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        TouchInput.Reset();
        GameAudio.DuckMusic(paused);
        if (PausedChanged != null) PausedChanged(paused);
    }

    static void LoadScene(string sceneName)
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}

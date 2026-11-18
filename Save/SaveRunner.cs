using UnityEngine;

/// <summary>
/// Невидимый объект, который живёт всю игру и сохраняет прогресс в нужные моменты:
/// при сворачивании приложения (OnApplicationPause(true)) — на телефоне это главный момент,
/// потому что систему не спросишь «а вы нас закрыть собираетесь?» — при потере фокуса и при выходе.
/// </summary>
public class SaveRunner : MonoBehaviour
{
    static SaveRunner instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        SaveSystem.EnsureLoaded();   // читаем save.json ДО первой сцены (или берём значения по умолчанию)

        GameObject go = new GameObject("SaveRunner");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SaveRunner>();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveSystem.Save();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveSystem.Save();
    }

    void OnApplicationQuit()
    {
        SaveSystem.Save();
    }
}

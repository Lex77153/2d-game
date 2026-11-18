using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Сохранение прогресса в JSON-файл Application.persistentDataPath/save.json.
///  * Load() НИКОГДА не бросает исключений: нет файла / файл повреждён -> значения по умолчанию;
///  * повреждённый файл копируется в save.json.broken, чтобы его можно было посмотреть;
///  * Save() пишет во временный файл и подменяет им save.json, поэтому обрыв записи не ломает старое сохранение.
/// Когда сохраняем: после уровня (CompleteLevel), при смене настроек (SetVolumes),
/// при сбросе (ResetProgress) и в SaveRunner.OnApplicationPause(true).
/// </summary>
public static class SaveSystem
{
    static SaveData data;

    /// <summary>Сообщение для игрока, если при запуске сохранение пришлось заменить (null — всё хорошо).</summary>
    public static string LastWarning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        data = null;
        LastWarning = null;
    }

    /// <summary>Игрок уже увидел сообщение о повреждённом сохранении — больше не показываем.</summary>
    public static void ClearWarning()
    {
        LastWarning = null;
    }

    public static string FilePath
    {
        get
        {
            string name = GameData.Config.saveFileName;
            if (string.IsNullOrEmpty(name)) name = "save.json";
            return Path.Combine(Application.persistentDataPath, name);
        }
    }

    /// <summary>Текущее сохранение в памяти (при первом обращении читается с диска).</summary>
    public static SaveData Data
    {
        get
        {
            if (data == null) data = Load();
            return data;
        }
    }

    public static void EnsureLoaded()
    {
        if (data == null) data = Load();
    }

    // ------------------------------------------------------------ загрузка

    public static SaveData Load()
    {
        string path = FilePath;
        SaveData loaded = null;

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null) throw new InvalidDataException("файл сохранения пуст");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveSystem: не удалось прочитать " + path + " (" + e.Message + "). Берём значения по умолчанию.");
            LastWarning = "SAVE FILE WAS DAMAGED - STARTING FRESH";
            KeepBrokenCopy(path);
            loaded = null;
        }

        if (loaded == null) loaded = SaveData.CreateDefault();

        if (loaded.Sanitize())
        {
            Debug.Log("SaveSystem: в сохранении были неверные значения, они исправлены.");
        }
        return loaded;
    }

    static void KeepBrokenCopy(string path)
    {
        try
        {
            if (File.Exists(path)) File.Copy(path, path + ".broken", true);
        }
        catch (Exception)
        {
            // копия нужна только для отладки — если не вышло, не страшно
        }
    }

    // ------------------------------------------------------------ запись

    public static bool Save()
    {
        if (data == null) return false;   // ещё ничего не загружали — нечего и записывать
        try
        {
            string path = FilePath;
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveSystem: не удалось сохранить (" + e.Message + ").");
            return false;
        }
    }

    // ------------------------------------------------------------ прогресс

    public static bool IsUnlocked(int levelNumber)
    {
        return Data.unlocked.Contains(levelNumber);
    }

    public static LevelRecord GetRecord(int levelNumber)
    {
        SaveData d = Data;
        for (int i = 0; i < d.records.Count; i++) if (d.records[i].levelNumber == levelNumber) return d.records[i];
        return null;
    }

    /// <summary>Уровень пройден: рекорды, открытие следующего уровня и запись на диск.</summary>
    public static void CompleteLevel(int levelNumber, int levelScore, int runScore, float seconds)
    {
        SaveData d = Data;

        LevelRecord r = GetRecord(levelNumber);
        if (r == null)
        {
            r = new LevelRecord();
            r.levelNumber = levelNumber;
            d.records.Add(r);
        }
        if (levelScore > r.bestScore) r.bestScore = levelScore;
        if (seconds > 0f && (r.bestTime <= 0f || seconds < r.bestTime)) r.bestTime = seconds;

        if (runScore > d.bestScore) d.bestScore = runScore;

        int next = levelNumber + 1;
        if (next <= GameData.MaxLevelNumber && !d.unlocked.Contains(next)) d.unlocked.Add(next);

        Save();
    }

    /// <summary>Сброс прогресса (рекорды и открытые уровни). Громкость остаётся.</summary>
    public static void ResetProgress()
    {
        SaveData old = Data;
        SaveData fresh = SaveData.CreateDefault();
        fresh.musicVol = old.musicVol;
        fresh.sfxVol = old.sfxVol;
        data = fresh;
        Save();
    }

    // ------------------------------------------------------------ настройки

    public static void SetVolumes(float music, float sfx)
    {
        SaveData d = Data;
        d.musicVol = Mathf.Clamp01(music);
        d.sfxVol = Mathf.Clamp01(sfx);
        GameAudio.ApplyVolumes();
        Save();
    }
}

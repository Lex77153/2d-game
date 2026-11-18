using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Рекорд на одном уровне.</summary>
[Serializable]
public class LevelRecord
{
    public int levelNumber;
    public int bestScore;
    public float bestTime;
}

/// <summary>
/// То, что лежит в save.json. Только простые поля, List и вложенные [Serializable] классы —
/// так JsonUtility умеет их сохранять (Dictionary он пропускает, поэтому «unlocked» — это List номеров).
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int bestScore;
    public List<int> unlocked = new List<int>();
    public List<LevelRecord> records = new List<LevelRecord>();
    public float musicVol = 0.8f;
    public float sfxVol = 1f;

    /// <summary>Чистое сохранение: открыт только первый уровень, громкость из GameConfig.</summary>
    public static SaveData CreateDefault()
    {
        SaveData d = new SaveData();
        d.musicVol = GameData.Config.defaultMusicVolume;
        d.sfxVol = GameData.Config.defaultSfxVolume;
        d.unlocked.Add(1);
        return d;
    }

    /// <summary>
    /// Чинит всё, что могло испортиться (файл правили руками, старая версия, мусор в числах).
    /// Возвращает true, если что-то пришлось исправить.
    /// </summary>
    public bool Sanitize()
    {
        bool changed = false;

        if (version != CurrentVersion)
        {
            // здесь была бы миграция со старых версий; пока версия одна
            version = CurrentVersion;
            changed = true;
        }

        if (bestScore < 0) { bestScore = 0; changed = true; }

        if (float.IsNaN(musicVol) || float.IsInfinity(musicVol)) { musicVol = GameData.Config.defaultMusicVolume; changed = true; }
        if (float.IsNaN(sfxVol) || float.IsInfinity(sfxVol)) { sfxVol = GameData.Config.defaultSfxVolume; changed = true; }
        float m = Mathf.Clamp01(musicVol);
        float s = Mathf.Clamp01(sfxVol);
        if (m != musicVol || s != sfxVol) { musicVol = m; sfxVol = s; changed = true; }

        if (unlocked == null) { unlocked = new List<int>(); changed = true; }
        int max = GameData.MaxLevelNumber;
        List<int> clean = new List<int>();
        for (int i = 0; i < unlocked.Count; i++)
        {
            int n = unlocked[i];
            if (n >= 1 && n <= max && !clean.Contains(n)) clean.Add(n);
        }
        if (!clean.Contains(1)) clean.Add(1);   // первый уровень всегда открыт
        clean.Sort();
        if (clean.Count != unlocked.Count) changed = true;
        unlocked = clean;

        if (records == null) { records = new List<LevelRecord>(); changed = true; }
        List<LevelRecord> cleanRecords = new List<LevelRecord>();
        for (int i = 0; i < records.Count; i++)
        {
            LevelRecord r = records[i];
            if (r == null || r.levelNumber < 1 || r.levelNumber > max) { changed = true; continue; }
            bool duplicate = false;
            for (int k = 0; k < cleanRecords.Count; k++) if (cleanRecords[k].levelNumber == r.levelNumber) duplicate = true;
            if (duplicate) { changed = true; continue; }
            if (r.bestScore < 0) { r.bestScore = 0; changed = true; }
            if (float.IsNaN(r.bestTime) || float.IsInfinity(r.bestTime) || r.bestTime < 0f) { r.bestTime = 0f; changed = true; }
            cleanRecords.Add(r);
        }
        records = cleanRecords;

        return changed;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Единая точка доступа к data-ассетам (ScriptableObject) из Resources/Data.
/// Если ассета нет (удалили, не импортировался) — создаётся объект со значениями по умолчанию
/// из самого класса, поэтому игра не падает, а только пишет предупреждение в Console.
/// Ассеты правятся прямо в Inspector — в том числе во время игры (Play Mode).
/// </summary>
public static class GameData
{
    const string Root = "Data/";

    static PlayerStats player;
    static ItemStats items;
    static BossStats boss;
    static GameConfig config;
    static readonly EnemyStats[] enemies = new EnemyStats[3];
    static List<LevelData> levels;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        player = null;
        items = null;
        boss = null;
        config = null;
        enemies[0] = null;
        enemies[1] = null;
        enemies[2] = null;
        levels = null;
    }

    public static PlayerStats Player
    {
        get { if (player == null) player = LoadOrDefault<PlayerStats>(Root + "PlayerStats"); return player; }
    }

    public static ItemStats Items
    {
        get { if (items == null) items = LoadOrDefault<ItemStats>(Root + "ItemStats"); return items; }
    }

    public static BossStats Boss
    {
        get { if (boss == null) boss = LoadOrDefault<BossStats>(Root + "BossStats"); return boss; }
    }

    public static GameConfig Config
    {
        get { if (config == null) config = LoadOrDefault<GameConfig>(Root + "GameConfig"); return config; }
    }

    public static EnemyStats Enemy(EnemyKind kind)
    {
        int i = (int)kind;
        if (i < 0 || i >= enemies.Length) i = 0;
        if (enemies[i] == null) enemies[i] = LoadOrDefault<EnemyStats>(Root + "Enemies/" + ((EnemyKind)i).ToString());
        return enemies[i];
    }

    /// <summary>Все уровни, отсортированные по номеру.</summary>
    public static List<LevelData> Levels
    {
        get
        {
            if (levels == null) levels = LoadLevels();
            return levels;
        }
    }

    public static int MaxLevelNumber
    {
        get
        {
            int max = 1;
            List<LevelData> all = Levels;
            for (int i = 0; i < all.Count; i++) if (all[i].levelNumber > max) max = all[i].levelNumber;
            return max;
        }
    }

    public static LevelData LevelByNumber(int number)
    {
        List<LevelData> all = Levels;
        for (int i = 0; i < all.Count; i++) if (all[i].levelNumber == number) return all[i];
        return null;
    }

    public static LevelData LevelForScene(string sceneName)
    {
        List<LevelData> all = Levels;
        for (int i = 0; i < all.Count; i++) if (all[i].sceneName == sceneName) return all[i];
        return null;
    }

    // ------------------------------------------------------------------

    static T LoadOrDefault<T>(string path) where T : ScriptableObject
    {
        T asset = Resources.Load<T>(path);
        if (asset == null)
        {
            Debug.LogWarning("GameData: нет ассета Resources/" + path + " — используются значения по умолчанию.");
            asset = ScriptableObject.CreateInstance<T>();
        }
        return asset;
    }

    static List<LevelData> LoadLevels()
    {
        List<LevelData> list = new List<LevelData>();
        LevelData[] loaded = Resources.LoadAll<LevelData>(Root + "Levels");
        for (int i = 0; i < loaded.Length; i++) if (loaded[i] != null) list.Add(loaded[i]);

        if (list.Count == 0)
        {
            // запасной вариант: сцены без расстановки, но игра запускается
            Debug.LogWarning("GameData: в Resources/Data/Levels нет LevelData — созданы уровни по умолчанию.");
            list.Add(MakeFallback(1, "Level1", "LEVEL 1", LevelGoal.KillAllEnemies, "Level2"));
            list.Add(MakeFallback(2, "Level2", "LEVEL 2", LevelGoal.SurviveTime, "Level3"));
            list.Add(MakeFallback(3, "Level3", "LEVEL 3", LevelGoal.KillAllEnemies, "LevelBoss"));
            list.Add(MakeFallback(4, "LevelBoss", "BOSS FIGHT", LevelGoal.DefeatBoss, ""));
        }

        list.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
        return list;
    }

    static LevelData MakeFallback(int number, string scene, string title, LevelGoal goal, string next)
    {
        LevelData d = ScriptableObject.CreateInstance<LevelData>();
        d.levelNumber = number;
        d.sceneName = scene;
        d.displayName = title;
        d.goal = goal;
        d.nextSceneName = next;
        d.replaceSceneEnemies = false;   // без данных оставляем врагов из сцены
        d.bombsEnabled = goal == LevelGoal.SurviveTime;
        d.musicTrack = goal == LevelGoal.DefeatBoss ? "music_boss" : "music_level";
        return d;
    }
}

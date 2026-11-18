using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Условие победы на уровне.</summary>
public enum LevelGoal
{
    KillAllEnemies = 0,   // портал открывается, когда побеждены все враги
    SurviveTime = 1,      // портал открывается, когда истёк таймер выживания
    DefeatBoss = 2        // победа — когда побеждён босс
}

public enum PickupKind { Coin = 0, Heart = 1 }

/// <summary>Фоновые частицы (светлячки, угольки, листья).</summary>
public enum AmbientKind { None = 0, Fireflies = 1, Embers = 2, Leaves = 3 }

[Serializable]
public class EnemySpawn
{
    public EnemyKind kind;
    public float x;
    public float y;
}

[Serializable]
public class PickupSpawn
{
    public PickupKind kind;
    public float x;
    public float y;
}

[Serializable]
public class PlatformSpawn
{
    [Tooltip("Центр по X")]
    public float x;
    [Tooltip("Высота ВЕРХНЕЙ грани (по ней ходит герой)")]
    public float top;
    public float width = 3f;
    public float thickness = 0.4f;
}

[Serializable]
public class PropSpawn
{
    [Tooltip("Имя предмета из VillageArt (tree_big, lamp_pole, banner, statue...)")]
    public string prop;
    public float x;
    [Tooltip("Точка на земле под предметом")]
    public float y;
    public int order = -2;
}

[Serializable]
public class SignSpawn
{
    public string text;
    public float x;
    public float y;
}

/// <summary>
/// Один уровень целиком: цель, время, враги, предметы, платформы, декор.
/// Один и тот же код (LevelRunner) загружает любой LevelData поверх своей сцены.
/// Файлы: Resources/Data/Levels/Level_01.asset, Level_02.asset, ...
/// </summary>
[CreateAssetMenu(fileName = "Level_01", menuName = "Samurai/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Кто это")]
    [Tooltip("Номер уровня: по нему уровень записывается в save.json")]
    public int levelNumber = 1;
    public string sceneName = "Level1";
    public string displayName = "LEVEL";
    [Tooltip("Надпись в начале уровня (пусто — без надписи)")]
    public string introText = "";
    [Tooltip("Куда ведёт портал (пусто — оставить как в сцене)")]
    public string nextSceneName = "";
    public string musicTrack = "music_level";

    [Header("Цель и время")]
    public LevelGoal goal = LevelGoal.KillAllEnemies;
    [Tooltip("Для SurviveTime: сколько секунд надо продержаться")]
    public float surviveSeconds = 40f;
    [Tooltip("Эталонное время, с (0 = без бонуса за скорость)")]
    public float parTimeSeconds = 0f;
    public int timeBonusPerSecond = 1;
    public int maxTimeBonus = 30;

    [Header("Герой")]
    public bool overridePlayerStart = false;
    public Vector2 playerStart;

    [Header("Враги")]
    [Tooltip("Убрать врагов, расставленных в сцене вручную, и создать врагов из списка")]
    public bool replaceSceneEnemies = true;
    public List<EnemySpawn> enemies = new List<EnemySpawn>();

    [Header("Бомбы (для уровней на выживание)")]
    public bool bombsEnabled = false;
    public float bombStartDelay = 1f;
    public float bombInterval = 0.5f;
    public float bombMinX = -10f;
    public float bombMaxX = 12f;
    public float bombSpawnY = 8f;

    [Header("Объекты сцены")]
    [Tooltip("Скорость движущихся платформ (0 — оставить как в сцене)")]
    public float movingPlatformSpeed = 0f;
    public List<PlatformSpawn> platforms = new List<PlatformSpawn>();
    public List<PickupSpawn> pickups = new List<PickupSpawn>();

    [Header("Детали и атмосфера")]
    public List<PropSpawn> props = new List<PropSpawn>();
    public List<SignSpawn> signs = new List<SignSpawn>();
    public AmbientKind ambient = AmbientKind.None;
    public int ambientCount = 30;
}

using UnityEngine;

/// <summary>
/// Метка на парящих платформах, которые LevelRunner создал из LevelData.
/// По ней босс отличает такие уступы от настоящей земли арены, когда ищет, на чём стоять
/// (иначе он мог бы «встать» на уступ, если тот оказался над его стартовой точкой).
/// </summary>
public class LevelDataPlatform : MonoBehaviour { }

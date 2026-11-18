using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вешается на Button и говорит, что она делает. Подписка на onClick идёт в Awake,
/// поэтому в сцене не нужны сохранённые (persistent) вызовы — меньше ломается при правках.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIAction : MonoBehaviour
{
    // новые значения — только в конец списка: числа уже записаны в сценах
    public enum Kind { Play, Quit, Pause, Resume, Restart, Menu, LevelSelect, Settings }

    public Kind kind;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Run);
    }

    void Run()
    {
        switch (kind)
        {
            case Kind.Play: GameFlow.Play(); break;
            case Kind.Quit: GameFlow.Quit(); break;
            case Kind.Pause: GameFlow.Click(); GameFlow.Pause(); break;
            case Kind.Resume: GameFlow.Click(); GameFlow.Resume(); break;
            case Kind.Restart: GameFlow.Restart(); break;
            case Kind.Menu: GameFlow.ToMenu(); break;
            case Kind.LevelSelect: GameFlow.Click(); MenuScreens.ShowLevelSelect(); break;
            case Kind.Settings: GameFlow.Click(); MenuScreens.ShowSettings(); break;
        }
    }
}

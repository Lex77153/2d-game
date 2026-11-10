using UnityEngine;

/// <summary>
/// Старые методы для кнопок главного меню. Новое меню (MobileUI) использует UIAction + GameFlow,
/// но этот класс оставлен, чтобы сцена и старые ссылки не ломались.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public void PlayGame() { GameFlow.Play(); }
    public void QuitGame() { GameFlow.Quit(); }
}

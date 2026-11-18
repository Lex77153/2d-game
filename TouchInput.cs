using UnityEngine;

/// <summary>
/// Мост между экранными кнопками и героем: стик пишет сюда направление,
/// кнопки «Прыжок» / «Удар» ставят флаг, PlayerController их забирает.
/// </summary>
public static class TouchInput
{
    public static Vector2 Move;
    static bool jump;
    static bool attack;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Reset(); }

    public static void QueueJump() { jump = true; }
    public static void QueueAttack() { attack = true; }

    public static bool ConsumeJump()
    {
        bool r = jump;
        jump = false;
        return r;
    }

    public static bool ConsumeAttack()
    {
        bool r = attack;
        attack = false;
        return r;
    }

    public static void Reset()
    {
        Move = Vector2.zero;
        jump = false;
        attack = false;
    }
}

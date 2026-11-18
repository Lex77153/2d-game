using UnityEngine;

public class Killzone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что в яму упал именно игрок
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // Вызываем метод смерти игрока из PlayerController
                player.Die();
            }
        }
    }
}
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Звук")]
    public AudioClip coinSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (CoinManager.instance != null)
            {
                CoinManager.instance.AddCoin(1);
            }

            // Проигрываем звук в точке монетки перед её уничтожением
            GameAudio.Play(coinSound, "stack-of-coins", 1f);

            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class UnderCoin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.AddCoin(1);
            GameManager.Instance.AddScore(200);

            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinSound);
            
            // 즉시 파괴
            Destroy(gameObject);
        }
    }
}
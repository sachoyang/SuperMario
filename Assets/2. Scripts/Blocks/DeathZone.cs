using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어(마리오)가 구덩이에 빠졌을 때
        if (collision.CompareTag("Player"))
        {
            MarioCtrl mario = collision.GetComponent<MarioCtrl>();
            // 이미 죽은 상태가 아니라면 Die() 함수 호출
            if (mario != null && !mario.isDead) 
            {
                mario.Die();
            }
        }
    }
}
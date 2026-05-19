using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float bounceForce = 7f; // 통통 튀어 오르는 힘
    private int direction = 1;
    private Rigidbody2D rb;
    private bool isSpawned = false;

    void Awake() 
    { 
        rb = GetComponent<Rigidbody2D>();
    }

    public void StartMoving(int startDir)
    {
        direction = startDir;
        isSpawned = true;
        rb.simulated = true;
        
        // 블록에서 다 올라오자마자 살짝 위로 한 번 튀게 만듭니다.
        rb.velocity = new Vector2(direction * moveSpeed, bounceForce * 0.5f);
    }

    void FixedUpdate()
    {
        if (!isSpawned) return;
        // x축은 계속 이동, y축은 현재 물리 속도(중력 및 점프) 유지
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 플레이어와 닿았을 때
        if (collision.gameObject.CompareTag("Player"))
        {
            MarioCtrl player = collision.gameObject.GetComponent<MarioCtrl>();
            if (player != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.powerUpSound);
                GameManager.Instance.AddScore(1000, transform.position + Vector3.up * 1.0f);
                player.TakeItem("Star"); // 무적 상태 전달
            }
            Destroy(gameObject);
            return;
        }

        // 2. 바닥이나 벽에 닿았을 때 물리 처리
        bool hitGround = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 위를 향하는 면(바닥)에 닿았는지 체크 (법선 벡터의 y값이 1에 가까움)
            if (contact.normal.y > 0.7f) 
            {
                hitGround = true;
            }
            // 옆면(벽)에 부딪혔을 때 방향 전환 (버섯과 동일)
            else if (Mathf.Abs(contact.normal.x) > 0.7f)
            {
                direction *= -1;
            }
        }

        // 바닥에 닿았다면 다시 위로 튕겨 오르기
        if (hitGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, bounceForce);
        }
    }

    // 블록 밑에서 쳤을 때 튕겨오르며 방향 전환하는 함수
    public void BounceAndChangeDirection()
    {
        if (!isSpawned) return;
        
        direction *= -1; // 방향 반전
        rb.velocity = new Vector2(rb.velocity.x, bounceForce); // 별 고유의 탄성으로 튕김
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomAI : MonoBehaviour
{
    public float moveSpeed = 2f;      // 버섯 이동 속도
    private int direction = 1;         // 1: 오른쪽, -1: 왼쪽
    private Rigidbody2D rb;
    private bool isSpawned = false;    // 블록에서 나오는 중인지 체크

    [Header("Item Settings")]
    public bool isOneUp = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 블록에서 위로 슥 올라오는 연출이 끝난 뒤 호출할 함수
    public void StartMoving(int startDir)
    {
        direction = startDir;
        isSpawned = true;
        rb.simulated = true; // 물리 시뮬레이션 시작
    }

    void FixedUpdate()
    {
        if (!isSpawned) return;

        // 일정 속도로 계속 이동
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
                if (isOneUp)
                {
                    if (GameManager.Instance != null)
                    {
                        //GameManager.Instance.lives++;
                        GameManager.Instance.AddLife(transform.position + Vector3.up * 1.0f);
                    }
                }
                else
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.powerUpSound);
                    GameManager.Instance.AddScore(1000, transform.position + Vector3.up * 1.0f);
                    player.TakeItem("Super"); // 플레이어에게 슈퍼 상태로 변하라고 전달
                }
            }
            Destroy(gameObject); // 버섯 먹었으므로 파괴
            return; // 아래 벽 충돌 로직은 건너뜀
        }

        // 2. 벽에 부딪혔을 때 방향 전환 (기존 로직)
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.7f) // 옆면 충돌 시
            {
                direction *= -1; // 방향 반전
                break;
            }
        }
    }

    public void BounceAndChangeDirection()
    {
        if (!isSpawned) return; // 아직 블록에서 나오는 중이면 무시
        
        direction *= -1; // 방향 반전
        // 살짝 위로 통통 튀어오르게 만듭니다 (수치는 원하시는 느낌으로 조절하세요)
        rb.velocity = new Vector2(rb.velocity.x, 5f); 
    }
}
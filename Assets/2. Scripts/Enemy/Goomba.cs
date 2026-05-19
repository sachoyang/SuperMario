using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goomba : MonoBehaviour, IEnemy
{
    public float moveSpeed = 2f; // 굼바 이동 속도
    private int direction = -1;  // 기본적으로 왼쪽(-1)으로 출발

    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D col;
    private SpriteRenderer sr;

    private bool isDead = false;

    // 굼바의 수면 상태와 카메라 참조 변수
    private bool isAwake = false;
    private Camera mainCam;
    public float wakeUpDistance = 12f; // 카메라 중앙으로부터 깨어날 거리 (화면 크기에 따라 조절)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        mainCam = Camera.main;
        rb.simulated = false;
    }

    // 매 프레임마다 카메라와의 거리 확인
    void Update()
    {
        if (isDead) return;

        // 1. 아직 자고 있다면 거리 체크
        if (!isAwake)
        {
            // 굼바의 X위치 - 카메라의 X위치 = 거리가 wakeUpDistance보다 작아지면 기상!
            if (transform.position.x - mainCam.transform.position.x < wakeUpDistance)
            {
                isAwake = true;
                rb.simulated = true; // 물리 엔진 켜기 (이때 바닥으로 툭 떨어지며 걷기 시작)
            }
        }
        // 2. [선택/최적화] 화면 왼쪽으로 너무 멀리 지나쳐 버렸다면 삭제 (원작 고증)
        else
        {
            if (mainCam.transform.position.x - transform.position.x > wakeUpDistance + 5f)
            {
                Destroy(gameObject); // 화면 밖으로 완전히 나가면 굼바 삭제 (메모리 절약)
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || !isAwake) return;
        // x축은 계속 이동, y축은 현재 물리 속도(중력) 유지
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // 1. 벽이나 파이프(옆면)에 부딪혔을 때 방향 전환
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.7f)
            {
                direction *= -1;
                break;
            }
        }

        // 2. 플레이어와 충돌했을 때 (밟혔는지, 옆에서 부딪혔는지 판별)
        if (collision.gameObject.CompareTag("Player"))
        {
            MarioCtrl player = collision.gameObject.GetComponent<MarioCtrl>();
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (player.isStarInvincible) return;    //스타 상태면 무시

            // 플레이어가 위에서 아래로 떨어지는 중이고, 굼바보다 위쪽에 있다면 '밟은 것'으로 판정
            if (playerRb.velocity.y < 0 && collision.transform.position.y > transform.position.y + 0.3f)
            {
                Squished(); // 굼바 찌그러짐
                player.StompBounce(); // 플레이어는 통통 튀어오름 (PlayerCtrl에 추가할 예정)
            }
            else
            {
                // 플레이어가 옆에서 닿았을 때 -> 플레이어 데미지 처리
                //Debug.Log("마리오가 데미지를 입어야 합니다!");
                player.TakeDamage();
            }
        }
    }

    // [죽음 1] 플레이어에게 밟혔을 때
    public void Squished()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.simulated = false; // 물리 연산 끄기
        col.enabled = false;  // 충돌체 끄기

        SoundManager.Instance.PlaySFX(SoundManager.Instance.stompSound);

        // 💡 [추가] 밟았을 때 100점 추가!
        if (GameManager.Instance != null)
        {
            //GameManager.Instance.AddScore(100);
            GameManager.Instance.AddScore(100, transform.position + Vector3.up * 0.5f);
        }

        anim.SetTrigger("Squish"); // 찌그러지는 애니메이션 실행
        Destroy(gameObject, 0.5f); // 0.5초 뒤에 시체(?) 삭제
    }

    // [죽음 2] 블록 아래에서 맞았을 때
    public void HitFromBelow()
    {
        if (isDead) return;
        isDead = true;
        SoundManager.Instance.PlaySFX(SoundManager.Instance.kickSound);
        anim.enabled = false; // 애니메이션 끄기
        sr.flipY = true;      // 거꾸로 뒤집기
        col.enabled = false;  // 충돌체 꺼서 바닥 밑으로 떨어지게 함

        // 위로 통통 튕겨 올라갔다가 떨어지는 연출
        rb.velocity = new Vector2(0, 2.5f);
        rb.gravityScale = 2f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100, transform.position + Vector3.up * 0.5f);
        }

        Destroy(gameObject, 2f); // 화면 밖으로 떨어질 시간(2초) 뒤 삭제
    }

    // 파이어볼에 맞았을 때
    public void HitByFireball()
    {
        if (isDead) return;
        isDead = true;

        anim.enabled = false;
        sr.flipY = true;
        col.enabled = false;
        SoundManager.Instance.PlaySFX(SoundManager.Instance.kickSound);
        // 위로 살짝 튕겼다가 떨어짐
        rb.velocity = new Vector2(0, 2.5f);
        rb.gravityScale = 2f;

        // 💡 100점 획득 및 팝업 텍스트 띄우기!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100, transform.position + Vector3.up * 0.5f);
        }

        Destroy(gameObject, 2f);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Koopa : MonoBehaviour, IEnemy
{
    // 💡 엉금엉금의 3가지 상태 정의
    public enum KoopaState { Walking, ShellIdle, ShellMoving }
    public KoopaState currentState = KoopaState.Walking;

    public float walkSpeed = 2f;    // 평소 걷는 속도
    public float shellSpeed = 10f;  // 등껍질 발차기 속도 (아주 빠름!)
    private int direction = -1;     // 시작 방향 (왼쪽)

    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D col;
    private SpriteRenderer sr;

    private bool isDead = false;
    private bool isAwake = false;
    private Camera mainCam;
    public float wakeUpDistance = 12f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식이면 GetComponentInChildren 사용
        mainCam = Camera.main;
        rb.simulated = false; // 수면 상태
    }

    void Update()
    {
        if (isDead) return;

        // 카메라 거리에 따른 수면/기상 (굼바와 동일)
        if (!isAwake)
        {
            if (transform.position.x - mainCam.transform.position.x < wakeUpDistance)
            {
                isAwake = true;
                rb.simulated = true;
            }
        }
        else if (mainCam.transform.position.x - transform.position.x > wakeUpDistance+2f)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (isDead || !isAwake) return;

        // 상태에 따라 물리 이동 속도가 다름
        if (currentState == KoopaState.Walking)
        {
            rb.velocity = new Vector2(direction * walkSpeed, rb.velocity.y);
        }
        else if (currentState == KoopaState.ShellMoving)
        {
            rb.velocity = new Vector2(direction * shellSpeed, rb.velocity.y);
        }
        else if (currentState == KoopaState.ShellIdle)
        {
            // 가만히 있는 껍질은 마찰력을 받아 제자리에 정지
            rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0, 15f * Time.fixedDeltaTime), rb.velocity.y);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || !isAwake) return;

        // 💡 1. 적(Enemy)과의 충돌 처리를 가장 먼저 합니다!
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (currentState == KoopaState.ShellMoving)
            {
                // [날아가는 껍질일 때] 적을 죽이고 나는 그대로 직진! (벽 충돌 무시)
                IEnemy otherEnemy = collision.gameObject.GetComponent<IEnemy>();
                if (otherEnemy != null)
                {
                    otherEnemy.HitByFireball(); // 적을 뒤집어서 날려버림 (콜라이더 즉시 꺼짐)
                    if (GameManager.Instance != null)
                        GameManager.Instance.AddScore(500, collision.transform.position + Vector3.up);
                }
                
                // 💡 여기서 return을 해버리기 때문에 아래의 '방향 전환(벽 판정)' 로직을 안 타고 그대로 직진합니다!
                return; 
            }
            else if (currentState == KoopaState.Walking)
            {
                // [걸어 다닐 때] 다른 적과 부딪히면 서로 튕겨서 돌아갑니다.
                direction *= -1;
                sr.flipX = (direction == 1);
                return;
            }
        }

        // 💡 2. 순수 벽/파이프 충돌 (방향 전환)
        bool hitWall = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 플레이어도 아니고 적도 아닐 때만 '벽'으로 인정합니다.
            if (Mathf.Abs(contact.normal.x) > 0.7f && 
                !collision.gameObject.CompareTag("Player") && 
                !collision.gameObject.CompareTag("Enemy"))
            {
                hitWall = true;
                break;
            }
        }

        if (hitWall)
        {
            direction *= -1; // 방향 반전
            if (currentState == KoopaState.Walking) sr.flipX = (direction == 1);
            
            // 껍질 상태에서 벽에 부딪히면 튕기는 효과음(퉁!) 넣기
            // if (currentState == KoopaState.ShellMoving) AudioSource.PlayClipAtPoint(bumpSound, transform.position);
        }

        // 💡 3. 플레이어(마리오)와 충돌
        if (collision.gameObject.CompareTag("Player"))
        {
            MarioCtrl player = collision.gameObject.GetComponent<MarioCtrl>(); // 또는 MarioCtrl
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (player.isStarInvincible) return;

            // 마리오가 위에서 아래로 밟았을 때
            if (playerRb.velocity.y < 0 && collision.transform.position.y > transform.position.y + 0.2f)
            {
                player.StompBounce();
                Squished(); // IEnemy 메서드 호출하여 껍질로 변신 또는 정지
            }
            // 옆에서 닿았을 때
            else
            {
                if (currentState == KoopaState.ShellIdle)
                {
                    // 가만히 있는 껍질을 옆에서 치면 -> 발차기!
                    // 마리오가 있는 쪽의 반대 방향으로 날아갑니다.
                    direction = (player.transform.position.x < transform.position.x) ? 1 : -1;
                    Squished(); // ShellMoving 상태로 변경됨
                }
                else
                {
                    // 걷고 있거나 날아가는 껍질에 닿으면 -> 마리오가 데미지를 입음!
                    player.TakeDamage();
                }
            }
        }
    }

    // --- IEnemy 인터페이스 구현 ---

    public void Squished()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance.kickSound);
        if (currentState == KoopaState.Walking)
        {
            ChangeState(KoopaState.ShellIdle); // 껍질로 변신
            if (GameManager.Instance != null) GameManager.Instance.AddScore(100, transform.position + Vector3.up * 0.5f);
        }
        else if (currentState == KoopaState.ShellIdle)
        {
            // 가만히 있는 껍질을 밟으면 -> 날아감
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) direction = (player.transform.position.x < transform.position.x) ? 1 : -1;
            ChangeState(KoopaState.ShellMoving);
        }
        else if (currentState == KoopaState.ShellMoving)
        {
            // 날아가는 껍질을 밟으면 -> 멈춤
            ChangeState(KoopaState.ShellIdle);
            if (GameManager.Instance != null) GameManager.Instance.AddScore(100, transform.position + Vector3.up * 0.5f);
        }

        GameManager.Instance.AddScore(500, transform.position + Vector3.up * 0.5f);
    }

    public void HitFromBelow() { DieByFlip(); }
    public void HitByFireball() { DieByFlip(); }

    private void DieByFlip()
    {
        if (isDead) return;
        SoundManager.Instance.PlaySFX(SoundManager.Instance.kickSound);
        isDead = true;
        anim.enabled = false;
        sr.flipY = true;
        col.enabled = false;
        rb.velocity = new Vector2(0, 2.5f); 
        rb.gravityScale = 2f;
        
        if (GameManager.Instance != null) GameManager.Instance.AddScore(500, transform.position + Vector3.up * 0.5f);
        Destroy(gameObject, 2f);
    }

    // --- 상태 변화 및 애니메이션/콜라이더 처리 ---
    
    private void ChangeState(KoopaState newState)
    {
        currentState = newState;
        
        if (newState == KoopaState.Walking)
        {
            anim.SetBool("isShell", false);
            // 걷기 상태 콜라이더 (키가 큼)
            col.size = new Vector2(0.8f, 1.0f);
            col.offset = new Vector2(0f, 0f);
        }
        else if (newState == KoopaState.ShellIdle || newState == KoopaState.ShellMoving)
        {
            anim.SetBool("isShell", true);
            // 껍질 상태 콜라이더 (작아짐)
            col.size = new Vector2(0.8f, 0.9f);
            col.offset = new Vector2(0f, 0f);
            
            // 발로 차인 상태 애니메이션 제어
            anim.SetBool("isMoving", newState == KoopaState.ShellMoving); 
        }
    }
}
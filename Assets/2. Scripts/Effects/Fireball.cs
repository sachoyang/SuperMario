using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 10f;          // 전진 속도
    public float bounceForce = 8f;     // 바닥 튕기는 힘
    public GameObject explosionPrefab; // 1단계에서 만든 폭발 이펙트 프리팹

    private Rigidbody2D rb;
    private int direction = 1;         // 1: 오른쪽, -1: 왼쪽

    [Header("Despawn Settings")]
    public float despawnDistanceX = 15f; // 카메라 중심으로부터 이만큼 멀어지면 삭제
    public float deadZoneY = -5f;        // 화면 맨 밑(구덩이)으로 떨어지면 삭제

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    // 생성 시점에 방향 설정
    public void Init(int dir)
    {
        direction = dir;
        rb.velocity = new Vector2(direction * speed, -bounceForce * 0.3f); // 살짝 아래로 비스듬히 발사
        SoundManager.Instance.PlaySFX(SoundManager.Instance.fireballSound);
    }

    void Update()
    {
        // ... (혹시 기존에 Update 안에 이동 로직 등이 있다면 그대로 유지하세요) ...

        // 1. 메인 카메라의 X 위치 가져오기
        if (Camera.main != null)
        {
            float camX = Camera.main.transform.position.x;

            // 2. 파이어볼과 카메라의 X축 거리 계산 (절댓값)
            float distanceX = Mathf.Abs(transform.position.x - camX);

            // 3. 거리가 멀어지거나 구덩이로 떨어지면 파괴
            if (distanceX > despawnDistanceX || transform.position.y < deadZoneY)
            {
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        // x축은 계속 이동, y축은 현재 물리 속도(중력 및 튕김) 유지
        rb.velocity = new Vector2(direction * speed, rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool hitWall = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 1. 바닥에 닿았을 때 통통 튕기기 (법선 벡터 y 체크)
            if (contact.normal.y > 0.6f)
            {
                rb.velocity = new Vector2(rb.velocity.x, bounceForce); // 위로 튕김
            }
            // 2. 벽(옆면)에 부딪혔을 때 폭발 (법선 벡터 x 체크)
            else if (Mathf.Abs(contact.normal.x) > 0.6f)
            {
                hitWall = true;
            }
        }

        // 3. 적(Enemy)과 부딪혔을 때 (나중에 Enemy 태그 구현)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 굼바 스크립트를 찾아 파이어볼 피격 함수 실행!
            IEnemy enemy = collision.gameObject.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.HitByFireball(); // 굼바든 거북이든 각자의 파이어볼 피격 함수가 실행됨
            }

            Explode();
            return;
        }

        if (hitWall) Explode();
    }

    void Explode()
    {
        if (explosionPrefab != null)
        {
            // 현재 위치에 폭발 이펙트 생성
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        // 파이어볼 본체 파괴
        Destroy(gameObject);
    }
}
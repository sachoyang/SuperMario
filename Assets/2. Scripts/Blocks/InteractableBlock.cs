using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BlockType { Question, Brick, Hidden, CoinBrick }

public class InteractableBlock : MonoBehaviour
{
    public BlockType type;
    public GameObject itemPrefab; // 나올 아이템 (버섯, 동전 등)
    [Header("Upgrade Item (Optional)")]
    public GameObject upgradedItemPrefab; // 슈퍼/파이어 상태일 때 나올 아이템 (꽃)
    public Sprite emptyBlockSprite; // 다 썼을 때 바뀔 스프라이트
    public int coinCount = 1; // 여러 번 칠 수 있는 벽돌용
    [Header("Break Effect Settings")]
    public GameObject debrisPrefab; // 1개의 기본 파편 프리팹

    // 4개의 조각 스프라이트를 인스펙터에서 순서대로 등록 (좌상, 우상, 좌하, 우하)
    public Sprite[] debrisSprites = new Sprite[4];

    private bool isUsed = false;
    private Animator anim;
    private SpriteRenderer sr;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        // 투명 블록인 경우 처음엔 렌더러를 끔
        if (type == BlockType.Hidden) sr.enabled = false;
    }

    // 플레이어가 아래에서 충돌했을 때 호출될 함수
    public void OnHit(bool isSuper)
    {
        if (isUsed)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.bumpSound);
            return;
        } 
        //Debug.Log("Block Hit!");
        // 블록을 치는 순간, 블록 위에 적이 있는지 박스를 그려서 검사합니다.
        CheckEnemyOnTop();
        // 1. 투명 블록이면 모습 드러내기
        if (type == BlockType.Hidden) sr.enabled = true;

        // 2. 블록 위아래로 들썩이는 애니메이션 실행
        if (anim != null) anim.SetTrigger("Hit");
        // 3. 타입별 로직
        switch (type)
        {
            case BlockType.Question:
            case BlockType.Hidden:
                SpawnItem(isSuper);
                SetToEmpty(); // 즉시 단단해짐
                break;

            case BlockType.CoinBrick:
                if (coinCount > 0)
                {
                    SpawnItem(isSuper);
                    coinCount--;
                    if (anim != null) anim.SetTrigger("Hit"); // 들썩이기

                    if (coinCount <= 0)
                        SetToEmpty(); // 10번 다 치면 단단해짐
                }
                break;

            case BlockType.Brick:
                // 아이템이 들어있는 벽돌인 경우 (프리펩에 item이 설정된 경우)
                if (itemPrefab != null)
                {
                    SpawnItem(isSuper);
                    SetToEmpty();
                }
                // 아무것도 없는 일반 벽돌인 경우
                else
                {
                    if (isSuper)
                    {
                        BreakBlock(); // 슈퍼 마리오면 파괴
                    }
                    else
                    {
                        if (anim != null) anim.SetTrigger("Hit"); // 작은 마리오는 들썩이기만
                    }
                }
                break;
        }
    }

    void SpawnItem(bool isSuper)
    {
        /* 아이템 생성 로직 */
        if (itemPrefab == null) return;

        GameObject targetPrefab = itemPrefab;
        if (isSuper && upgradedItemPrefab != null)
        {
            targetPrefab = upgradedItemPrefab;
        }

        if (targetPrefab == null) return;

        // 블록의 약간 위쪽에서 아이템 생성
        GameObject item = Instantiate(targetPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        // 아이템이 동전인지, 버섯(이동형)인지에 따라 처리를 나눕니다.
        if (item.CompareTag("Coin"))
        {
            // 동전은 애니메이션 후 파괴되거나 특정 로직 실행
            // 동전 블록을 치면 동전 1개 증가, 200점 획득 및 팝업 텍스트 띄우기!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(1);
                // 블록 바로 위쪽에 200점 텍스트를 띄웁니다.
                GameManager.Instance.AddScore(200, transform.position + Vector3.up * 1.0f);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.coinSound);
            }
            //StartCoroutine(AnimateCoin(item));
        }
        else
        {
            // 버섯이나 꽃은 위로 슥 올라오는 연출 필요
            SoundManager.Instance.PlaySFX(SoundManager.Instance.itemAppearSound);
            StartCoroutine(AppearItem(item));
        }
    }
    void SetToEmpty()
    {
        isUsed = true;
        sr.sprite = emptyBlockSprite;
    }
    void BreakBlock()
    {
        // 4개 파편의 초기 물리 세팅 (속도, 회전력)
        // 순서: 좌상, 우상, 좌하, 우하
        Vector2[] velocities = {
            new Vector2(-3f, 8f),  // 좌상: 왼쪽 위로 강하게
            new Vector2(3f, 8f),   // 우상: 오른쪽 위로 강하게
            new Vector2(-2f, 5f),  // 좌하: 왼쪽 위로 약하게
            new Vector2(2f, 5f)    // 우하: 오른쪽 위로 약하게
        };
        float[] torques = { 5f, -5f, 5f, -5f }; // 회전 방향도 다르게

        // 4번 반복하며 파편 생성 및 초기화
        for (int i = 0; i < 4; i++)
        {
            // 1. 프리팹 생성
            GameObject debrisGo = Instantiate(debrisPrefab, transform.position, Quaternion.identity);
            BrickDebris debris = debrisGo.GetComponent<BrickDebris>();

            // 2. 해당 순서의 스프라이트와 물리 값 주입
            if (debris != null && debrisSprites[i] != null)
            {
                debris.Init(debrisSprites[i], velocities[i], torques[i]);
            }
        }

        // 본체 파괴
        Destroy(gameObject);
    }

    // 버섯이 블록 안에서 슥 올라오는 연출
    IEnumerator AppearItem(GameObject item)
    {
        // 1. 처음에는 물리/충돌을 꺼둠
        item.GetComponent<Collider2D>().enabled = false;
        if (item.GetComponent<Rigidbody2D>() != null)
            item.GetComponent<Rigidbody2D>().simulated = false;

        Vector3 endPos = item.transform.position + Vector3.up * 0.6f; // 0.5칸 위로

        float elapsed = 0;
        while (elapsed < 1f)
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, endPos, Time.deltaTime * 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. 올라온 후 물리 작동 및 AI 시작
        item.GetComponent<Collider2D>().enabled = true;
        if (item.GetComponent<Rigidbody2D>() != null)
            item.GetComponent<Rigidbody2D>().simulated = true;

        // 버섯 AI 스크립트가 있다면 이동 시작 명령 전달
        MushroomAI ai = item.GetComponent<MushroomAI>();
        if (ai != null)
        {
            ai.StartMoving(1); // 1은 오른쪽 방향
        }

        StarAI sAi = item.GetComponent<StarAI>();
        if (sAi != null)
        {
            sAi.StartMoving(1);
        }
    }

    // --- InteractableBlock.cs 의 CheckEnemyOnTop 함수 교체 ---

    void CheckEnemyOnTop()
    {
        Vector2 checkPos = (Vector2)transform.position + Vector2.up * 1.0f;

        // 💡 OverlapBoxAll을 사용하여 박스 안에 있는 모든 콜라이더를 가져옵니다.
        Collider2D[] cols = Physics2D.OverlapBoxAll(checkPos, new Vector2(0.9f, 0.2f), 0f);

        foreach (Collider2D col in cols)
        {
            // 1. 적(Enemy)인 경우 기존처럼 처리
            if (col.CompareTag("Enemy"))
            {
                IEnemy enemy = col.GetComponent<IEnemy>();
                if (enemy != null)
                {
                    enemy.HitFromBelow();
                }
            }
            // 2. 적이 아닐 경우 아이템인지 확인 (태그를 안 써도 컴포넌트로 직접 확인)
            else 
            {
                MushroomAI mushroom = col.GetComponent<MushroomAI>();
                if (mushroom != null)
                {
                    mushroom.BounceAndChangeDirection();
                }

                StarAI star = col.GetComponent<StarAI>();
                if (star != null)
                {
                    star.BounceAndChangeDirection();
                }
            }
        }
    }

    // 에디터에서 블록 위 타격 범위(박스)를 눈으로 확인하기 위한 기즈모 추가
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Vector2 checkPos = (Vector2)transform.position + Vector2.up * 1.0f;
        Gizmos.DrawWireCube(checkPos, new Vector2(0.9f, 0.2f));
    }
}
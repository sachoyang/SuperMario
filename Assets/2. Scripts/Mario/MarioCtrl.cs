using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarioCtrl : MonoBehaviour
{
    private Animator anim;                    // Player 객체의 Animator component 를 위한 Reference(참조) 이다
    private Rigidbody2D rigidbody2D;

    public RuntimeAnimatorController smallController; // 기본 컨트롤러
    public AnimatorOverrideController superOverride;  // 버섯 먹었을 때

    [Header("Transformation Sprites")]
    public Sprite idleSmall; // 작은 마리오 서있는 스프라이트
    public Sprite idleSuper; // 슈퍼 마리오 서있는 스프라이트
    public Sprite idleFire;  // 파이어 마리오 서있는 스프라이트

    [Header("Fireball Setting")]
    public GameObject fireballPrefab;   // 3단계에서 만든 파이어볼 프리팹
    public Transform fireballSpawnPos;  // 파이어볼이 스폰될 위치 (마리오 손 앞쪽 자식 오브젝트)
    public int maxFireballCount = 2;   // 화면에 동시에 존재 가능한 파이어볼 개수
    public string currentState = "Small"; // 현재 마리오 상태 (Small, Super, Fire) - TakeItem에서 갱신

    [Header("Fire Mario Split Body")]
    public GameObject fireMarioBody; // 아까 만든 부모 빈 객체
    public Animator upperAnim;       // 상체 애니메이터
    public Animator lowerAnim;       // 하체 애니메이터
    private SpriteRenderer mainSr;   // 기존 몸통(작은/슈퍼 마리오용) 렌더러

    [Header("Star Invincibility")]
    public bool isStarInvincible = false; // 현재 별 먹은 상태인지 체크
    public float starDuration = 10f;      // 무적 지속 시간 (10초)
    private Coroutine starRoutine;        // 별 무적 코루틴 참조 (중복 획득 시 이것만 정지)

    // 유니티 밖에서 준비한 3개의 전체 스프라이트 시트(그림 파일)를 넣습니다.
    // 인스펙터에서 0:레드, 1:그린, 2:블랙 순서로 넣어주세요.
    public Texture2D[] starPaletteTextures;
    // 파이어 마리오 전용 상/하체 텍스처 배열
    [Header("Fire Mario Star Textures")]
    public Texture2D[] fireUpperStarTextures; // 상체용 3장 (빨, 검, 초)
    public Texture2D[] fireLowerStarTextures; // 하체용 3장 (빨, 검, 초)

    [Header("Clear Settings")]
    public Animator castleFlagAnim;

    // 원래 사용하던 기본 (레드) 텍스처를 기억해둡니다.
    private Texture2D defaultTexture;
    // 현재 어떤 텍스처를 덮어씌워야 하는지 기억할 변수
    private Texture2D currentStarTexture = null;
    private Texture2D currentUpperStarTexture = null; // 상체 지시용
    private Texture2D currentLowerStarTexture = null; // 하체 지시용

    private BoxCollider2D col;

    // 인스펙터에 노출 안됨
    [HideInInspector]
    public bool dirRight = true;            // 플레이어의 현재 바라보는 방향을 알기 위함 

    //[HideInInspector]
    public bool jump = false;                   // 플레이어가 현재 점프중인지 아닌지 알기 위함 

    private string originalSortingLayerName;
    private int originalSortingOrder;

    [Header("Player States")]
    public bool isCrouching = false; // 숙이고 있는지 체크
    public bool grounded = false;            // 플레이어가 땅에 있는지 아닌지 구별을 위한 변수
    public bool isSkidding = false;
    private Transform groundCheck;             //만약 플레이어가 땅에 있을때 position을 marking 할 곳
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); // 체크할 사각형의 크기

    public bool isSuper = false;

    private Transform headCheck;       // 머리 위치 객체
    private BoxCollider2D headCol;     // 머리에 달린 트리거 콜라이더
    private AudioSource audio;

    [Header("SMB1 Physics Constants")]
    private float PPU = 16f; // 1 유니티 Unit당 픽셀 수

    // X축 이동 속도 (픽셀/초 단위에서 PPU로 나눔)
    private float MIN_WALK { get { return 4.453125f / PPU; } }
    private float MAX_WALK { get { return 93.75f / PPU; } }
    private float MAX_RUN { get { return 153.75f / PPU; } }
    private float ACC_WALK { get { return 133.59375f / PPU; } }
    private float ACC_RUN { get { return 200.390625f / PPU; } }
    private float DEC_REL { get { return 182.8125f / PPU; } }  // 키 뗐을 때 마찰력
    private float DEC_SKID { get { return 365.625f / PPU; } }  // 방향 틀 때 마찰력 (스퀴드)

    // Y축 점프 및 중력
    private float JUMP_SPEED_IDLE { get { return 280f / PPU; } } // JS의 -280을 유니티에 맞게 양수로
    private float JUMP_SPEED_RUN { get { return 300f / PPU; } }

    // 떨어질 때 중력 (상황별로 다름)
    private float STOP_FALL { get { return 1575f / PPU; } }
    private float WALK_FALL { get { return 1800f / PPU; } }
    private float RUN_FALL { get { return 2025f / PPU; } }

    // 점프 키를 누르고 올라갈 때의 약한 중력
    private float STOP_FALL_A { get { return 450f / PPU; } }
    private float WALK_FALL_A { get { return 421.875f / PPU; } }
    private float RUN_FALL_A { get { return 562.5f / PPU; } }

    private float MAX_FALL { get { return 270f / PPU; } } // 최대 낙하 속도

    // 현재 상태를 기억할 변수들
    private float currentGravity = 0f;
    private float currentGravityBase = 0f; // 키 뗐을 때 기본 중력
    private float currentGravityHold = 0f; // 키 누르고 있을 때 가벼운 중력

    [Header("Damage & Death")]
    public bool isInvincible = false; // 무적 상태인지 체크
    public bool isDead = false;       // 죽었는지 체크
    public Sprite deadSprite;         // 마리오 사망 스프라이트

    [Header("Clear Sequence")]
    public bool isClearing = false;
    public float slideDownSpeed = 3f;      // 깃발 타고 내려오는 속도
    // 인스펙터에서 조절할 점프 힘 (x: 앞으로 나아가는 힘, y: 위로 뛰는 힘)
    public Vector2 jumpOffForce = new Vector2(2f, 5f);
    public float walkToCastleSpeed = 3f;   // 성으로 걸어가는 속도
    private bool isClearJumping = false;

    private float originalGravity; // 기존 중력값 저장용

    [Header("Warp Sequence")]
    public bool isWarping = false; // 워프 상태 플래그

    void Awake()
    {
        // 레퍼런스(참조)들을 셋팅.
        groundCheck = transform.Find("groundCheck");
        anim = GetComponent<Animator>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        audio = GetComponent<AudioSource>();
        col = GetComponent<BoxCollider2D>();
        mainSr = GetComponent<SpriteRenderer>();
        headCheck = transform.Find("headCheck"); // 자식 오브젝트 이름 확인 필수!
        defaultTexture = mainSr.sprite.texture as Texture2D;
        if (headCheck != null)
        {
            headCol = headCheck.GetComponent<BoxCollider2D>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // 디버깅용 링크에러 체크
        // 링크 확인용-Awake에서 참조하는 것들 모두 assert로 확인해야함.(오타, awake를 탓는지 여부도 확인가능)
        Debug.Assert(groundCheck);
        Debug.Assert(anim);
        Debug.Assert(rigidbody2D);

        SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldBGM);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || isClearing) return;

        grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, 1 << LayerMask.NameToLayer("Ground"));   // overlapbox방식
        anim.SetBool("grounded", grounded);
        // 만약 점프 버튼을 눌렀를때 플레이어가 땅에 있었다면 플레이어는 점프 할 수있다.
        if (Input.GetButtonDown("Jump") && grounded)
        {
            jump = true;
            if (currentState == "Small")
                SoundManager.Instance.PlaySFX(SoundManager.Instance.jumpSmallSound);
            else
                SoundManager.Instance.PlaySFX(SoundManager.Instance.jumpSuperSound);
        }

        float v = Input.GetAxisRaw("Vertical");

        // 슈퍼/파이어 마리오이고, 땅에 있으며, 아래 키를 눌렀을 때
        if (isSuper && grounded && v < 0)
        {
            if (!isCrouching)
            {
                isCrouching = true;
                anim.SetBool("isCrouching", true);
                if (currentState == "Fire") { upperAnim.SetBool("isCrouching", true); lowerAnim.SetBool("isCrouching", true); }

                // 콜라이더를 작은 마리오 사이즈로 반 접어버림!
                SetColliderSize(0.75f, 1f, 0.5f);
                SetCheckers(0f, 0.7f, 1f, 0.75f);
            }
        }
        // 아래 키를 뗐거나, 공중으로 떴을 때 (숙이기 해제)
        else if (isCrouching && (v >= 0 || !grounded))
        {
            isCrouching = false;
            anim.SetBool("isCrouching", false);
            if (currentState == "Fire") { upperAnim.SetBool("isCrouching", false); lowerAnim.SetBool("isCrouching", false); }

            // 💡 원래 사이즈(높이 2)로 복구
            float width = (currentState == "Fire") ? 0.9f : 0.75f;
            SetColliderSize(width, 2f, 1.0f);
            SetCheckers(0f, 0.7f, 2f, 0.75f);
        }

        // 파이어 마리오 상태일 때만 공격 키(보통 'Fire' 또는 특정 키) 감지
        // 💡 스타 무적 중에는 파이어볼을 막습니다.
        //    발사 모션(Upper_Shoot)만 다른 아틀라스(throwfireball.png, 34x32)를 쓰는데,
        //    스타 상체 팔레트는 fire_upper.png(96x24) 리컬러라 _MainTex 를 덮어씌우면
        //    UV가 어긋나 상체 스프라이트가 깨집니다. (달리기는 FixedUpdate 에서
        //    Input.GetButton("Fire1") 을 따로 읽으므로 그대로 동작합니다.)
        if (currentState == "Fire" && !isStarInvincible && Input.GetButtonDown("Fire1")) // 예: Left Ctrl 또는 Left Click
        {
            // 화면에 동시에 파이어볼이 2개까지만 존재하게 제한
            if (GameObject.FindGameObjectsWithTag("FireBall").Length < maxFireballCount) // Coin 태그를 Fireball 전용 태그로 바꾸는 걸 권장
            {
                ShootFireball();
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.timeLeft = 110f;
                Debug.Log("테스트용: 남은 시간을 110초로 강제 변경했습니다!");
            }
        }

    }

    // 애니메이터가 렌더링을 끝낸 직후(LateUpdate)에 무조건 색을 덮어씌움! (철벽 방어)
    private void LateUpdate()
    {
        if (isStarInvincible)
        {
            // 상/하체용 텍스처를 모두 넘겨줍니다.
            ApplyStarTexture(currentStarTexture, currentUpperStarTexture, currentLowerStarTexture);
        }
    }

    void FixedUpdate()
    {
        if (isDead || isWarping) return;
        if (isClearing && !isClearJumping) return;
        // GetAxisRaw를 써야 레트로 게임처럼 0 아니면 1로 딱 떨어집니다.
        // float h = Input.GetAxisRaw("Horizontal");
        // bool isRunButtonHeld = Input.GetButton("Fire1"); // 대시(B) 버튼 (보통 파이어볼과 같이 씀)
        // bool isJumpButtonHeld = Input.GetButton("Jump"); // 점프(A) 버튼 유지 여부

        // 클리어 점프 중일 때는 사용자 입력(h, 버튼)을 강제로 0/false로 만듭니다.
        float h = (isClearing && isClearJumping) ? 0f : Input.GetAxisRaw("Horizontal");
        bool isRunButtonHeld = (isClearing && isClearJumping) ? false : Input.GetButton("Fire1");
        bool isJumpButtonHeld = (isClearing && isClearJumping) ? false : Input.GetButton("Jump");

        Vector2 vel = rigidbody2D.velocity;
        float absVx = Mathf.Abs(vel.x);

        // ----------------------------------------------------
        // 1. 수평(X축) 이동 로직 (SMB1 완벽 구현)
        // ----------------------------------------------------
        if (!isClearing || !isClearJumping)
        {
            if (h == 0)
            {
                // 입력이 없을 때: 서서히 멈춤 (마찰력)
                if (absVx > 0)
                {
                    vel.x = Mathf.MoveTowards(vel.x, 0, DEC_REL * Time.fixedDeltaTime);
                }
                isSkidding = false;
            }
            else
            {
                // 입력이 있을 때: 가속 또는 스퀴드
                if (Mathf.Sign(h) == Mathf.Sign(vel.x) || absVx < MIN_WALK)
                {
                    // 같은 방향으로 가속 중
                    isSkidding = false;
                    float acc = isRunButtonHeld ? ACC_RUN : ACC_WALK;
                    vel.x += h * acc * Time.fixedDeltaTime;

                    // 최고 속도 제한
                    float currentMaxSpeed = isRunButtonHeld ? MAX_RUN : MAX_WALK;
                    vel.x = Mathf.Clamp(vel.x, -currentMaxSpeed, currentMaxSpeed);
                }
                else
                {
                    // 반대 방향으로 키를 누름 (스퀴드 발생!)
                    isSkidding = true;
                    vel.x += h * DEC_SKID * Time.fixedDeltaTime;
                }
            }

            // 아주 느릴 때 강제로 멈추게 해서 덜덜거림 방지
            if (h == 0 && Mathf.Abs(vel.x) < MIN_WALK) vel.x = 0;
        }
        // ----------------------------------------------------
        // 2. 수직(Y축) 이동 및 가변 중력 로직 (SMB1 완벽 구현)
        // ----------------------------------------------------
        if (jump && (!isClearing || !isClearJumping))
        {
            // 점프를 시작하는 순간, '현재 속도'에 따라 점프력과 중력이 결정됨!
            if (absVx < 1f) // 가만히 서 있거나 아주 느림
            {
                vel.y = JUMP_SPEED_IDLE;
                currentGravityBase = STOP_FALL;
                currentGravityHold = STOP_FALL_A;
            }
            else if (absVx < 2.5f) // 걷는 중
            {
                vel.y = JUMP_SPEED_IDLE;
                currentGravityBase = WALK_FALL;
                currentGravityHold = WALK_FALL_A;
            }
            else // 달리는 중 (대시 점프)
            {
                vel.y = JUMP_SPEED_RUN;
                currentGravityBase = RUN_FALL;
                currentGravityHold = RUN_FALL_A;
            }

            // 오디오 재생
            //AudioSource.PlayClipAtPoint(isSuper ? jumpClips[1] : jumpClips[0], transform.position);

            grounded = false;
            anim.SetBool("grounded", false);
            if (currentState == "Fire") { upperAnim.SetBool("grounded", false); lowerAnim.SetBool("grounded", false); }

            anim.SetTrigger("Jump");
            if (currentState == "Fire") { upperAnim.SetTrigger("Jump"); lowerAnim.SetTrigger("Jump"); }

            jump = false;

            // 💡 중력은 여기서 빼지 않습니다.
            // 바로 위에서 grounded = false 로 바꿨기 때문에, 아래의 '공중 중력 적용' 블록이
            // 같은 프레임에 반드시 실행됩니다. 예전에는 여기서도 한 번 빼서
            // 점프를 시작하는 프레임에만 중력이 두 번 적용되고 있었습니다.
            // 중력 계산 지점을 아래 한 곳으로 통일합니다.
        }

        // 공중에 있을 때의 중력 적용
        if (!grounded)
        {
            // 올라가는 중(vel.y > 0)이고, 점프 키를 꾹 누르고 있다면 중력이 약해짐 (더 높이 뜀)
            if (vel.y > 0 && isJumpButtonHeld)
            {
                currentGravity = currentGravityHold;
            }
            else // 떨어지는 중이거나, 키를 뗐다면 무거운 중력 적용 (툭 떨어짐)
            {
                currentGravity = currentGravityBase;
            }

            vel.y -= currentGravity * Time.fixedDeltaTime;
            vel.y = Mathf.Max(vel.y, -MAX_FALL); // 최대 낙하 속도 제한 (단말 속도)
        }
        else if (!jump)
        {
            // 땅에 있을 때는 기본 중력 상태로 리셋
            currentGravityBase = STOP_FALL;
        }

        //스타 상태일때
        if (isStarInvincible)
        {
            CheckStarEnemyCollision();
        }
        // ----------------------------------------------------
        // 3. 물리 및 애니메이션 최종 적용
        // ----------------------------------------------------
        rigidbody2D.velocity = vel;

        anim.SetFloat("moveSpeed", Mathf.Abs(vel.x));
        anim.SetBool("isSkidding", isSkidding);

        UpdateAnimations(h);

        // 바라보는 방향 전환 (Flip)
        if (h > 0 && !dirRight) Flip();
        else if (h < 0 && dirRight) Flip();
    }
    public void ChangeMarioState(string state)
    {
        currentState = state;
        switch (state)
        {
            case "Small":
                mainSr.enabled = true;           // 기본 몸통 켜기
                fireMarioBody.SetActive(false);  // 파이어 몸통 끄기
                anim.runtimeAnimatorController = smallController;
                SetColliderSize(0.75f, 1f, 0.5f); // 16x16 크기
                SetCheckers(-0.1f, 0.7f, 1f, 0.75f);
                break;
            case "Super":
                mainSr.enabled = true;
                fireMarioBody.SetActive(false);
                anim.runtimeAnimatorController = superOverride;
                SetColliderSize(0.8f, 2f, 1f); // 16x32 크기
                SetCheckers(-0.1f, 0.7f, 2.0f, 0.75f);
                break;
            case "Fire":
                mainSr.enabled = false;          // 기본 몸통 끄기!
                fireMarioBody.SetActive(true);   // 파이어 전용 상/하체 켜기!
                //anim.runtimeAnimatorController = fireOverride;
                SetColliderSize(0.8f, 2f, 1f); // 16x32 크기
                SetCheckers(-0.1f, 0.7f, 2.0f, 0.75f);
                break;
        }
    }

    // 2. 애니메이션 파라미터 동기화 (Update / FixedUpdate에 추가)
    // 기존 anim.SetFloat("moveSpeed", ...) 하시는 부분 바로 아래에 다음을 추가하세요.
    void UpdateAnimations(float h)
    {
        // 기본 몸통(Small, Super) 애니메이션 업데이트
        anim.SetFloat("moveSpeed", Mathf.Abs(h));
        anim.SetBool("grounded", grounded);
        anim.SetBool("isSkidding", isSkidding); // (skid 조건 검사 변수화 하셨다면)

        // 💡 파이어 마리오 상태일 때는 상/하체 애니메이터에도 똑같은 명령을 내려줍니다!
        if (currentState == "Fire" && fireMarioBody.activeSelf)
        {
            upperAnim.SetFloat("moveSpeed", Mathf.Abs(h));
            upperAnim.SetBool("grounded", grounded);
            upperAnim.SetBool("isSkidding", isSkidding);

            lowerAnim.SetFloat("moveSpeed", Mathf.Abs(h));
            lowerAnim.SetBool("grounded", grounded);
            lowerAnim.SetBool("isSkidding", isSkidding); // 하체만 스키드 재생
        }
    }

    void SetColliderSize(float width, float height, float offsetY)
    {
        // 💡 인스펙터에서 설정한 edgeRadius 값을 가져옵니다. (안 설정했으면 0)
        float edge = col.edgeRadius;

        // 💡 둥근 모서리 때문에 바깥으로 튀어나간 살(양쪽 두께인 edge * 2)만큼 
        // 원래 크기(width, height)에서 빼서 딱 맞춰줍니다!
        col.size = new Vector2(width - (edge * 2), height - (edge * 2));

        col.offset = new Vector2(0, offsetY);
    }

    void SetCheckers(float groundY, float groundSizeX, float headY, float headSizeX)
    {
        // 1. 발바닥(GroundCheck) 위치와 크기 변경
        if (groundCheck != null)
        {
            // 부모(플레이어) 기준 상대적 위치(localPosition)를 변경
            groundCheck.localPosition = new Vector3(0f, groundY, 0f);
            // y 크기(두께)는 기존 값을 유지하고 x 크기만 변경
            groundCheckSize = new Vector2(groundSizeX, groundCheckSize.y);
        }

        // 2. 머리(HeadCheck) 위치와 크기 변경
        if (headCheck != null)
        {
            headCheck.localPosition = new Vector3(0f, headY, 0f);
            if (headCol != null)
            {
                headCol.size = new Vector2(headSizeX, headCol.size.y);
            }
        }
    }


    // 에디터 뷰에서 체크 영역을 시각적으로 확인하기 위한 함수
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

    // 케릭터의 현재 방향을 바꿔주는 함수 
    void Flip()
    {
        //플레이어의 바라보는 방향을 바꾸자 
        dirRight = !dirRight;

        //플레이어의 local scale x에 -1을 곱하자
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    // 아이템(버섯, 꽃)이 플레이어와 닿았을 때 호출할 함수
    public void TakeItem(string itemType)
    {
        if (itemType == "Super")
        {
            if (currentState == "Small")
            {
                // 작은 마리오 -> 슈퍼 마리오 변신
                StartCoroutine(TransformationSequence("Small", "Super", idleSmall, idleSuper, false));
            }
            else
            {
                // 💡 이미 슈퍼이거나 파이어 상태일 때 버섯을 먹으면 변신 없이 점수만 획득!
                if (GameManager.Instance != null)
                {
                    //GameManager.Instance.AddScore(1000);
                    GameManager.Instance.AddScore(1000, transform.position + Vector3.up);
                }

                // (선택) 아이템 먹는 효과음 재생
                // AudioSource.PlayClipAtPoint(powerUpClip, transform.position);
            }
        }
        else if (itemType == "Fire")
        {
            if (currentState == "Small")
            {
                StartCoroutine(TransformationSequence("Small", "Fire", idleSmall, idleFire, false));
            }
            else if (currentState == "Super")
            {
                StartCoroutine(TransformationSequence("Super", "Fire", idleSuper, idleFire, false));
            }
            else if (currentState == "Fire")
            {
                // 💡 이미 파이어 상태일 때 꽃을 먹으면 변신 없이 점수만 획득!
                if (GameManager.Instance != null) GameManager.Instance.AddScore(1000);
            }
        }
        else if (itemType == "Star")
        {
            if (GameManager.Instance != null) GameManager.Instance.AddScore(1000, transform.position + Vector3.up);

            // 💡 이미 무적 상태인데 별을 또 먹었을 경우를 대비해 시간 리셋
            // StopAllCoroutines() 는 피격 무적 깜빡임·변신 연출·클리어 시퀀스까지 함께 죽여서
            // 마리오가 투명해지거나 무적이 풀리지 않는 상태로 남는 문제가 있었습니다.
            // 별 코루틴만 참조로 들고 있다가 그것만 정지시킵니다.
            if (starRoutine != null) StopCoroutine(starRoutine);
            starRoutine = StartCoroutine(StarTextureSwapRoutine());
        }
    }

    // startInvincible 파라미터 추가
    IEnumerator TransformationSequence(string oldState, string newState, Sprite oldSprite, Sprite newSprite, bool startInvincible)
    {
        // 1. 게임 일시 정지 및 애니메이터 비활성화
        Time.timeScale = 0f;
        anim.enabled = false;

        if (fireMarioBody != null) fireMarioBody.SetActive(false);
        mainSr.enabled = true;

        // 2. 3번 깜빡이는 연출 (큰 -> 작은 -> 큰 -> 작은 교차)
        for (int i = 0; i < 3; i++)
        {
            mainSr.sprite = newSprite; // 작은 모습
            yield return new WaitForSecondsRealtime(0.1f);

            mainSr.sprite = oldSprite; // 큰 모습
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // 3. 최종 상태 적용 및 게임 재개
        anim.enabled = true;
        isSuper = (newState == "Super" || newState == "Fire");

        ChangeMarioState(newState);

        Time.timeScale = 1f; // 게임 재개

        // 게임이 다시 돌아가기 시작할 때, 필요하다면 무적 깜빡임 시작!
        if (startInvincible)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    void ShootFireball()
    {
        if (fireballPrefab == null || fireballSpawnPos == null) return;

        // 파이어볼 생성
        GameObject fireballGo = Instantiate(fireballPrefab, fireballSpawnPos.position, Quaternion.identity);
        Fireball fireball = fireballGo.GetComponent<Fireball>();
        StartCoroutine(ShootingAnimationRoutine());
        if (fireball != null)
        {
            // 마리오가 바라보는 방향에 맞춰 파이어볼 초기화 (dirRight 플래그 활용)
            int dir = dirRight ? 1 : -1;
            fireball.Init(dir);
        }
    }

    IEnumerator ShootingAnimationRoutine()
    {
        // 💡 오직 상체(upperAnim)에만 명령을 내립니다. 하체는 계속 뛰고 있습니다!
        upperAnim.SetBool("isShooting", true);

        // 0.15초 동안 손 뻗은 자세 유지
        yield return new WaitForSeconds(0.15f);

        upperAnim.SetBool("isShooting", false);
    }

    // 몬스터를 밟았을 때 튕겨오르는 함수 (Goomba.cs에서 호출함)
    public void StompBounce()
    {
        if (isStarInvincible) return;
        // PPU가 16일 때 280f는 일반 제자리 점프 높이입니다. 
        // 밟았을 때는 살짝 낮게 튀어오르도록 200f 정도로 줍니다. (조절 가능)
        float bounceSpeed = 200f / PPU;

        rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, bounceSpeed);

        // 공중 상태 플래그 처리
        grounded = false;
        anim.SetBool("grounded", false);
        if (currentState == "Fire") { upperAnim.SetBool("grounded", false); lowerAnim.SetBool("grounded", false); }

        // 밟을 때 나는 특유의 사운드를 넣으셔도 좋습니다.
        // AudioSource.PlayClipAtPoint(stompClip, transform.position);
    }

    public void TakeDamage()
    {
        if (isInvincible || isDead || isStarInvincible) return;

        if (isInvincible || isDead) return;

        if (currentState == "Fire")
        {
            // 마지막 파라미터를 true로 설정 -> 무적 코루틴 체이닝
            StartCoroutine(TransformationSequence("Fire", "Super", idleFire, idleSuper, true));
            // StartCoroutine(InvincibilityRoutine()); 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.pipeWarpSound);
        }
        else if (currentState == "Super")
        {
            // 마지막 파라미터를 true로 설정 -> 무적 코루틴 체이닝
            StartCoroutine(TransformationSequence("Super", "Small", idleSuper, idleSmall, true));
            // StartCoroutine(InvincibilityRoutine());
            SoundManager.Instance.PlaySFX(SoundManager.Instance.pipeWarpSound);
        }
        else if (currentState == "Small")
        {
            Die();
        }
    }

    // 피격 후 무적 깜빡임 코루틴
    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        // 약 2초 동안 깜빡임
        while (elapsed < 2f)
        {
            // 💡 현재 상태에 따라 깜빡거려야 할 오브젝트를 다르게 지정
            if (currentState == "Fire" && fireMarioBody != null)
                fireMarioBody.SetActive(!fireMarioBody.activeSelf);
            else
                mainSr.enabled = !mainSr.enabled;

            yield return new WaitForSeconds(0.1f); // 0.1초 간격
            elapsed += 0.1f;
        }

        // 코루틴이 끝날 때 확실하게 켜줌
        if (currentState == "Fire" && fireMarioBody != null) fireMarioBody.SetActive(true);
        else mainSr.enabled = true;

        isInvincible = false;  // 무적 해제
    }

    // 사망 처리 함수
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. 모든 애니메이션 정지 및 본체 켜기
        anim.enabled = false;
        if (upperAnim != null) upperAnim.enabled = false;
        if (lowerAnim != null) lowerAnim.enabled = false;

        fireMarioBody.SetActive(false);
        mainSr.enabled = true;

        // 2. 사망 스프라이트로 교체
        mainSr.sprite = deadSprite;

        // 3. 충돌체 끄기 (바닥을 뚫고 떨어지게 만듦)
        col.enabled = false;
        if (headCol != null) headCol.enabled = false;
        if (groundCheck != null) groundCheck.gameObject.SetActive(false); // 바닥 체크 기능도 끔


        // 죽었을 때 목숨 하나 깎기
        // if (GameManager.Instance != null)
        // {
        //     GameManager.Instance.LoseLife();
        // }
        // 4. 원작 특유의 사망 연출: 위로 붕~ 튀어 올랐다가 밑으로 떨어짐
        // PPU 설정에 맞춰 15f 정도 주면 적당히 튀어오릅니다.
        rigidbody2D.velocity = new Vector2(0, 5f);

        rigidbody2D.gravityScale = 2f;

        // 배경음악을 끄고 사망 효과음을 여기서 재생합니다.
        SoundManager.Instance.PlayBGM(SoundManager.Instance.deathBGM, false);

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(3f); // 3초 대기

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife(); // 이때 전환 씬으로 넘어감
        }
    }

    // 색깔 틴트가 아닌 그림(Texture) 자체를 아주 빠르게 스와핑합니다.
    IEnumerator StarTextureSwapRoutine()
    {
        isStarInvincible = true;

        // 💡 발사 직후(0.15초 안에) 별을 먹으면 상체가 Upper_Shoot 상태에 남아
        //    다른 아틀라스 스프라이트에 스타 텍스처가 덮여 깨져 보입니다.
        //    별이 시작되는 순간 발사 모션을 확실히 내려줍니다.
        if (upperAnim != null) upperAnim.SetBool("isShooting", false);

        float elapsed = 0f;
        int paletteIndex = 0;

        // 스타 텍스처 배열이 비어있으면 에러 방지를 위해 리턴
        if (starPaletteTextures == null || starPaletteTextures.Length == 0)
        {
            Debug.LogError("스타 팔레트 텍스처를 인스펙터에 넣어주세요!");
            isStarInvincible = false;
            starRoutine = null;
            yield break;
        }

        // 🎵 (선택) 스타 배경음악 재생
        SoundManager.Instance.PlayBGM(SoundManager.Instance.starBGM);

        // 스타 상태가 되면 Player와 Enemy의 물리 충돌을 무시합니다. (유령화)
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        while (elapsed < starDuration)
        {
            // 일반/슈퍼 마리오일 때
            if (starPaletteTextures != null && starPaletteTextures.Length > 0)
            {
                currentStarTexture = starPaletteTextures[paletteIndex % starPaletteTextures.Length];
            }

            // 파이어 마리오일 때 (상/하체 각각 맞는 배열에서 꺼내옴)
            if (fireUpperStarTextures != null && fireUpperStarTextures.Length > 0)
            {
                currentUpperStarTexture = fireUpperStarTextures[paletteIndex % fireUpperStarTextures.Length];
            }
            if (fireLowerStarTextures != null && fireLowerStarTextures.Length > 0)
            {
                currentLowerStarTexture = fireLowerStarTextures[paletteIndex % fireLowerStarTextures.Length];
            }

            paletteIndex++;
            yield return new WaitForSecondsRealtime(0.05f);
            elapsed += 0.05f;
        }

        // 시간이 다 끝나면 지시를 거둠 (원상복구)
        currentStarTexture = null;
        currentUpperStarTexture = null;
        currentLowerStarTexture = null;
        RemoveStarTexture();

        //ApplyStarTexture(defaultTexture); 
        isStarInvincible = false;

        // 스타 상태가 끝나면 충돌 무시를 해제하여 다시 맞을 수 있게 합니다.
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        // 🎵 (선택) 원래 배경음악으로 복구
        if (GameManager.Instance.timeLeft <= 100)
        {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldFastBGM);
        }
        else
        {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldBGM);
        }

        starRoutine = null; // 정상 종료했으므로 참조를 비움
    }

    // 💡 [수정] 실제 스프라이트 렌더러의 텍스처를 바꾸는 함수 (파라미터 3개로 늘어남)
    void ApplyStarTexture(Texture2D mainTex, Texture2D upperTex, Texture2D lowerTex)
    {
        // ① 작은/슈퍼 마리오일 때
        if (mainSr.enabled)
        {
            SwapMainTex(mainSr, mainTex);
        }

        // ② 파이어 마리오일 때 (상/하체 분리해서 칠해줌)
        if (fireMarioBody.activeSelf)
        {
            if (upperAnim != null) SwapMainTex(upperAnim.GetComponent<SpriteRenderer>(), upperTex);
            if (lowerAnim != null) SwapMainTex(lowerAnim.GetComponent<SpriteRenderer>(), lowerTex);
        }
    }

    // 💡 스프라이트의 UV(textureRect)는 자기 원본 아틀라스 크기를 기준으로 잡혀 있습니다.
    //    그래서 _MainTex 를 갈아끼우려면 교체 텍스처가 원본과 크기·배치가 같아야 하고,
    //    다르면 엉뚱한 영역을 샘플링해서 스프라이트가 깨져 보입니다.
    //    (실제 사례: 상체 발사 모션만 throwfireball.png(34x32)를 쓰는데
    //     스타 상체 팔레트는 fire_upper.png(96x24) 리컬러여서 상체가 깨졌습니다.)
    //    크기가 맞는 프레임에만 스타 색을 입히고, 아니면 원본 그림을 그대로 보여줍니다.
    void SwapMainTex(SpriteRenderer sr, Texture2D starTex)
    {
        if (sr == null) return;

        Sprite cur = sr.sprite;
        bool sameAtlas = starTex != null && cur != null && cur.texture != null
                         && cur.texture.width == starTex.width
                         && cur.texture.height == starTex.height;

        if (!sameAtlas)
        {
            sr.SetPropertyBlock(null); // 덮어쓰기 해제 → 애니메이터의 원본 그림이 그대로 나옴
            return;
        }

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetTexture("_MainTex", starTex);
        sr.SetPropertyBlock(mpb);
    }

    // 코팅을 벗겨내고 원래 그림으로 되돌리는 함수
    void RemoveStarTexture()
    {
        // SetPropertyBlock(null)을 주면, 우리가 코드로 강제로 덮어씌웠던 색깔 코팅이 싹 날아가고
        // 애니메이터가 원래 가지고 있던 기본 그림이 자연스럽게 다시 나타납니다.
        if (mainSr != null) mainSr.SetPropertyBlock(null);
        if (upperAnim != null) upperAnim.GetComponent<SpriteRenderer>().SetPropertyBlock(null);
        if (lowerAnim != null) lowerAnim.GetComponent<SpriteRenderer>().SetPropertyBlock(null);
    }

    // 몸통 레이더 스크립트 작성(스타)
    void CheckStarEnemyCollision()
    {
        // 마리오의 상태(Small, Super)에 따라 몸통 박스의 높이를 결정합니다.
        float boxHeight = (currentState == "Small") ? 1.0f : 2.0f;

        // 마리오의 중심 위치 (y축 보정)
        Vector2 centerPos = new Vector2(transform.position.x, transform.position.y + (boxHeight / 2f) - 0.5f);

        // 마리오 몸통 크기만한 레이더 빔(OverlapBox)을 발사합니다. (약간 넉넉하게 잡음)
        Vector2 boxSize = new Vector2(0.8f, boxHeight);

        // 감지된 모든 Enemy를 가져옵니다. (여러 마리 동시에 치일 수 있으므로 All 사용)
        Collider2D[] enemies = Physics2D.OverlapBoxAll(centerPos, boxSize, 0f, LayerMask.GetMask("Enemy"));

        foreach (Collider2D enemyCol in enemies)
        {
            IEnemy enemy = enemyCol.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.HitByFireball(); // 적을 날려버림
                // (선택) 걷어찰 때 나는 사운드 (킥 사운드가 있다면 재생)
                // SoundManager.Instance.PlaySFX(SoundManager.Instance.kickSound); 
            }
        }
    }

    // FlagPole 스크립트에서 호출
    public void StartClearSequence(FlagPole pole, Vector3 castlePos)
    {
        isClearing = true;

        // 플레이어 물리 초기화 및 중력 무시 (직선으로 하강하기 위함)
        originalGravity = rigidbody2D.gravityScale;
        rigidbody2D.velocity = Vector2.zero;
        rigidbody2D.gravityScale = 0f;

        // 애니메이션 속도를 0으로 만들어 잡고 있을 때 가만히 있도록 함 (애니메이션 정지)
        if (anim != null) anim.SetFloat("moveSpeed", 0f);
        if (upperAnim != null) upperAnim.SetFloat("moveSpeed", 0f);
        if (lowerAnim != null) lowerAnim.SetFloat("moveSpeed", 0f);
        SoundManager.Instance.PlayBGM(SoundManager.Instance.clearBGM, false);
        StartCoroutine(ClearRoutine(pole, castlePos));
    }

    IEnumerator ClearRoutine(FlagPole pole, Vector3 castlePos)
    {
        // 닿은 순간의 X 위치(깃대 왼쪽) 고정, Y축으로만 하강
        float startX = transform.position.x;
        float bottomY = pole.bottomPos.position.y;
        Transform flag = pole.flag;

        // 마리오와 깃발이 각각 도착했는지 확인하는 변수
        bool marioReached = false;
        bool flagReached = flag == null; // 깃발이 없으면 바로 도착한 것으로 처리

        // 1. 마리오와 깃발이 '각자의 위치'에서 바닥까지 일정한 속도로 내려옴
        // 둘 다 바닥에 도착할 때까지 반복
        while (!marioReached || !flagReached)
        {
            // 마리오 하강
            if (!marioReached)
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(startX, bottomY, transform.position.z), slideDownSpeed * Time.deltaTime);
                if (transform.position.y <= bottomY) marioReached = true;
            }

            // 깃발 하강 (맨 위에서부터 내려옴)
            if (!flagReached && flag != null)
            {
                flag.position = Vector3.MoveTowards(flag.position, new Vector3(flag.position.x, bottomY, flag.position.z), slideDownSpeed * Time.deltaTime * 2);
                if (flag.position.y <= bottomY) flagReached = true;
            }

            yield return null;
        }

        // 오차 보정을 위해 완전히 바닥 좌표로 맞춤
        transform.position = new Vector3(startX, bottomY, transform.position.z);
        if (pole.flag != null) pole.flag.position = new Vector3(pole.flag.position.x, bottomY, pole.flag.position.z);

        yield return new WaitForSeconds(0.1f); // 바닥에 닿고 아주 잠깐 대기

        // 깃대 오른쪽으로 위치 이동 및 Flip (방향 전환)
        transform.position = new Vector3(pole.transform.position.x + 0.5f, transform.position.y, transform.position.z);

        // (현재 프로젝트의 Flip 방식이 localScale.x를 양수로 만드는 것이라 가정)
        // if (transform.localScale.x < 0)
        // {
        //     transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        // }
        // 만약 SpriteRenderer의 flipX를 사용 중이라면: 
        mainSr.flipX = true;
        // 파이어 마리오 상체/하체 SpriteRenderer 가져와서 뒤집기
        if (upperAnim != null)
        {
            SpriteRenderer upperSr = upperAnim.GetComponent<SpriteRenderer>();
            if (upperSr != null) upperSr.flipX = true;
        }

        if (lowerAnim != null)
        {
            SpriteRenderer lowerSr = lowerAnim.GetComponent<SpriteRenderer>();
            if (lowerSr != null) lowerSr.flipX = true;
        }

        yield return new WaitForSeconds(0.2f); // 플립 후 잠깐 대기
        mainSr.flipX = false;
        // 파이어 마리오 상체/하체 SpriteRenderer 가져와서 뒤집기
        if (upperAnim != null)
        {
            SpriteRenderer upperSr = upperAnim.GetComponent<SpriteRenderer>();
            if (upperSr != null) upperSr.flipX = false;
        }

        if (lowerAnim != null)
        {
            SpriteRenderer lowerSr = lowerAnim.GetComponent<SpriteRenderer>();
            if (lowerSr != null) lowerSr.flipX = false;
        }

        // 독자 물리 엔진 호출을 위해 플래그 온!
        isClearJumping = true;

        if (anim != null) anim.SetTrigger("Jump");
        if (upperAnim != null) upperAnim.SetTrigger("Jump");
        if (lowerAnim != null) lowerAnim.SetTrigger("Jump");

        // 인스펙터에서 설정한 점프 힘 적용
        rigidbody2D.velocity = jumpOffForce;

        yield return new WaitForSeconds(0.2f); // 상승하는 동안 대기

        // 다시 바닥(bottomY) 근처로 떨어져서 착지할 때까지 대기
        while (grounded)
        {
            yield return null;
        }

        // 바닥에 닿았으므로 다시 물리 엔진 수동 제어 모드로 변경
        isClearJumping = false;

        // Y축 속도만 강제 고정 (통통 튀는 것 방지)
        rigidbody2D.velocity = new Vector2(walkToCastleSpeed, 0);

        // 걷기 애니메이션 강제 재생을 위한 파라미터 세팅
        if (anim != null)
        {
            anim.SetFloat("moveSpeed", walkToCastleSpeed);
            // 프로젝트에서 바닥 감지 파라미터 이름이 "isGrounded" 또는 "Grounded" 인지 확인 후 맞춰주세요.
            anim.SetBool("grounded", true); // 강제로 바닥에 있다고 인식시킴
        }
        if (upperAnim != null)
        {
            upperAnim.SetFloat("moveSpeed", walkToCastleSpeed);
            upperAnim.SetBool("grounded", true);
        }
        if (lowerAnim != null)
        {
            lowerAnim.SetFloat("moveSpeed", walkToCastleSpeed);
            lowerAnim.SetBool("grounded", true);
        }

        while (transform.position.x < castlePos.x)
        {
            // 실제 Rigidbody 속도를 제어하여 이동시킴
            rigidbody2D.velocity = new Vector2(walkToCastleSpeed, rigidbody2D.velocity.y);
            yield return new WaitForFixedUpdate(); // 물리를 다루므로 FixedUpdate 타이밍에 맞춤
        }

        // 성에 도착
        rigidbody2D.velocity = Vector2.zero;
        if (anim != null) anim.SetFloat("moveSpeed", 0f);

        // 단순히 모습 숨기기
        if (mainSr != null) mainSr.enabled = false;
        if (fireMarioBody != null) fireMarioBody.SetActive(false);

        // 시간 -> 점수 정산 시작
        if (GameManager.Instance != null)
        {
            StartCoroutine(GameManager.Instance.CalculateTimeScoreRoutine(castleFlagAnim));
        }
    }

    public void StartWarp()
    {
        isWarping = true;
        rigidbody2D.velocity = Vector2.zero; // 현재 속도 초기화

        // 파이프 들어가는 동안 걷기 애니메이션이 나오게 하려면 speed를 살짝 줌
        if (anim != null)
        {
            anim.SetFloat("moveSpeed", 0f);
        }
        if (mainSr != null)
        {
            originalSortingLayerName = mainSr.sortingLayerName;
            originalSortingOrder = mainSr.sortingOrder;
        }
        ChangeSortingLayer("BackGround", 1);
    }

    public void EndWarp()
    {
        isWarping = false;
        if (anim != null) anim.SetFloat("moveSpeed", 0f);

        ChangeSortingLayer(originalSortingLayerName, originalSortingOrder);
    }

    private void ChangeSortingLayer(string layerName, int order)
    {
        // 1. 기본 몸통 (작은 마리오, 슈퍼 마리오)
        if (mainSr != null)
        {
            mainSr.sortingLayerName = layerName;
            mainSr.sortingOrder = order;
        }

        // 2. 파이어 마리오 상체
        if (upperAnim != null)
        {
            SpriteRenderer upperSr = upperAnim.GetComponent<SpriteRenderer>();
            if (upperSr != null)
            {
                upperSr.sortingLayerName = layerName;
                upperSr.sortingOrder = order;
            }
        }

        // 3. 파이어 마리오 하체
        if (lowerAnim != null)
        {
            SpriteRenderer lowerSr = lowerAnim.GetComponent<SpriteRenderer>();
            if (lowerSr != null)
            {
                lowerSr.sortingLayerName = layerName;
                lowerSr.sortingOrder = order;
            }
        }
    }
}

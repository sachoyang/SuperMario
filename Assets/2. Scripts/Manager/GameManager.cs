using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    [Header("Global Game Data (SMB1 Inspired)")]
    // 원작 HUD에 보이는 데이터들
    public int totalScore = 0;   // MARIO 옆 점수
    public int coins = 0;        // 동전 아이콘 옆
    public int lives = 3;        // 원작 타이틀엔 안 보이지만 플레이 시 필수 데이터
    public int world = 1;        // WORLD X-X
    public int stage = 1;        // WORLD X-X
    public float timeLeft = 400f; // TIME countdown (SMB1 기준 400초)

    // 전환 씬의 3가지 상태를 정의합니다.
    public enum TransitionType { LevelIntro, TimeUp, GameOver }
    [HideInInspector] public TransitionType currentTransition;
    public bool isWarningPlayed = false; // 경고음이 울렸는지 확인하는 스위치

    [Header("Scene Names")]
    public string tileSceneName = "scTitle"; // 타이틀 씬 이름
    public string firstLevelSceneName = "scMario1_1"; // 첫 번째 스테이지 씬 이름
    public string transitionSceneName = "scTransition"; // 전환 씬

    [Header("Prefabs")]
    public GameObject floatingScorePrefab; // 팝업 점수 프리팹
    public bool isGameActive = false; // 게임 플레이 중인지 체크 (타이머 작동용)

    private void Awake()
    {
        // [싱글톤 패턴 초기화]
        if (Instance == null)
        {
            Instance = this;
            // 이 오브젝트는 씬이 바뀌어도 절대 파괴되지 않음!
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 매니저가 존재한다면 새로 생성된 건 지워버림
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 게임 플레이 중일 때만 시간 카운트다운
        if (isGameActive && timeLeft > 0)
        {
            timeLeft -= Time.deltaTime; // 실제 시간(초)만큼 깎음

            // 💡 100초 감지 시 코루틴 실행으로 변경
            if (timeLeft <= 100 && !isWarningPlayed)
            {
                isWarningPlayed = true;
                StartCoroutine(TimeWarningRoutine());
            }

            if (timeLeft <= 0)
            {
                timeLeft = 0;
                // 타임 오버 처리 로직 (나중에 Die() 함수 호출 등 추가)
                Debug.Log("Time Over!");
                // GameObject player = GameObject.FindGameObjectWithTag("Player");
                // if (player != null) player.GetComponent<MarioCtrl>().Die();
                TimeOut();
            }
        }


        // F11 키를 누르면 창모드 <-> 전체화면 전환
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Screen.fullScreen 은 현재 전체화면인지 아닌지를 true/false로 가지고 있습니다.
            // 이것을 반대로(!) 뒤집어 줍니다.
            Screen.fullScreen = !Screen.fullScreen;
        }
    }

    // 💡 100초 경고 및 빠른 BGM 전환 코루틴
    private IEnumerator TimeWarningRoutine()
    {
        if (SoundManager.Instance != null)
        {
            // 1. 재생 중이던 원래 BGM을 끕니다.
            SoundManager.Instance.bgmSource.Stop();

            // 2. 100초 경고음 재생
            if (SoundManager.Instance.warningSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.warningSound);
                
                // 경고음 오디오 파일의 원래 길이(초)만큼 정확히 대기합니다.
                yield return new WaitForSecondsRealtime(SoundManager.Instance.warningSound.length);
            }

            // 3. 경고음이 끝나면 빠른 BGM으로 교체해서 재생합니다.
            if (SoundManager.Instance.overworldFastBGM != null)
            {
                SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldFastBGM);
            }
        }
    }

    // --- 데이터 변경 함수들 ---

    // 점수와 함께 띄울 위치를 전달받는 오버로딩 함수
    public void AddScore(int scoreToAdd, Vector3 spawnPosition)
    {
        totalScore += scoreToAdd; // 기존 점수 올리기

        // 프리팹이 등록되어 있다면 팝업 생성
        if (floatingScorePrefab != null)
        {
            GameObject floatingObj = Instantiate(floatingScorePrefab, spawnPosition, Quaternion.identity);
            FloatingScore fs = floatingObj.GetComponent<FloatingScore>();
            if (fs != null)
            {
                fs.Init(scoreToAdd); // 숫자 텍스트 변경
            }
        }
    }

    // 점수 추가 (굼바 밟기 등)
    public void AddScore(int scoreToAdd)
    {
        totalScore += scoreToAdd;
    }

    // 동전 추가 (100개 모으면 목숨+1 로직도 추가 가능)
    public void AddCoin(int coinToAdd)
    {

        coins += coinToAdd;
        if (coins >= 100)
        {
            coins = 0;
            GameObject mario = GameObject.FindWithTag("Player");
            if (mario != null)
            {
                AddLife(mario.transform.position + Vector3.up * 2.0f);
            }
            else
            {
                // 마리오를 못 찾을 경우를 대비한 기본 처리
                AddLife(Vector3.zero);
            }
        }
    }

    public void AddLife(Vector3 spawnPosition)
    {
        lives++; // 생명 1 증가

        // 1UP 사운드가 있다면 여기서 재생
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.oneUpSound);

        // 프리팹이 등록되어 있다면 1UP 팝업 생성
        if (floatingScorePrefab != null)
        {
            GameObject floatingObj = Instantiate(floatingScorePrefab, spawnPosition, Quaternion.identity);
            FloatingScore fs = floatingObj.GetComponent<FloatingScore>();

            if (fs != null)
            {
                // 💡 기존의 int형 Init() 대신, 문자열을 받는 함수를 호출해야 합니다.
                fs.InitText("1UP");
            }
        }
    }

    // 목숨 감소
    public void LoseLife()
    {
        lives--;
        if (lives <= 0)
        {
            // 게임 오버 처리 로직 (타이틀로 돌아가기 등)
            //SceneManager.LoadScene(tileSceneName);
            //ResetGameData(); // 데이터 초기화\
            SoundManager.Instance.PlayBGM(SoundManager.Instance.gameoverBGM);
            LoadTransition(TransitionType.GameOver);
        }
        else
        {
            //SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldBGM);
            LoadTransition(TransitionType.LevelIntro);
        }
    }

    // 시간 초과 시 호출될 함수
    public void TimeOut()
    {
        LoadTransition(TransitionType.TimeUp);
    }

    // 상태를 세팅하고 전환 씬을 부르는 전용 함수
    public void LoadTransition(TransitionType type)
    {
        currentTransition = type;
        isGameActive = false; // 타이머 일시정지
        SceneManager.LoadScene(transitionSceneName);
    }

    // --- 게임 흐름 제어 함수들 ---

    // 타이틀에서 게임을 시작할 때 호출
    public void StartNewGame(int numberOfPlayers)
    {
        Debug.Log("Start New Game for " + numberOfPlayers + " Player(s)!");
        ResetGameData(); // 혹시 남아있을 데이터 초기화
        isGameActive = true;
        // 첫 번째 레벨 씬 로드
        LoadTransition(TransitionType.LevelIntro);
        //SceneManager.LoadScene(firstLevelSceneName);
    }

    // 데이터를 처음 상태로 리셋
    private void ResetGameData()
    {
        totalScore = 0;
        coins = 0;
        lives = 3;
        world = 1;
        stage = 1;
        timeLeft = 400f;
        isGameActive = false;

        isWarningPlayed = false;
    }

    public IEnumerator CalculateTimeScoreRoutine(Animator flagAnim)
    {
        isGameActive = false; // 타임 감소 타이머 정지

        while (timeLeft > 0)
        {
            timeLeft--;
            totalScore += 10; // 1초당 10점

            // (선택) 틱틱거리는 사운드 재생
            // if (tickSound != null) AudioSource.PlayClipAtPoint(tickSound, Camera.main.transform.position);

            yield return new WaitForSecondsRealtime(0.01f); // 점수가 빠르게 올라가도록 짧은 대기
        }

        yield return new WaitForSeconds(0.1f); // 계산 끝나고 아주 잠깐 여운을 줌

        // 택배로 넘겨받은 깃발 애니메이터를 실행합니다.
        if (flagAnim != null)
        {
            flagAnim.SetTrigger("Raise");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.itemAppearSound);
        }

        // 3. 깃발이 다 올라올 때까지 대기 (애니메이션 길이에 맞추세요. 예: 1초)
        yield return new WaitForSeconds(2f);

        // 기존에 저장된 최고 점수를 불러옴 (없으면 0)
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);

        // 현재 점수가 더 높으면 갱신
        if (totalScore > currentHighScore)
        {
            PlayerPrefs.SetInt("HighScore", totalScore);
            PlayerPrefs.Save(); // 디스크에 즉시 저장
        }

        // 💡 4. 타이틀 씬으로 이동
        SceneManager.LoadScene("scTitle");

        // 다음 씬 전환 (예시: Transition 씬으로 이동)
        //LoadTransition(TransitionType.LevelIntro);
    }
}
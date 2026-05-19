using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Legacy UI 사용

public class TitleMenuManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }

    [Header("Cursor Settings")]
    public RectTransform cursorTransform; // 버섯 커서의 위치 (RectTransform)
    public RectTransform player1Text;     // "1 PLAYER GAME" 텍스트의 위치
    public RectTransform player2Text;     // "2 PLAYER GAME" 텍스트의 위치
    public float cursorXOffset = -30f;    // 텍스트 기준 왼쪽에 얼마나 띄울지 (해상도에 따라 조절)

    [Header("Score Settings")]
    public Text highScoreText;            // 💡 최고 점수를 표시할 텍스트 UI 변수 추가
    private int selectedOption = 1; // 1: 1 Player, 2: 2 Player

    private void Start()
    {
        // 💡 씬이 시작될 때 저장된 최고 점수를 불러와서 텍스트를 업데이트합니다.
        UpdateHighScoreDisplay();
    }

    private void Update()
    {
        // 1. 키보드 입력 처리 (W/S 또는 위/아래 방향키)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (selectedOption == 2) selectedOption = 1;
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fireballSound);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (selectedOption == 1) selectedOption = 2;
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fireballSound);
        }

        // 2. 커서 위치 업데이트
        UpdateCursorPosition();

        // 3. 엔터(Enter) 키 입력 시 게임 시작
        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return))
        {
            if (GameManager.Instance != null)
            {

                // GameManager에게 몇 인용인지 전달하며 게임 시작 씬 전환
                GameManager.Instance.StartNewGame(selectedOption);
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerPrefs.DeleteKey("HighScore"); // HighScore 데이터만 콕 집어서 삭제
            // PlayerPrefs.DeleteAll(); // 만약 저장된 모든 데이터를 날리고 싶다면 이것을 사용
            Debug.Log("최고 점수가 초기화되었습니다!");
            UpdateHighScoreDisplay();
        }
    }

    void UpdateCursorPosition()
    {
        Vector2 targetPosition;

        // 선택한 옵션에 따라 타겟 텍스트의 Y축 위치를 가져옴
        if (selectedOption == 1)
        {
            targetPosition = player1Text.anchoredPosition;
        }
        else
        {
            targetPosition = player2Text.anchoredPosition;
        }

        // X축 오프셋을 적용해 커서를 텍스트 왼쪽에 배치
        targetPosition.x += cursorXOffset;

        // 버섯 커서를 최종 위치로 순간이동
        cursorTransform.anchoredPosition = targetPosition;
    }

    // 최고 점수를 가져와서 UI에 반영하는 전용 함수
    public void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            // PlayerPrefs에 저장된 "HighScore" 키값을 가져옵니다. (저장된 게 없으면 0)
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            
            // "TOP- 000000" 형태로 6자리 숫자를 맞춰서 표시합니다.
            highScoreText.text = highScore.ToString("D6");
        }
    }
}
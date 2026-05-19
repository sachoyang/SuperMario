using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 💡 TMPro 대신 기본 UI 네임스페이스 사용!

public class HUDManager : MonoBehaviour
{
    [Header("HUD Text Elements")]
    // 💡 TMP_Text 대신 레거시 Text 컴포넌트 사용
    public Text scoreText;    // MARIO 아래 점수 (6자리)
    public Text coinText;     // 동전 아이콘 옆 (2자리)
    public Text worldText;    // WORLD 아래 스테이지 (X-X)
    public Text timeText;     // TIME 아래 숫자 (3자리 정수)

    private void Update()
    {
        // 씬 안에 살아있는 유일한 GameManager 인스턴스를 찾아 데이터를 가져옵니다.
        if (GameManager.Instance != null)
        {
            // 1. 점수 업데이트 (D6: 6자리 숫자로 만들고 빈칸은 0으로 채움)
            scoreText.text = GameManager.Instance.totalScore.ToString("D6");

            // 2. 동전 업데이트 (D2: 2자리 숫자로 만듦)
            coinText.text = "×" + GameManager.Instance.coins.ToString("D2");

            // 3. 월드 업데이트 (X-X 포맷)
            worldText.text = GameManager.Instance.world + "-" + GameManager.Instance.stage;

            // 4. 시간 업데이트 (소수점을 떼어내고 3자리 정수로 만듦)
            int displayTime = Mathf.CeilToInt(GameManager.Instance.timeLeft);
            timeText.text = displayTime.ToString("D3");
        }
    }
}
    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject timeUpPanel;
    public GameObject gameOverPanel;

    [Header("Intro Elements")]
    public Text worldText;
    public Text livesText;

    private void Start()
    {
        // 1. 일단 모든 패널을 끕니다.
        introPanel.SetActive(false);
        timeUpPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // 2. GameManager의 현재 상태를 확인합니다.
        if (GameManager.Instance != null)
        {
            GameManager.TransitionType type = GameManager.Instance.currentTransition;
            Debug.Log("현재 전환 타입: " + type); // 💡 로그 확인용

            if (type == GameManager.TransitionType.LevelIntro)
            {
                introPanel.SetActive(true);
                worldText.text = "WORLD  " + GameManager.Instance.world + "-" + GameManager.Instance.stage;
                livesText.text = "x  " + GameManager.Instance.lives;
                StartCoroutine(WaitAndLoad(GameManager.Instance.firstLevelSceneName, 3f)); // 3초 뒤 게임 시작
                //SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldBGM);
            }
            else if (type == GameManager.TransitionType.TimeUp)
            {
                timeUpPanel.SetActive(true);
                StartCoroutine(HandleTimeUp());
                //SoundManager.Instance.PlayBGM(SoundManager.Instance.deathBGM);
            }
            else if (type == GameManager.TransitionType.GameOver)
            {
                gameOverPanel.SetActive(true);
                StartCoroutine(WaitAndLoad(GameManager.Instance.tileSceneName, 4f)); // 4초 뒤 타이틀로
                SoundManager.Instance.PlayBGM(SoundManager.Instance.gameoverBGM);
            }
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    IEnumerator WaitAndLoad(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 💡 게임 씬으로 돌아갈 때만 타이머를 다시 켜줍니다.
        if (sceneName == GameManager.Instance.firstLevelSceneName)
        {
            GameManager.Instance.timeLeft = 400f; // 시간 초기화 (원한다면)
            GameManager.Instance.isGameActive = true; // (이건 나중에 게임씬 로드 완료 후 켜는게 안전합니다)
        }
        
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator HandleTimeUp()
    {
        yield return new WaitForSeconds(3f); // TIME UP 3초 보여줌
        
        // 타임 업도 결국 죽은 거니까 목숨을 하나 깎습니다.
        // GameManager의 LoseLife()가 알아서 Intro로 갈지 GameOver로 갈지 결정해 줍니다!
        GameManager.Instance.LoseLife(); 
    }
}
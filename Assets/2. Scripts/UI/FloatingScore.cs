using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Legacy Text 사용

public class FloatingScore : MonoBehaviour
{
    public float moveSpeed = 2f;    // 위로 올라가는 속도
    public float destroyTime = 1f;  // 몇 초 뒤에 사라질지
    public Text scoreText;

    // 외부에서 점수를 입력받아 텍스트를 세팅하는 함수
    public void Init(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        // 지정된 시간 뒤에 오브젝트 파괴
        Destroy(gameObject, destroyTime);
    }

    public void InitText(string textToDisplay)
    {
        // 사용 중인 UI 컴포넌트에 맞게 주석을 해제해서 사용하세요.
        if (scoreText != null)
        {
            scoreText.text = textToDisplay;
        }

        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 매 프레임마다 위쪽(Vector3.up)으로 스르륵 이동
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}
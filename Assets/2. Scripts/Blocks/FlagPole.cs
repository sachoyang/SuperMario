using System.Collections;
using UnityEngine;

public class FlagPole : MonoBehaviour
{
    [Header("Flag Pole Settings")]
    public Transform bottomPos;     // 깃대 맨 아래 위치 (바닥 기준점)
    public Transform flag;          // 내려올 깃발 이미지 객체
    //public Transform flagbottom;
    public Transform castleDoorPos; // 마리오가 걸어갈 성문 위치

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;
            MarioCtrl mario = collision.GetComponent<MarioCtrl>();
            if (mario != null)
            {
                if (GameManager.Instance != null)
                {
                    // 1. 깃대 꼭대기와 마리오의 높이 차이 계산
                    // (transform.position.y가 깃대의 맨 위라고 가정합니다)
                    float poleTopY = transform.position.y+5.25f;
                    float marioY = collision.transform.position.y;

                    // 높이 차이가 0.5f 이하이면 꼭대기를 잡은 것으로 판정 (1UP!)
                    if (poleTopY - marioY <= 0.5f)
                    {
                        // 1UP 함수 호출 (마리오 머리 위로 1UP 텍스트 팝업)
                        GameManager.Instance.AddLife(collision.transform.position + Vector3.up * 1.0f);
                        GameManager.Instance.AddScore(2000);
                    }
                    else
                    {
                        // 그 아래를 잡았을 때: 기존처럼 높이에 비례하여 점수 계산
                        float heightRatio = Mathf.InverseLerp(bottomPos.position.y, poleTopY, marioY);
                        int score = Mathf.RoundToInt(Mathf.Lerp(1000f, 2000f, heightRatio) / 100f) * 100;

                        // AddScore 함수를 호출하여 점수와 팝업 처리
                        GameManager.Instance.AddScore(score, collision.transform.position + Vector3.up * 1.0f);
                    }
                }

                // 2. 마리오의 클리어 시퀀스 시작 (깃발 정보 전달)
                mario.StartClearSequence(this, castleDoorPos.position);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class BrickDebris : MonoBehaviour
{
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    // 블록 스크립트에서 호출하여 파편을 초기화하는 함수
    public void Init(Sprite sprite, Vector2 velocity, float torque)
    {
        GameManager.Instance.AddScore(50);
        sr.sprite = sprite;        // 해당 조각 스프라이트 설정
        rb.velocity = velocity;    // 초기 속도(방향+힘) 설정
        rb.AddTorque(torque, ForceMode2D.Impulse); // 회전 효과
        SoundManager.Instance.PlaySFX(SoundManager.Instance.breakblockSound);
        // 2초 뒤 자동 삭제
        Destroy(gameObject, 2f);
    }
}
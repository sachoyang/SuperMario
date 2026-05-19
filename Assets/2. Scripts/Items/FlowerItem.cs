using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerItem : MonoBehaviour
{
    // 이동할 필요가 없으므로 FixedUpdate가 없습니다.

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            MarioCtrl player = collision.gameObject.GetComponent<MarioCtrl>();
            if (player != null)
            {
                // 플레이어에게 파이어 마리오로 변신하라고 전달!
                player.TakeItem("Fire"); 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.powerUpSound);
                GameManager.Instance.AddScore(1000, transform.position + Vector3.up * 1.0f);
            }
            Destroy(gameObject); // 먹었으니 파괴
        }
    }
}
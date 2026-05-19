using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHead : MonoBehaviour
{
    private MarioCtrl playerCtrl;

    void Awake()
    {
        // 부모 오브젝트에 있는 PlayerCtrl 참조
        playerCtrl = GetComponentInParent<MarioCtrl>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Block"))
        {
            // 위로 상승 중일 때만 처리 (머리로 박았을 때)
            Rigidbody2D rb = playerCtrl.GetComponent<Rigidbody2D>();
            if (rb.velocity.y > 0)
            {
                collision.GetComponent<InteractableBlock>().OnHit(playerCtrl.isSuper);
                
                // 머리를 박았으니 상승 속도 정지
                rb.velocity = new Vector2(rb.velocity.x, 0);
            }
        }
    }
}
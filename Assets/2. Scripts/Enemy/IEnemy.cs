using UnityEngine;

// MonoBehaviour를 상속받지 않는 순수 인터페이스입니다.
public interface IEnemy
{
    void Squished();        // 밟혔을 때
    void HitFromBelow();    // 블록 밑에서 맞았을 때
    void HitByFireball();   // 파이어볼에 맞았을 때
}
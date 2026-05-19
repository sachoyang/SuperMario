using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WarpPipe : MonoBehaviour
{
    public enum PipeType { VerticalIn, HorizontalOut }

    [Header("Pipe Settings")]
    public PipeType pipeType;
    public Transform warpDestination; // 이동할 목적지 (빈 오브젝트)
    public float slideDuration = 1.0f; // 파이프 안으로 들어가는 시간

    [Header("Exit Settings (도착 후 연출)")]
    public bool doExitAnimation = true;
    // 💡 마리오가 파이프에서 빠져나올 방향 (기본값: 위로 솟아오름)
    public Vector3 exitDirection = Vector3.up;
    public float exitDistance = 1.5f; // 파이프에서 얼만큼 빠져나올지 거리

    [Header("Camera Warp Settings")]
    // 이 파이프를 타고 이동한 '후'의 카메라 설정입니다.
    public bool setCameraStatic;    // 이동 후 카메라 고정 여부
    public bool setCameraLockY;     // 이동 후 Y축 고정 여부 (지상 복귀 시 true)
    public Transform staticCameraPos; // 카메라 고정 모드일 때의 위치 (오브젝트로 받음)

    [Header("UI Settings")]
    public GameObject blackOverlay; // 위에서 만든 WarpOverlay 이미지를 여기에 할당하세요
    private bool isWarping = false;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isWarping) return;

        if (collision.CompareTag("Player"))
        {
            MarioCtrl mario = collision.GetComponent<MarioCtrl>();
            if (mario == null) return;

            // 1. 수직 파이프 (아래 키 입력을 통해 지하로 이동)
            if (pipeType == PipeType.VerticalIn && Input.GetAxisRaw("Vertical") < -0.5f)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.pipeWarpSound);
                StartCoroutine(WarpSequence(mario, Vector3.down));
            }
            // 2. 가로 파이프 (오른쪽 키 입력을 통해 지상으로 복귀)
            else if (pipeType == PipeType.HorizontalOut && Input.GetAxisRaw("Horizontal") > 0.5f)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.pipeWarpSound);
                StartCoroutine(WarpSequence(mario, Vector3.right));
            }
        }
    }

    IEnumerator WarpSequence(MarioCtrl mario, Vector3 moveDir)
    {
        isWarping = true;
        mario.StartWarp(); // 마리오 입력 및 중력 차단

        // 1. 파이프 안으로 스르륵 들어가는 연출
        Vector3 startPos = mario.transform.position;
        Vector3 endPos = startPos + (moveDir * 1.5f); // 1.5칸 정도 안으로 들어감

        float elapsed = 0;
        while (elapsed < slideDuration)
        {
            mario.transform.position = Vector3.Lerp(startPos, endPos, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // 블랙아웃
        if (blackOverlay != null) blackOverlay.SetActive(true);
        yield return new WaitForSeconds(0.5f); // 잠시 암전 느낌의 대기

        // 4. 목적지 파이프 '안쪽'으로 위치 이동 및 카메라 설정 적용
        if (warpDestination != null)
        {
            mario.transform.position = warpDestination.position;

            FollowCamera cam = Camera.main.GetComponent<FollowCamera>();
            if (cam != null)
            {
                Vector3? targetPos = null;
                if (setCameraStatic && staticCameraPos != null) targetPos = staticCameraPos.position;
                else if (setCameraLockY) targetPos = new Vector3(mario.transform.position.x, cam.fixedY, cam.transform.position.z);
                cam.ConfigureCamera(setCameraStatic, setCameraLockY, targetPos);
            }
        }
        yield return new WaitForSeconds(0.3f);

        // 4. 다시 화면을 보여줌
        if (blackOverlay != null) blackOverlay.SetActive(false);

        // 💡 6. 파이프에서 빠져나오는 연출 코루틴 (목적지가 세팅되어 있을 때만)
        if (warpDestination != null)
        {
            Vector3 exitStartPos = mario.transform.position;
            Vector3 exitEndPos = exitStartPos + (exitDirection * exitDistance);
            elapsed = 0;

            if (doExitAnimation)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.pipeWarpSound); // 나올 때도 워프 소리 재생

                // 나올 때는 꼿꼿이 서서 나오도록 애니메이션 파라미터 0으로 고정
                mario.GetComponent<Animator>().SetFloat("moveSpeed", 0f);

                while (elapsed < slideDuration)
                {
                    mario.transform.position = Vector3.Lerp(exitStartPos, exitEndPos, elapsed / slideDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (GameManager.Instance.timeLeft <= 100)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.overworldFastBGM);
                }
                else
                {
                    SoundManager.Instance.PlayBGM(SoundManager.Instance.overworldBGM);

                }
            }
            else
            {
                SoundManager.Instance.PlayBGM(SoundManager.Instance.underworldBGM);
            }


            // 오차 보정
            mario.transform.position = exitEndPos;
        }

        // 5. 워프 종료 및 조작 복구
        mario.EndWarp();
        isWarping = false;
    }
}
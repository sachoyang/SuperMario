using UnityEngine;

public class CameraResolution : MonoBehaviour
{
    private void Start()
    {
        // 💡 목표로 하는 화면 비율 (패미컴 원작 해상도 256 x 240)
        float targetWidth = 256f;
        float targetHeight = 240f;
        float targetRatio = targetWidth / targetHeight;

        // 현재 기기의 화면 비율
        float currentRatio = (float)Screen.width / Screen.height;

        // 비율을 비교하여 카메라의 Viewport Rect를 조절
        float scaleHeight = currentRatio / targetRatio;

        Camera camera = GetComponent<Camera>();
        Rect rect = camera.rect;

        if (scaleHeight < 1f) 
        {
            // 현재 화면이 목표 비율보다 세로로 길 때 (위아래 블랙바 생성)
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) / 2f;
        }
        else 
        {
            // 현재 화면이 목표 비율보다 가로로 길 때 (좌우 블랙바 생성)
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) / 2f;
            rect.y = 0f;
        }

        camera.rect = rect;
    }
}
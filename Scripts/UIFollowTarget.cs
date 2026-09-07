using UnityEngine;

public class UIFollowTarget : MonoBehaviour
{
    [Header("추적할 3D 대상")]
    public Transform target3D; // 캐릭터나 특정 3D 오브젝트의 Transform

    [Header("UI 위치 오프셋")]
    public Vector3 offset = new Vector3(0, 2.0f, 0); // Y축 오프셋

    private RectTransform rectTransform;
    private Camera mainCamera;
    private CanvasGroup canvasGroup; // SetActive 대신 알파값(투명도) 제어

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        // CanvasGroup 컴포넌트가 없으면 자동으로 추가하여 캐싱
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (target3D == null)
        {
            Debug.LogWarning($"{gameObject.name}에 추적할 Target3D가 지정되지 않았습니다.");
        }

        if (mainCamera == null)
        {
            Debug.LogError("MainCamera 태그가 설정된 카메라를 찾을 수 없습니다.");
        }
    }

    void LateUpdate()
    {
        // 대상이나 카메라가 없으면 UI를 안 보이게 처리 후 리턴
        if (target3D == null || mainCamera == null)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return;
        }

        // 1. 3D 오브젝트의 월드 좌표 + 오프셋 계산
        Vector3 targetWorldPos = target3D.position + offset;

        // 2. 카메라 전방과 대상 방향 벡터 연산
        Vector3 forward = mainCamera.transform.forward;
        Vector3 toTarget = targetWorldPos - mainCamera.transform.position;

        // 카메라 정면에 대상이 있을 때
        if (Vector3.Dot(forward, toTarget) > 0)
        {
            // UI 보이게 설정 (오브젝트는 켜둔 채 알파만 1)
            canvasGroup.alpha = 1f;

            // 3. 월드 좌표를 스크린 좌표로 변환하여 위치 업데이트
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);
            rectTransform.position = screenPos;
        }
        else
        {
            // 카메라 뒤로 넘어가면 알파만 0으로 숨김 (LateUpdate는 계속 동작함)
            canvasGroup.alpha = 0f;
        }
    }
}
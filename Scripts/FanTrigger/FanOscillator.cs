using UnityEngine;

public class FanOscillator : MonoBehaviour
{
    [Header("Oscillation Settings")]
    [Tooltip("중심을 기준으로 왕복할 각도입니다. (예: 45로 설정하면 좌우로 45도씩, 총 90도를 왕복합니다)")]
    public float oscillationAngle = 45f;

    [Tooltip("회전하는 속도입니다. 값이 클수록 선풍기가 빨리 돕니다.")]
    public float oscillationSpeed = 2f;

    [Tooltip("회전할 기준 축입니다. 보통 바닥에 놓인 선풍기는 Y축(0, 1, 0)을 기준으로 돕니다.")]
    public Vector3 rotationAxis = Vector3.up;

    private Quaternion startRotation; // 처음 배치된 방향(중심점)
    private float timer = 0f;

    void Start()
    {
        // 게임이 시작될 때 선풍기가 바라보고 있는 방향을 중심점으로 기억해둡니다.
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // 시간에 속도를 곱해서 타이머를 굴립니다.
        timer += Time.deltaTime * oscillationSpeed;

        // Mathf.Sin()은 시간이 지남에 따라 -1 에서 1 사이를 부드럽게 왔다 갔다 하는 값을 반환합니다.
        // 여기에 왕복할 각도(oscillationAngle)를 곱해주면, 예를 들어 -45도에서 +45도 사이를 부드럽게 오가게 됩니다.
        float currentAngle = Mathf.Sin(timer) * oscillationAngle;

        // 시작 회전값(중심점)을 기준으로, 지정된 축(rotationAxis)을 따라 currentAngle만큼 회전시킵니다.
        transform.localRotation = startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}
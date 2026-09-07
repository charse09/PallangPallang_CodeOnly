using UnityEngine;

// MonoBehaviour 뒤에 , IInteractable 을 추가하여 규칙을 따르겠다고 선언합니다.
public class SmoothRotator : MonoBehaviour, IInteractable
{
    [Header("Trigger Variable")]
    public bool isTriggered = false;

    [Header("Rotation Settings")]
    [SerializeField] private float targetRotationY = 90f;
    [SerializeField] private float rotationSpeed = 5f;

    private float currentY;
    private float targetY;
    private bool isDone = false;

    private void Start()
    {
        currentY = transform.localEulerAngles.y;
        targetY = currentY + targetRotationY;
    }

    private void Update()
    {
        if (isTriggered && !isDone)
        {
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * rotationSpeed);

            transform.localEulerAngles = new Vector3(
                transform.localEulerAngles.x,
                currentY,
                transform.localEulerAngles.z
            );

            if (Mathf.Abs(targetY - currentY) < 0.1f)
            {
                currentY = targetY;
                transform.localEulerAngles = new Vector3(
                    transform.localEulerAngles.x,
                    currentY,
                    transform.localEulerAngles.z
                );

                isDone = true;
            }
        }
    }

    // --- [새로 추가된 부분] ---
    // 외부(ClosestObjectFinder)에서 이 오브젝트를 찾아 이 함수를 실행하게 됩니다.
    public void Interact()
    {
        // 작동 스위치를 켭니다.
        isTriggered = true;
        Debug.Log($"[{gameObject.name}] SmoothRotator가 작동을 시작합니다!");
    }
}
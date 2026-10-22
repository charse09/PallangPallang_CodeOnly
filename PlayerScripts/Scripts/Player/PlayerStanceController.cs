using UnityEngine;

public class PlayerStanceController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;

    [SerializeField] private float standHeight = 18.0f;
    [SerializeField] private float standCameraLocalY = 1.6f;

    [SerializeField] private float crouchHeight = 12.0f;
    [SerializeField] private float crouchCameraLocalY = 0.8f;

    [SerializeField] private float stanceLerpSpeed = 10.0f; // 자세 전환 시 보간 속도.

    private bool isCrouching = false;
    private float currentControllerHeight = 2.0f;
    private float currentCameraHeight = 1.6f;

    

    void Update()
    {
        // HandleCrouchToggleInput();
        // UpdateStanceTransition();
    }

    /// <summary>
    /// 입력 리더에서 앉기 토글 입력을 받아서 자세 상태를 전환한다.
    /// </summary>
    void HandleCrouchToggleInput()
    {
        if (InputManager.Instance == null)
        {
            return;
        }

        if (InputManager.Instance.CrouchTogglePressed == true)
        {
            // ! : 논리 연산자인 NOT 연산자
            isCrouching = !isCrouching;
        }
    }

    /// <summary>
    /// 현재 자세 상태에 맞는 높이와 카메라 높이를 계산하고 보간 처리.
    /// </summary>
    void UpdateStanceTransition()
    {
        float targetHeight = isCrouching == true ? crouchHeight : standHeight;
        float targetCameraLocalY = isCrouching == true ? crouchCameraLocalY : standCameraLocalY;

        currentControllerHeight = characterController.height;
            //Mathf.Lerp(currentControllerHeight, targetHeight, stanceLerpSpeed * Time.deltaTime);
        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraLocalY, stanceLerpSpeed * Time.deltaTime);

        // 실제 적용.
        ApplyControllerHeight(currentControllerHeight);

        // ApplyCameraHeight(currentCameraHeight);
    }

    void ApplyControllerHeight(float targetHeight)
    {
        characterController.height = targetHeight;
        Vector3 currentCenter = characterController.center;
        float nextCenterY = targetHeight * 0.5f;
        Vector3 nextCenter = new Vector3(currentCenter.x, nextCenterY, currentCenter.z);
        characterController.center = nextCenter;
    }

    //void ApplyCameraHeight(float targetCameraY)
    //{
    //    Vector3 currentLocalPosition = cameraPivot.localPosition;
    //    Vector3 nextLocalPosition = new Vector3(currentLocalPosition.x, targetCameraY, currentLocalPosition.z);

    //    cameraPivot.localPosition = nextLocalPosition;
    //}

    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    public float CurrentControllerHeight
    {
        get { return currentControllerHeight; }
    }

    public float CurrentCameraLocalY
    {
        get { return currentCameraHeight; }
    }
}

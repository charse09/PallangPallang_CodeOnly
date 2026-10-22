using UnityEngine;

public class Wobble : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform walkerBody;

    [Header("Body Base Local Position")]
    [SerializeField] private Vector3 bodyBaseLocalPosition = new Vector3(0f, 1f, 0f);

    [Header("Wobble / Bounce Settings")]
    [SerializeField] private float stepFrequency = 4.5f;
    [SerializeField] private float bounceHeight = 0.14f;
    [SerializeField] private float tiltAngle = 12f;

    [Header("Smoothing")]
    [SerializeField] private float tiltSharpness = 12f;
    [SerializeField] private float bounceSharpness = 12f;

    [Header("Auto Wobble State")]
    public bool autoWobble = false; // 기본상태 false, true로 체크하면 자동으로 흔들림

    private float stepCycle;
    private float currentTilt;
    private float currentBounce;

    private void Start()
    {
        // walkerBody가 설정되지 않았다면 첫 번째 자식을 자동으로 할당
        if (walkerBody == null && transform.childCount > 0)
            walkerBody = transform.GetChild(0);

        if (walkerBody != null)
            walkerBody.localPosition = bodyBaseLocalPosition;
    }

    private void Update()
    {
        // 1. 외부 매니저로부터 입력값 확인
        Vector2 moveInput = InputManager.Instance.Movement;
        bool hasInput = moveInput.sqrMagnitude > 0.0001f;

        // [추가된 로직] 실제 입력이 있거나, 자동 흔들림(autoWobble)이 true일 때 작동
        bool shouldWobble = hasInput || autoWobble;

        // 2. 스텝 사이클 계산
        if (shouldWobble)
        {
            // 자동 흔들림 상태일 때 입력값이 없으면 magnitude가 0이 되어 멈추는 것을 방지
            // 입력이 없으면 기본 속도(1f)를 사용하고, 입력이 있으면 둘 중 큰 값을 사용합니다.
            float speedMultiplier = autoWobble ? Mathf.Max(1f, moveInput.magnitude) : moveInput.magnitude;

            stepCycle += Time.deltaTime * stepFrequency * speedMultiplier;
        }
        else
        {
            // 멈췄을 때 cycle을 0으로 서서히 보내거나 고정 (딱딱하게 멈추지 않도록)
            stepCycle = Mathf.MoveTowards(stepCycle, 0f, Time.deltaTime * stepFrequency);
        }

        // 3. 사인파를 이용한 흔들림 값 계산
        // Sin 함수를 통해 -1 ~ 1 사이를 왕복
        float wave = Mathf.Sin(stepCycle * Mathf.PI * 2f);

        // 이동 중(또는 자동 흔들림 켜짐)일 때만 타겟 값을 설정, 아니면 0으로 수렴
        float targetTilt = shouldWobble ? wave * tiltAngle : 0f;
        float targetBounce = shouldWobble ? Mathf.Abs(wave) * bounceHeight : 0f;

        // 4. 부드러운 보간 처리 (Lerp)
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSharpness * Time.deltaTime);
        currentBounce = Mathf.Lerp(currentBounce, targetBounce, bounceSharpness * Time.deltaTime);

        // 5. 비주얼 적용 (자식 오브젝트의 로컬 값만 변경)
        if (walkerBody != null)
        {
            // 상하 바운싱 적용
            walkerBody.localPosition = bodyBaseLocalPosition + Vector3.up * currentBounce;

            // 좌우 기울기(Tilt) 적용 (Z축 회전)
            // 부모의 회전값은 유지하면서 자식만 로컬로 흔들립니다.
            walkerBody.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
        }
    }
}
using UnityEngine;

public class EnemyWobble : MonoBehaviour
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
    public bool autoWobble = false;

    private float stepCycle;
    private float currentTilt;
    private float currentBounce;

    // [변경점] 플레이어의 Input 대신, 몬스터의 이동 속도를 저장할 변수
    private float moveMagnitude = 0f;

    private void Start()
    {
        if (walkerBody == null && transform.childCount > 0)
            walkerBody = transform.GetChild(0);

        if (walkerBody != null)
            walkerBody.localPosition = bodyBaseLocalPosition;
    }

    // [추가됨] 몬스터의 Locomotion 스크립트에서 매 프레임 이 함수를 호출해 속도를 전달합니다.
    public void SetMovementMagnitude(float magnitude)
    {
        moveMagnitude = magnitude;
    }

    private void Update()
    {
        // 1. 전달받은 속도값 확인 (InputManager 제거됨)
        bool hasMovement = moveMagnitude > 0.0001f;
        bool shouldWobble = hasMovement || autoWobble;

        // 2. 스텝 사이클 계산
        if (shouldWobble)
        {
            float speedMultiplier = autoWobble ? Mathf.Max(1f, moveMagnitude) : moveMagnitude;
            stepCycle += Time.deltaTime * stepFrequency * speedMultiplier;
        }
        else
        {
            stepCycle = Mathf.MoveTowards(stepCycle, 0f, Time.deltaTime * stepFrequency);
        }

        // 3. 사인파를 이용한 흔들림 값 계산
        float wave = Mathf.Sin(stepCycle * Mathf.PI * 2f);
        float targetTilt = shouldWobble ? wave * tiltAngle : 0f;
        float targetBounce = shouldWobble ? Mathf.Abs(wave) * bounceHeight : 0f;

        // 4. 부드러운 보간 처리 (Lerp)
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSharpness * Time.deltaTime);
        currentBounce = Mathf.Lerp(currentBounce, targetBounce, bounceSharpness * Time.deltaTime);

        // 5. 비주얼 적용
        if (walkerBody != null)
        {
            walkerBody.localPosition = bodyBaseLocalPosition + Vector3.up * currentBounce;
            walkerBody.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
        }
    }
}
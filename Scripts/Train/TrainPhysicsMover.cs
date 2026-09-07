using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class TrainPhysicsMover : MonoBehaviour
{
    public enum MovementMethod { Time, Speed }
    public enum LoopMode { LoopContinuous, PingPong, Once }

    [Header("Spline Setup")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Movement Settings")]
    [SerializeField] private bool playOnAwake = true;
    [Range(0f, 1f)]
    [SerializeField] private float startOffset = 0f;
    [SerializeField] private MovementMethod method = MovementMethod.Time;

    [Tooltip("Method가 Time일 때: 한 바퀴 도는 데 걸리는 시간(초)")]
    [SerializeField] private float duration = 30f;

    [Tooltip("Method가 Speed일 때: 이동 속도")]
    [SerializeField] private float speed = 10f;

    [SerializeField] private LoopMode loopMode = LoopMode.LoopContinuous;

    private Rigidbody rb;
    private float elapsedTime = 0f;
    private bool isPlaying = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 물리 연산 및 지터링 방지 필수 설정
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        if (playOnAwake) Play();
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;

    public void Restart()
    {
        elapsedTime = 0f;
        isPlaying = true;
    }

    void FixedUpdate()
    {
        if (!isPlaying || splineContainer == null) return;

        // 1. 진행 시간 업데이트
        elapsedTime += Time.fixedDeltaTime;

        // 2. 총 소요 시간(Total Duration) 계산
        float totalDuration = duration;
        if (method == MovementMethod.Speed)
        {
            float splineLength = splineContainer.CalculateLength();
            totalDuration = splineLength / Mathf.Max(speed, 0.001f);
        }

        // 3. 0~1 사이의 normalized time (t) 계산
        float progress = elapsedTime / Mathf.Max(totalDuration, 0.001f);
        float rawT = startOffset + progress;
        float t = 0f;

        switch (loopMode)
        {
            case LoopMode.LoopContinuous:
                t = Mathf.Repeat(rawT, 1f);
                break;
            case LoopMode.PingPong:
                t = Mathf.PingPong(rawT, 1f);
                break;
            case LoopMode.Once:
                t = Mathf.Clamp01(rawT);
                if (t >= 1f) isPlaying = false;
                break;
        }

        // 4. 스플라인의 로컬 위치, 정면, 위쪽 방향 추출
        splineContainer.Evaluate(t, out float3 localPos, out float3 localTangent, out float3 localUp);

        // 5. 스플라인 Container의 Transform을 반영해 월드 좌표로 변환
        Transform splineTransform = splineContainer.transform;
        Vector3 targetPos = splineTransform.TransformPoint((Vector3)localPos);
        Vector3 worldForward = splineTransform.TransformDirection((Vector3)localTangent);
        Vector3 worldUp = splineTransform.TransformDirection((Vector3)localUp);

        Quaternion targetRotation = rb.rotation;
        if (worldForward.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(worldForward, worldUp);
        }

        // 6. [지터링 해결 핵심] Kinematic Rigidbody에 실제 선속도와 각속도 부여
        Vector3 calculatedVelocity = (targetPos - rb.position) / Time.fixedDeltaTime;
        rb.linearVelocity = calculatedVelocity;

        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(rb.rotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        if (angleInDegrees > 180f) angleInDegrees -= 360f;

        if (Mathf.Abs(angleInDegrees) > 0.001f && !float.IsNaN(rotationAxis.x))
        {
            rb.angularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }

        // 7. 위치 및 회전 적용
        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRotation);
    }
}
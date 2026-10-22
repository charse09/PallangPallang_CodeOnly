using UnityEngine;

public class RocketFistProjectile : MonoBehaviour
{
    private Vector3 shootDirection;
    private float targetSpeed;       // 최종 도달할 최고 속도
    private float currentSpeed = 0f; // 현재 속도 (처음엔 0으로 시작)
    private float maxRange;
    private float traveledDistance = 0f;

    [Header("Rocket Dynamics")]
    [Tooltip("초기 발사 속도입니다. 약간 느리게 시작하려면 5~10 정도를 줍니다.")]
    [SerializeField] private float startSpeed = 5f;

    [Tooltip("최고 속도에 도달할 때까지의 가속도입니다. 클수록 빨리 최고 속도가 됩니다.")]
    [SerializeField] private float acceleration = 60f;

    // 보스 컨트롤러에서 발사할 때 이 함수를 호출해 세팅값을 전달합니다.
    public void Initialize(Vector3 direction, float maxSpeed, float range)
    {
        shootDirection = direction.normalized;
        targetSpeed = maxSpeed;
        maxRange = range;

        // 발사 직후 속도를 시작 속도로 세팅 (서서히 출발)
        currentSpeed = startSpeed;
    }

    void Update()
    {
        // 1. 속도 가속 로직 (매 프레임 가속도가 붙어 서서히 빨라짐)
        if (currentSpeed < targetSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            // 최고 속도를 넘지 않도록 제한
            if (currentSpeed > targetSpeed)
            {
                currentSpeed = targetSpeed;
            }
        }

        // 2. 실제 이동 로직
        float moveAmount = currentSpeed * Time.deltaTime;
        transform.position += shootDirection * moveAmount;
        traveledDistance += moveAmount;

        // 3. 최대 사거리에 도달하면 스스로 파괴됨
        if (traveledDistance >= maxRange)
        {
            // TODO: 폭발 이펙트 등을 이곳에 추가할 수 있습니다.
            Destroy(gameObject);
        }
    }
}
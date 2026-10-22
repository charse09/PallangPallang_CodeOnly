using UnityEngine;

public class DeathTriggerZone : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject executionerPrefab; // 처형자 프리팹
    public Transform spawnPoint;         // 처형자 스폰 위치

    [Header("IK Target Settings")]
    // [필수] 프로젝트 창에 빈 게임오브젝트를 프리팹으로 만들어서 여기에 넣어주세요!
    public GameObject ikTargetPrefab;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 1. 플레이어 위치(발밑에서 약간 위 0.1m)에 진짜로 타겟 프리팹을 스폰(Spawn)합니다.
            Vector3 spawnTargetPos = other.transform.position;
            GameObject spawnedTarget = Instantiate(ikTargetPrefab, spawnTargetPos, Quaternion.identity);

            // 2. 처형자 캐릭터 스폰
            GameObject executioner = Instantiate(executionerPrefab, spawnPoint.position, spawnPoint.rotation);

            // 3. 처형자에게 플레이어 정보와 방금 플레이어 위치에 스폰한 타겟의 Transform을 함께 전달
            ExecutionerController controller = executioner.GetComponent<ExecutionerController>();
            if (controller != null)
            {
                controller.Initialize(other.transform, spawnedTarget.transform);
            }
        }
    }
}
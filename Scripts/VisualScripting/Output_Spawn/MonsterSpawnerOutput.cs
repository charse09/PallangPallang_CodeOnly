using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 위치 주변에 특정 수량의 프리팹(몬스터 등)을 무작위 위치에 생성하는 Output 모듈
    /// </summary>
    public class MonsterSpawnerOutput : ProcessBase
    {
        [Header("Target & Prefab")]
        [Tooltip("생성할 몬스터의 프리팹")]
        [SerializeField] private GameObject monsterPrefab;

        [Tooltip("생성의 기준이 될 중앙 위치 (비워두면 이 스크립트가 붙은 오브젝트의 위치를 사용합니다)")]
        [SerializeField] private Transform spawnCenterLocation;

        [Header("Spawn Settings")]
        [Tooltip("생성할 몬스터의 총 수량")]
        [SerializeField] private int spawnCount = 3;

        [Tooltip("중앙 위치로부터 몬스터가 생성될 수 있는 최대 반경(미터)")]
        [SerializeField] private float spawnRadius = 3.0f;

        [Tooltip("각 몬스터가 생성되는 사이의 시간 간격(초)\n(0으로 설정하면 한 번에 동시에 생성됩니다)")]
        [SerializeField] private float delayBetweenSpawns = 0.2f;

        private bool isSpawning = false;

        private void Start()
        {
            // 중앙 위치가 지정되지 않았다면 자기 자신을 기준으로 삼음
            if (spawnCenterLocation == null)
            {
                spawnCenterLocation = transform;
            }
        }

        public override void Execute()
        {
            // 프리팹이 할당되어 있고, 현재 스폰 중이 아닐 때만 실행
            if (!isSpawning && monsterPrefab != null)
            {
                StartCoroutine(SpawnRoutine());
            }
            else if (monsterPrefab == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 스폰할 몬스터 프리팹이 할당되지 않았습니다!");
            }
        }

        private IEnumerator SpawnRoutine()
        {
            isSpawning = true;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 몬스터 스폰 시작! (총 {spawnCount}마리)");
#endif

            for (int i = 0; i < spawnCount; i++)
            {
                // 원형 범위 내의 무작위 X, Z 좌표를 계산 (Y축은 지면을 유지하기 위해 0으로 처리)
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);

                // 최종 스폰 위치 (중앙 위치 + 무작위 오프셋)
                Vector3 spawnPosition = spawnCenterLocation.position + randomOffset;

                // 몬스터 생성
                Instantiate(monsterPrefab, spawnPosition, spawnCenterLocation.rotation);

                // 스폰 간격이 설정되어 있다면 대기 (자연스러운 등장 연출 및 렉 방지)
                if (delayBetweenSpawns > 0f)
                {
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
            }

            // 모든 스폰이 완료됨
            isSpawning = false;

            // 프레임워크 상태 갱신: 스폰이 끝났음을 알리고 다음 노드(이벤트) 실행
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> {spawnCount}마리의 몬스터 스폰 완료.");
#endif
        }

        // ==========================================
        // 에디터 씬 뷰에서 스폰 영역을 시각적으로 표시
        // ==========================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = spawnCenterLocation != null ? spawnCenterLocation.position : transform.position;

            // 바닥(XZ 평면)에 몬스터가 생성될 범위를 원반 형태로 그려줍니다.
            UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f); // 반투명한 빨간색
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, spawnRadius);

            // 원반의 테두리
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, spawnRadius);
        }
#endif
    }
}
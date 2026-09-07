/*+
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 타겟 오브젝트의 글로벌 Position과 Rotation을 기반으로 정해진 오브젝트를 소환하는 프로세스
    /// </summary>
    public class SpawnObjectAtTarget : ProcessBase
    {
        [Header("[Spawn Settings]")]
        [Tooltip("소환할 원본 프리팹 또는 오브젝트")]
        [SerializeField] private GameObject prefabToSpawn;

        [Tooltip("위치와 회전값을 가져올 기준 타겟 오브젝트")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("소환된 오브젝트의 부모(Parent)를 지정하고 싶다면 설정 (비워두면 최상위에 생성)")]
        [SerializeField] private Transform spawnParent;

        public override void Execute()
        {
            // 방어 코드: 필수 레퍼런스 체크
            if (prefabToSpawn == null)
            {
                Debug.LogError($"<{gameObject.name}> prefabToSpawn이 지정되지 않았습니다.");
                IsOn = true; // 에러 발생 시에도 흐름이 멈추지 않도록 처리
                return;
            }

            if (targetTransform == null)
            {
                Debug.LogError($"<{gameObject.name}> targetTransform이 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 타겟의 글로벌 위치(position)와 회전값(rotation)을 가져와 소환
            Vector3 spawnPosition = targetTransform.position;
            Quaternion spawnRotation = targetTransform.rotation;

            GameObject spawnedInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, spawnParent);

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetTransform.name}의 위치({spawnPosition})에 {prefabToSpawn.name} 소환 완료.");
#endif

            // 소환 로직이 완료되었으므로 다음 프로세스로 넘어가도록 상태 완료 처리
            IsOn = true;
        }
    }
}
*/

using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 타겟 오브젝트의 글로벌 Position과 Rotation을 기반으로 정해진 오브젝트를 소환하고,
    /// 설정에 따라 일정 시간 후 자동으로 파괴하는 프로세스
    /// </summary>
    public class SpawnObjectAtTarget : ProcessBase
    {
        [Header("[Spawn Settings]")]
        [Tooltip("소환할 원본 프리팹 또는 오브젝트")]
        [SerializeField] private GameObject prefabToSpawn;

        [Tooltip("위치와 회전값을 가져올 기준 타겟 오브젝트")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("소환된 오브젝트의 부모(Parent)를 지정하고 싶다면 설정 (비워두면 최상위에 생성)")]
        [SerializeField] private Transform spawnParent;

        [Header("[Auto Destroy Settings]")]
        [Tooltip("체크하면 소환된 오브젝트가 일정 시간 후에 자동으로 사라집니다.")]
        [SerializeField] private bool autoDestroy = false;

        [Tooltip("소환되고 몇 초 후에 사라질지 설정 (autoDestroy가 켜져 있어야 작동)")]
        [Min(0f)]
        [SerializeField] private float destroyDelay = 3.0f;

        public override void Execute()
        {
            // 방어 코드: 필수 레퍼런스 체크
            if (prefabToSpawn == null)
            {
                Debug.LogError($"<{gameObject.name}> prefabToSpawn이 지정되지 않았습니다.");
                IsOn = true; 
                return;
            }

            if (targetTransform == null)
            {
                Debug.LogError($"<{gameObject.name}> targetTransform이 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 타겟의 글로벌 위치(position)와 회전값(rotation)을 가져와 소환
            Vector3 spawnPosition = targetTransform.position;
            Quaternion spawnRotation = targetTransform.rotation;

            GameObject spawnedInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, spawnParent);

            // 🔥 자동 파괴 로직 추가
            if (autoDestroy && spawnedInstance != null)
            {
                // 유니티 내장 함수 Destroy(오브젝트, 지연시간)를 활용해 깔끔하게 처리
                Destroy(spawnedInstance, destroyDelay);
            }

#if UNITY_EDITOR
            string destroyLog = autoDestroy ? $" ({destroyDelay}초 후 자동 파괴 예약)" : "";
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetTransform.name}의 위치에 {prefabToSpawn.name} 소환 완료.{destroyLog}");
#endif

            // 소환 처리가 끝났으므로 즉시 다음 프로세스로 흐름을 넘김
            IsOn = true;
        }
    }
}
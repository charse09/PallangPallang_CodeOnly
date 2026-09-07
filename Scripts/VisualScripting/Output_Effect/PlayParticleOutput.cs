using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 설정한 파티클(이펙트) 프리팹을 지정된 위치에 생성하여 재생하는 Output 모듈
    /// </summary>
    public class PlayParticleOutput : ProcessBase
    {
        [Header("Particle Settings")]
        [Tooltip("재생할 파티클/이펙트 프리팹을 끌어다 넣으세요.")]
        [SerializeField] private GameObject particlePrefab;

        [Tooltip("파티클이 생성될 기준 위치 (비워두면 이 노드 오브젝트의 위치에서 생성됩니다)")]
        [SerializeField] private Transform spawnLocation;

        [Tooltip("생성된 파티클을 기준 위치의 자식(Child)으로 묶어, 기준 오브젝트가 이동할 때 파티클도 따라다니게 할지 여부")]
        [SerializeField] private bool attachToLocation = false;

        public override void Execute()
        {
            if (particlePrefab != null)
            {
                // 생성할 위치와 회전값을 결정 (지정된 타겟이 없으면 자기 자신)
                Transform targetTransform = spawnLocation != null ? spawnLocation : transform;

                // 파티클 소환
                GameObject spawnedParticle = Instantiate(particlePrefab, targetTransform.position, targetTransform.rotation);

                // 대상을 따라다니게 만들고 싶다면 부모-자식 관계로 묶음
                if (attachToLocation)
                {
                    spawnedParticle.transform.SetParent(targetTransform);
                }

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> 파티클 재생: {particlePrefab.name}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> 재생할 파티클(Particle Prefab)이 할당되지 않았습니다!");
#endif
            }

            // 프레임워크 룰: 액션이 끝났으므로 다음 노드로 신호 전달
            IsOn = true;
        }
    }
}
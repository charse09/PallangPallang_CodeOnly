using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 재생 중인 파티클 시스템을 정지시키는 Output 모듈
    /// </summary>
    public class StopParticleOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("중지할 파티클 시스템 (비워두면 이 오브젝트에 붙은 파티클을 중지합니다)")]
        [SerializeField] private ParticleSystem targetParticle;

        [Header("Stop Options")]
        [Tooltip("체크 시: 새로운 파티클 생성을 멈추고, 이미 나온 파티클은 수명이 다할 때까지 둡니다 (자연스러움).\n체크 해제 시: 화면에 보이는 모든 파티클을 즉시 삭제합니다.")]
        [SerializeField] private bool stopEmittingOnly = true;

        [Tooltip("체크 시: 자식 오브젝트에 붙은 파티클들도 함께 정지시킵니다.")]
        [SerializeField] private bool withChildren = true;

        public override void Execute()
        {
            if (targetParticle == null)
            {
                targetParticle = GetComponent<ParticleSystem>();
            }

            if (targetParticle != null)
            {
                // 정지 모드 설정
                // stopEmittingOnly가 true면 새로운 입자 생성만 중단 (자연스럽게 사라짐)
                // false면 모든 입자를 즉시 소멸(Clear) 시킴
                ParticleSystemStopBehavior behavior = stopEmittingOnly ?
                    ParticleSystemStopBehavior.StopEmitting :
                    ParticleSystemStopBehavior.StopEmittingAndClear;

                targetParticle.Stop(withChildren, behavior);

#if UNITY_EDITOR
                Debug.Log($"<color=orange>[{gameObject.name}]</color> '{targetParticle.name}' 파티클 정지됨 (모드: {behavior})");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> 중지할 파티클 시스템을 찾을 수 없습니다.");
#endif
            }

            // 프레임워크 룰: 즉시 완료
            IsOn = true;
        }
    }
}
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어가 특정 영역 등에 진입했을 때, 어느 방향에서 오든 지정된 특정 회전(Rotation) 값으로 강제 회전시키는 Output 프로세스입니다.
    /// 즉각적인 회전 및 부드러운 회전(Lerp/Slerp) 모두 지원합니다.
    /// </summary>
    public class SetPlayerRotationOutput : ProcessBase
    {
        [Header("Rotation Settings")]
        [Tooltip("플레이어가 바라보게 될 목표 회전값 (Euler Angles)")]
        [SerializeField] private Vector3 targetRotationEuler;
        
        [Tooltip("true이면 즉시 회전, false이면 지정된 시간에 걸쳐 부드럽게 회전합니다.")]
        [SerializeField] private bool rotateInstantly = true;
        
        [Tooltip("부드럽게 회전할 경우 소요되는 시간 (초)")]
        [SerializeField] private float rotationDuration = 0.5f;

        [Header("Optional Target")]
        [Tooltip("직접 특정 오브젝트를 바라보게 하고 싶다면 이 필드에 Transform을 할당하세요. (할당되면 Euler 각도 대신 이 타겟을 바라봅니다)")]
        [SerializeField] private Transform lookAtTarget;

        public override void Execute()
        {
            IsOn = false;
            
            PlayerLocomotion player = FindFirstObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                Quaternion targetRot = Quaternion.Euler(targetRotationEuler);
                
                if (lookAtTarget != null)
                {
                    Vector3 dir = (lookAtTarget.position - player.transform.position).normalized;
                    dir.y = 0; // 보통 Y축(위아래) 회전은 제외하고 평면상으로만 바라보게 함
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        targetRot = Quaternion.LookRotation(dir);
                    }
                }

                if (rotateInstantly)
                {
                    // 1. 즉시 회전
                    player.transform.rotation = targetRot;
                    
                    Rigidbody rb = player.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.rotation = targetRot;
                        rb.angularVelocity = Vector3.zero;
                    }

                    // 2. PlayerLocomotion 내부의 목표 회전값도 동기화하여 다시 원래 각도로 돌아가는 현상 방지
                    player.SetBaseRotation(targetRot * Vector3.forward);
                    
                    IsOn = true;
                }
                else
                {
                    // 3. 코루틴으로 부드럽게 회전
                    StartCoroutine(RotateRoutine(player, targetRot));
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] SetPlayerRotationOutput: PlayerLocomotion을 찾을 수 없습니다.");
                IsOn = true;
            }
        }
        
        private IEnumerator RotateRoutine(PlayerLocomotion player, Quaternion targetRot)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            Quaternion startRot = player.transform.rotation;
            
            float elapsed = 0f;
            
            // 회전 중에는 PlayerLocomotion이 목표 방향을 바라보도록 지속 갱신
            while (elapsed < rotationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rotationDuration);
                
                // 부드러운 감속 효과(Ease-out)
                t = t * t * (3f - 2f * t); 

                Quaternion currentRot = Quaternion.Slerp(startRot, targetRot, t);
                
                player.transform.rotation = currentRot;
                if (rb != null)
                {
                    rb.MoveRotation(currentRot);
                }
                
                player.SetBaseRotation(currentRot * Vector3.forward);
                
                yield return null;
            }
            
            // 최종 보정
            player.transform.rotation = targetRot;
            if (rb != null)
            {
                rb.rotation = targetRot;
                rb.angularVelocity = Vector3.zero;
            }
            player.SetBaseRotation(targetRot * Vector3.forward);
            
            IsOn = true;
        }
    }
}

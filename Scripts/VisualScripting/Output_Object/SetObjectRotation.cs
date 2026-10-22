using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetObjectRotation : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트")]
        [SerializeField] private GameObject targetObject;

        [Header("Rotation Settings")]
        [Tooltip("오브젝트가 바라보게 될 목표 회전값 (x, y, z)")]
        [SerializeField] private Vector3 targetRotation = Vector3.zero;

        [Space(5)]
        [Tooltip("체크 시 부모를 기준으로 한 Local 회전값을 변경하며, 해제 시 월드 기준 회전값을 변경합니다.")]
        [SerializeField] private bool isLocalRotation = true;

        private void Awake()
        {
            // 인스펙터에서 대상을 비워두면 자기 자신을 할당
            if (!targetObject) targetObject = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetObject == null)
            {
                Debug.LogWarning("[SetObjectRotation] 타겟 오브젝트가 할당되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 인스펙터에서 입력한 Vector3(오일러 각)를 유니티 회전 체계인 Quaternion으로 변환
            Quaternion newRotation = Quaternion.Euler(targetRotation);

            // 로컬 좌표계 기준인지 월드 좌표계 기준인지에 따라 회전값 적용
            if (isLocalRotation)
            {
                targetObject.transform.localRotation = newRotation;
            }
            else
            {
                targetObject.transform.rotation = newRotation;
            }

            // 다음 Visual Scripting 노드로 제어권 넘김
            IsOn = true;
        }
    }
}
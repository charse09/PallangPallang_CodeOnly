using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetPetStateProcess : ProcessBase
    {
        [Header("Pet Settings")]
        [SerializeField] private FloatingPet targetPet;

        [Header("Command Settings")]
        [SerializeField] private PetState commandState = PetState.Follow;

        [SerializeField] private Vector3 customOffset = new Vector3(0f, 1.0f, 1.5f);
        [SerializeField] private Transform waypointTransform;

        // 새로 추가된 속도 제어 옵션
        [Header("Movement Override")]
        [Tooltip("이 명령을 수행할 때 펫이 이동할 속도입니다.\n(0으로 설정하면 펫 컴포넌트에 설정된 기본 속도를 사용합니다)")]
        [SerializeField] private float customMoveSpeed = 0f;

        [Header("Flow Settings")]
        [SerializeField] private float waitDuration = 1.0f;

        private Coroutine _processCoroutine;

        private void Awake()
        {
            if (targetPet == null) targetPet = GetComponent<FloatingPet>();
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetPet == null)
            {
                Debug.LogWarning("SetPetStateProcess: 제어할 FloatingPet이 없습니다!");
                IsOn = true;
                return;
            }

            ExecutePetCommand();

            if (waitDuration > 0f)
            {
                if (_processCoroutine != null) StopCoroutine(_processCoroutine);
                _processCoroutine = StartCoroutine(WaitRoutine());
            }
            else
            {
                IsOn = true;
            }
        }

        private void ExecutePetCommand()
        {
            // 명령을 내릴 때 customMoveSpeed를 함께 넘겨줍니다.
            switch (commandState)
            {
                case PetState.Follow:
                    targetPet.SetStateFollow(customMoveSpeed);
                    break;
                case PetState.Wander:
                    targetPet.SetStateWander(customMoveSpeed);
                    break;
                case PetState.CustomOffset:
                    targetPet.MoveToCustomOffset(customOffset, customMoveSpeed);
                    break;
                case PetState.MoveToTransform:
                    if (waypointTransform != null)
                    {
                        targetPet.MoveToSpecificTransform(waypointTransform, customMoveSpeed);
                    }
                    else
                    {
                        Debug.LogWarning("SetPetStateProcess: 이동할 빈 오브젝트가 없습니다!");
                    }
                    break;
            }
        }

        private IEnumerator WaitRoutine()
        {
            yield return new WaitForSeconds(waitDuration);
            _processCoroutine = null;
            IsOn = true;
        }
    }
}
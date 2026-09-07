using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SequenceOutlineTrigger : ProcessBase
    {
        [System.Serializable]
        public struct OutlineStepData
        {
            [Tooltip("순서 검사용 TriggerEnter 컴포넌트")]
            public TriggerEnter trigger;

            [Tooltip("아웃라인 머티리얼을 추가할 오브젝트의 MeshRenderer")]
            public MeshRenderer targetRenderer;

            [HideInInspector]
            [Tooltip("런타임 시 원본 머티리얼 배열을 기억하기 위한 백업 배열")]
            public Material[] originalMaterials;
        }

        [Header("순서 및 아웃라인 설정 목록")]
        [SerializeField] private List<OutlineStepData> sequenceSteps;

        [Header("머티리얼 설정")]
        [Tooltip("현재 활성화된(눌러야 하는) 버튼에 '추가'로 입힐 아웃라인 머티리얼")]
        [SerializeField] private Material outlineMaterial;

        [Header("설정")]
        [SerializeField] private bool resetOnFail = true; // 순서를 틀렸을 때 처음부터 다시 할지 여부

        private bool isFail = false;
        private int _currentStepIndex = 0; // 현재 진행 중인 단계

        private void Start()
        {
            // 1. 게임 시작 시점에 모든 발판의 순수 원본 머티리얼 배열을 안전하게 백업(캐싱)합니다.
            BackupOriginalMaterials();

            // 2. 초기 상태 셋팅 및 첫 번째 버튼에 아웃라인 추가 적용
            ResetAllMaterials();
            UpdateOutlineHighlight();
        }

        private void Update()
        {
            // 이미 완료되었거나, 리스트가 비어있으면 작동하지 않음
            if (IsOn || sequenceSteps.Count == 0) return;

            // 1. 현재 단계에서 눌러야 할 '정답 트리거' 확인
            TriggerEnter expectedTrigger = sequenceSteps[_currentStepIndex].trigger;

            if (expectedTrigger.IsOn)
            {
                Debug.Log($"[{gameObject.name}] {_currentStepIndex + 1}단계 성공!");

                // 임무를 완수한 이전 버튼의 아웃라인을 제거 (원본 머티리얼 상태로 복구)
                RestoreDefaultMaterial(_currentStepIndex);

                _currentStepIndex++;

                // 모든 순서를 다 맞췄다면?
                if (_currentStepIndex >= sequenceSteps.Count)
                {
                    IsOn = true;
                    Execute(); // 후속 노드로 신호 전달
                    Debug.Log($"[{gameObject.name}] 순서 퍼즐 완료! 최종 트리거 발동!");
                }
                else
                {
                    // 아직 다음 단계가 남아있다면 다음 버튼에 아웃라인 겹쳐서 켜기
                    UpdateOutlineHighlight();
                }
                return; // 성공했으면 이번 프레임 검사 종료
            }

            // 2. 순서를 틀렸는지 검사 (resetOnFail이 true일 때만)
            if (resetOnFail)
            {
                // 아직 누를 차례가 아닌 '미래의 스위치'가 눌렸는지 확인합니다.
                for (int i = _currentStepIndex + 1; i < sequenceSteps.Count; i++)
                {
                    if (sequenceSteps[i].trigger.IsOn)
                    {
                        FailSequence();
                        isFail = true;
                        break;
                    }
                }
            }
        }

        private void FailSequence()
        {
            Debug.Log($"[{gameObject.name}] 잘못된 순서입니다! 퍼즐이 초기화됩니다.");
            _currentStepIndex = 0; // 진행도 초기화

            // 리스트에 있는 모든 스위치의 상태를 끕니다.
            foreach (var step in sequenceSteps)
            {
                step.trigger.Reset();
            }

            // 모든 머티리얼에서 아웃라인을 떼어내고, 다시 첫 번째 버튼에 아웃라인 추가
            ResetAllMaterials();
            UpdateOutlineHighlight();
        }

        /// <summary>
        /// 모든 발판의 기존 원본 머티리얼 리스트를 고유하게 복사하여 안전하게 백업합니다.
        /// (sharedMaterials를 받아와 복사하므로 메모리 누수 및 에디터 Inspector 포인터 깨짐 현상을 예방합니다)
        /// </summary>
        private void BackupOriginalMaterials()
        {
            for (int i = 0; i < sequenceSteps.Count; i++)
            {
                OutlineStepData step = sequenceSteps[i];
                if (step.targetRenderer != null)
                {
                    // 기존에 장착된 머티리얼 배열 복사 (단일 혹은 멀티 머티리얼 모두 완벽 대응)
                    step.originalMaterials = step.targetRenderer.sharedMaterials;
                    sequenceSteps[i] = step;
                }
            }
        }

        /// <summary>
        /// 현재 순서(_currentStepIndex)에 해당하는 버튼의 기존 머티리얼 배열 뒤에 아웃라인 머티리얼을 '추가'합니다.
        /// </summary>
        private void UpdateOutlineHighlight()
        {
            if (_currentStepIndex < sequenceSteps.Count)
            {
                var currentStep = sequenceSteps[_currentStepIndex];
                if (currentStep.targetRenderer != null && outlineMaterial != null && currentStep.originalMaterials != null)
                {
                    // 1. 기존 원본 머티리얼 개수보다 1개 더 큰 임시 배열 생성
                    int originalCount = currentStep.originalMaterials.Length;
                    Material[] newMaterials = new Material[originalCount + 1];

                    // 2. 기존 원래 머티리얼들을 그대로 복사해 넣음
                    for (int i = 0; i < originalCount; i++)
                    {
                        newMaterials[i] = currentStep.originalMaterials[i];
                    }

                    // 3. 배열의 맨 마지막 슬롯에 아웃라인 머티리얼 추가
                    newMaterials[originalCount] = outlineMaterial;

                    // 4. 렌더러에 겹쳐진 새 머티리얼 리스트 대입
                    currentStep.targetRenderer.materials = newMaterials;
                }
            }
        }

        /// <summary>
        /// 특정 인덱스의 버튼 머티리얼을 아웃라인이 섞이지 않은 순수 원본 백업본으로 복구합니다.
        /// </summary>
        private void RestoreDefaultMaterial(int index)
        {
            if (index < sequenceSteps.Count)
            {
                var step = sequenceSteps[index];
                if (step.targetRenderer != null && step.originalMaterials != null)
                {
                    // 백업해두었던 순수 원래 머티리얼 배열로 덮어씌워서 아웃라인 제거
                    step.targetRenderer.materials = step.originalMaterials;
                }
            }
        }

        /// <summary>
        /// 모든 버튼의 머티리얼을 아웃라인이 없는 순수 원본 상태로 초기화합니다.
        /// </summary>
        private void ResetAllMaterials()
        {
            for (int i = 0; i < sequenceSteps.Count; i++)
            {
                RestoreDefaultMaterial(i);
            }
        }

        public override void Execute()
        {
            // 이 컴포넌트가 최종적으로 완료되었을 때 실행할 내용
        }

        public bool CheckIsFail()
        {
            if (isFail == true)
            {
                isFail = false;
                return true;
            }
            else { return false; }
        }
    }
}
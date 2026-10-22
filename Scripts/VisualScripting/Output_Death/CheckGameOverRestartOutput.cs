using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 특정 체크포인트 번호(1~5)로 재시작되었는지 판별하여,
    /// 플레이어 이동 및 활성화, 기믹 일괄 비활성화, IsOn 신호 출력 및
    /// 지정된 SequenceConditional / Conditional 노드들을 즉시 실행시킵니다.
    /// </summary>
    public class CheckGameOverRestartOutput : ProcessBase
    {
        [Header("1. Checkpoint Identification")]
        [Tooltip("★ 이 노드가 담당할 체크포인트 번호 (1 ~ 5)")]
        [Range(1, 5)]
        [SerializeField] private int targetCheckpointIndex = 1;

        [Tooltip("씬 시작(Start) 시 자동으로 검사할지 여부")]
        [SerializeField] private bool autoCheckOnStart = true;

        [Header("2. Respawn Point Settings")]
        [Tooltip("위치를 옮겨줄 플레이어 Transform (비워둘 경우 'Player' 태그 자동 탐색)")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("★ 이 체크포인트에서 부활할 스폰 위치 Transform")]
        [SerializeField] private Transform respawnPoint;

        [Tooltip("스폰 위치의 회전 각도(방향)까지 일치시킬지 여부")]
        [SerializeField] private bool matchRotation = true;

        [Header("3. Gimmicks To Disable (꺼버릴 기믹들)")]
        [Tooltip("이 체크포인트로 부활했을 때 자동으로 비활성화(OFF)할 이전 기믹/장애물 게임오브젝트들")]
        [SerializeField] private List<GameObject> gimmicksToDeactivate = new List<GameObject>();

        [Header("4. Trigger Conditionals On Restart (재시작 시 강제 실행할 시퀀스)")]
        [Tooltip("★ 재시작과 동시에 Input 조건과 무관하게 즉시 시작시킬 SequenceConditional 또는 Conditional 노드들")]
        [SerializeField] private List<ProcessBase> processesToExecuteOnRestart = new List<ProcessBase>();

        [Header("5. Branch Callbacks")]
        [Tooltip("이 체크포인트로 부활했을 때 실행할 추가 커스텀 이벤트")]
        [SerializeField] private UnityEvent onRestartThisCheckpoint;

        private void Start()
        {
            if (autoCheckOnStart)
            {
                Execute();
            }
        }

        public override void Execute()
        {
            StartCoroutine(CheckAndRestoreRoutine());
        }

        private IEnumerator CheckAndRestoreRoutine()
        {
            // 0초 트리거 및 플레이어 컨트롤러 초기화 충돌 방지를 위해 1프레임 대기
            yield return null;

            if (SceneRestartState.IsRestartedFromGameOver && SceneRestartState.ActiveCheckpoint == targetCheckpointIndex)
            {
                // 1. 기믹 일괄 비활성화 (OFF)
                if (gimmicksToDeactivate != null)
                {
                    foreach (var gimmick in gimmicksToDeactivate)
                    {
                        if (gimmick != null)
                        {
                            gimmick.SetActive(false);
                        }
                    }
                }

                // 2. 안전 텔레포트 및 활성화 실행
                if (respawnPoint != null)
                {
                    if (playerTransform == null)
                    {
                        GameObject p = GameObject.FindWithTag("Player");
                        if (p != null)
                        {
                            playerTransform = p.transform;
                        }
                        else
                        {
                            // 플레이어가 SetActive(false) 상태라 FindWithTag로 안 잡힐 경우 비활성화 포함 탐색
                            foreach (var root in gameObject.scene.GetRootGameObjects())
                            {
                                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                                {
                                    if (child.CompareTag("Player"))
                                    {
                                        playerTransform = child;
                                        break;
                                    }
                                }
                                if (playerTransform != null) break;
                            }
                        }
                    }

                    if (playerTransform != null)
                    {
                        CharacterController cc = playerTransform.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;

                        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }

                        // 위치와 회전을 스폰 지점으로 먼저 이동
                        playerTransform.position = respawnPoint.position;
                        if (matchRotation)
                        {
                            playerTransform.rotation = respawnPoint.rotation;
                        }

                        Physics.SyncTransforms();

                        // 스폰 포인트로 완전히 옮겨진 뒤 활성화
                        if (!playerTransform.gameObject.activeSelf)
                        {
                            playerTransform.gameObject.SetActive(true);
                        }

                        yield return null;
                        if (cc != null) cc.enabled = true;
                    }
                }

                // 3. 본인 완료 신호 On (방식 1: Input Data로 물려있는 조건 노드 반응)
                IsOn = true;

                // 4. 방식 2: 등록된 SequenceConditional / Conditional 강제 시작
                if (processesToExecuteOnRestart != null)
                {
                    foreach (var proc in processesToExecuteOnRestart)
                    {
                        if (proc != null)
                        {
                            proc.Reset();
                            proc.Execute();
                        }
                    }
                }

                onRestartThisCheckpoint?.Invoke();
            }
            else
            {
                IsOn = false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            StopAllCoroutines();
            IsOn = false;
        }
    }
}
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// PlayerLocomotionController(또는 PlayerLocomotion)가 비활성화된 동안
    /// 외부 조작(RotateObject, MatchTransform 등)으로 인해 transform이 변경된 경우,
    /// 스크립트를 다시 활성화하기 직전에 이 노드를 실행하면
    /// Locomotion 내부 상태를 현재 transform에 맞게 동기화합니다.
    ///
    /// ────────────────────────────────────────────────────────────────
    /// [해결하는 문제]
    /// Locomotion의 LateUpdate는 매 프레임 baseMoveRotation을 기준으로
    /// transform.rotation을 강제 덮어쓰기 때문에, Locomotion이 꺼진 사이에
    /// RotateObject 등이 바꿔놓은 rotation이 Locomotion 재활성화 즉시 원복됩니다.
    /// 이 노드는 baseMoveRotation(및 기타 내부 값)을 현재 transform 상태로
    /// 동기화하여 원복 현상을 방지합니다.
    ///
    /// [권장 사용 순서]
    ///   RotateObject 완료
    ///       → SyncLocomotionStateOutput  ← 이 노드
    ///           → SetObjectScriptsEnabledOutput (PlayerLocomotion 재활성화)
    ///
    /// ────────────────────────────────────────────────────────────────
    /// [동기화 항목 선택 옵션]
    ///
    ///  ✅ syncRotation (기본 ON)
    ///     현재 transform.rotation의 수평 방향(Y축)을 baseMoveRotation으로 동기화.
    ///     → Locomotion 재활성화 후 LateUpdate가 rotation을 원복하는 현상 방지.
    ///
    ///  ✅ resetRigidbodyVelocity (기본 ON)
    ///     Rigidbody의 linearVelocity와 angularVelocity를 0으로 초기화.
    ///     → 재활성화 시 잔여 물리 속도로 인한 튕김 방지.
    ///
    ///  ✅ resetInternalVelocities — PlayerLocomotions(Controller) / VillainLocomotion만 해당 (기본 ON)
    ///     verticalVelocity, externalHorizontalVelocity, lerpMoveDir, inputMoveDir 초기화.
    ///     * PlayerLocomotion(Variable)은 OnEnable에서 자동 초기화되므로 별도 처리 불필요.
    ///
    ///  ⬜ resetPaperSlimBraceState (기본 OFF)
    ///     paperState, slimState, braceState를 강제 해제.
    ///     눕히는 연출 이후 일어서는 경우 등 상태를 깨끗이 정리할 때만 켜세요.
    ///     * PlayerLocomotion(Variable)은 이 옵션이 OFF여도 SetForceBraceState(false)만 호출됩니다.
    ///       paper/slim 상태 해제가 필요하면 별도로 PaperStateOutput 노드를 사용하세요.
    /// </summary>
    public class SyncLocomotionStateOutput : ProcessBase
    {
        [Header("Target (비워두면 씬에서 자동 검색)")]
        [Tooltip("PlayerLocomotion 또는 PlayerLocomotions 컴포넌트가 붙은 오브젝트.\n비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private GameObject playerObject;

        [Header("Rotation Sync")]
        [Tooltip("현재 transform.forward(수평)를 baseMoveRotation으로 동기화합니다.\n" +
                 "RotateObject 등으로 rotation을 바꾼 뒤 Locomotion을 켤 때 반드시 활성화하세요.")]
        [SerializeField] private bool syncRotation = true;

        [Header("Velocity Reset")]
        [Tooltip("Rigidbody의 linearVelocity와 angularVelocity를 0으로 초기화합니다.\n" +
                 "재활성화 시 잔여 속도로 인한 튕김/밀려남을 방지합니다.")]
        [SerializeField] private bool resetRigidbodyVelocity = true;

        [Tooltip("[PlayerLocomotions(Controller)만 해당]\n" +
                 "verticalVelocity, externalHorizontalVelocity 등 내부 속도 변수를 초기화합니다.\n" +
                 "PlayerLocomotion(Variable)은 OnEnable에서 자동 초기화되므로 이 옵션이 적용되지 않습니다.")]
        [SerializeField] private bool resetInternalVelocities = true;

        [Header("State Reset")]
        [Tooltip("체크 시 braceState를 강제 해제합니다 (SetForceBraceState(false) 호출).\n" +
                 "paper/slim 상태까지 해제하려면 PlayerLocomotions(Controller)의 경우\n" +
                 "CancelSpecialStatesForCondition()도 함께 호출됩니다.")]
        [SerializeField] private bool resetBraceState = false;

        public override void Execute()
        {
            // ── 대상 오브젝트 자동 검색 ──────────────────────────────
            if (playerObject == null)
            {
                var autoVar = FindFirstObjectByType<PlayerLocomotion>();
                if (autoVar != null) playerObject = autoVar.gameObject;
            }
            if (playerObject == null)
            {
                var autoCtrl = FindFirstObjectByType<PlayerLocomotions>();
                if (autoCtrl != null) playerObject = autoCtrl.gameObject;
            }
            if (playerObject == null)
            {
                var autoVillain = FindFirstObjectByType<VillainLocomotion>();
                if (autoVillain != null) playerObject = autoVillain.gameObject;
            }

            if (playerObject == null)
            {
                Debug.LogWarning($"[{gameObject.name}] SyncLocomotionStateOutput: " +
                                 "PlayerLocomotion / PlayerLocomotions / VillainLocomotion을 찾을 수 없습니다.\n" +
                                 "PlayerObject를 직접 지정하거나 씬에 해당 컴포넌트가 있는지 확인하세요.");
                IsOn = true;
                return;
            }

            bool synced = false;

            // ── ① PlayerLocomotion (PlayerVariable 구조) ─────────────
            var locomotion = playerObject.GetComponent<PlayerLocomotion>();
            if (locomotion != null)
            {
                SyncPlayerLocomotion(locomotion);
                synced = true;
            }

            // ── ② PlayerLocomotions (Controller 구조) ─────────────────
            var controller = playerObject.GetComponent<PlayerLocomotions>();
            if (controller != null)
            {
                SyncPlayerLocomotions(controller);
                synced = true;
            }

            // ── ③ VillainLocomotion ────────────────────────────────────
            var villain = playerObject.GetComponent<VillainLocomotion>();
            if (villain != null)
            {
                SyncVillainLocomotion(villain);
                synced = true;
            }

            if (!synced)
            {
                Debug.LogWarning($"[{gameObject.name}] SyncLocomotionStateOutput: " +
                                 $"'{playerObject.name}'에서 PlayerLocomotion / PlayerLocomotions / VillainLocomotion을 찾을 수 없습니다.");
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> SyncLocomotionStateOutput 완료 — " +
                      $"syncRot:{syncRotation}  rbVel:{resetRigidbodyVelocity}  " +
                      $"intVel:{resetInternalVelocities}  brace:{resetBraceState}");
#endif

            IsOn = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PlayerLocomotion (PlayerVariable 방식)
        //   - baseMoveRotation 접근은 SetBaseRotation() 공개 API로만 가능
        //   - verticalVelocity 등은 OnEnable에서 자동 초기화되므로 별도 처리 불필요
        // ─────────────────────────────────────────────────────────────────────
        private void SyncPlayerLocomotion(PlayerLocomotion locomotion)
        {
            // [1] Rotation 동기화
            if (syncRotation)
            {
                Vector3 forward = GetHorizontalForward(playerObject.transform.forward);
                locomotion.SetBaseRotation(forward);
            }

            // [2] Rigidbody 속도 초기화
            if (resetRigidbodyVelocity) ResetRb(playerObject);

            // [3] 내부 속도 변수: PlayerLocomotion은 OnEnable에서 자동으로
            //     verticalVelocity=0, lerpMoveDir=0, inputMoveDir=0 처리되므로 생략.

            // [4] 상태 리셋
            if (resetBraceState)
                locomotion.SetForceBraceState(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PlayerLocomotions (Controller 방식)
        //   - ResetInternalVelocities()가 이 PR에서 추가된 공개 메서드
        // ─────────────────────────────────────────────────────────────────────
        private void SyncPlayerLocomotions(PlayerLocomotions controller)
        {
            // [1] Rotation 동기화
            if (syncRotation)
            {
                Vector3 forward = GetHorizontalForward(playerObject.transform.forward);
                controller.SetBaseRotation(forward);
            }

            // [2] Rigidbody 속도 초기화
            if (resetRigidbodyVelocity) ResetRb(playerObject);

            // [3] 내부 속도 변수 초기화 (isGrounded 등 접지 상태는 건드리지 않음)
            if (resetInternalVelocities)
                controller.ResetInternalVelocities();

            // [4] 상태 리셋
            if (resetBraceState)
            {
                controller.SetForceBraceState(false);
                controller.CancelSpecialStatesForCondition();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // VillainLocomotion
        //   - PlayerLocomotions(Controller)와 동일한 API 구조를 공유합니다.
        // ─────────────────────────────────────────────────────────────────────
        private void SyncVillainLocomotion(VillainLocomotion villain)
        {
            // [1] Rotation 동기화
            if (syncRotation)
            {
                Vector3 forward = GetHorizontalForward(playerObject.transform.forward);
                villain.SetBaseRotation(forward);
            }

            // [2] Rigidbody 속도 초기화
            if (resetRigidbodyVelocity) ResetRb(playerObject);

            // [3] 내부 속도 변수 초기화
            if (resetInternalVelocities)
                villain.ResetInternalVelocities();

            // [4] 상태 리셋
            if (resetBraceState)
            {
                villain.SetForceBraceState(false);
                villain.CancelSpecialStatesForCondition();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공통 유틸
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// transform.forward의 수평 성분을 반환합니다.
        /// 오브젝트가 완전히 눕혀진 경우(forward가 거의 수직) Vector3.forward를 fallback으로 사용합니다.
        /// </summary>
        private static Vector3 GetHorizontalForward(Vector3 transformForward)
        {
            Vector3 h = new Vector3(transformForward.x, 0f, transformForward.z);
            return h.sqrMagnitude > 0.01f ? h.normalized : Vector3.forward;
        }

        /// <summary>Rigidbody의 선속도·각속도를 즉시 0으로 초기화합니다.</summary>
        private static void ResetRb(GameObject target)
        {
            var rb = target.GetComponent<Rigidbody>();
            if (rb == null) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어 캐릭터를 특정 벽면(Surface)에 그림처럼 착 달라붙게 만들고,
    /// 부착되어 있는 동안 다른 F키 상호작용 트리거 및 플레이어 고유의 종이 변신 입력을 차단하는 Output 모듈
    /// </summary>
    public class AttachToSurfaceOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("플레이어 오브젝트 (비워두면 자동으로 PlayerLocomotion을 탐색합니다)")]
        [SerializeField] private GameObject playerObject;

        [Header("Surface Alignment")]
        [Tooltip("플레이어가 달라붙을 벽면의 기준 트랜스폼")]
        [SerializeField] private Transform surfaceTarget;

        [Tooltip("벽에 붙을 때 걸리는 시간")]
        [SerializeField] private float attachDuration = 0.25f;

        [Tooltip("벽면으로부터 플레이어가 유지할 미세한 간격")]
        [SerializeField] private float surfaceOffset = 0.05f;

        [Header("Interaction Blocker (F키 중복 차단 설정)")]
        [Tooltip("벽에 붙어있는 동안 무력화시킬 씬 내의 다른 F키 상호작용 컴포넌트의 타입명")]
        [SerializeField] private string targetTriggerTypeName = "KeyInputTrigger";

        private MonoBehaviour[] _blockedTriggers;
        private bool _isAttached = false;

        public override void Execute()
        {
            IsOn = false;

            if (playerObject == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) playerObject = locomotion.gameObject;
            }

            if (playerObject == null || surfaceTarget == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 플레이어 또는 부착할 벽면이 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            if (!_isAttached)
            {
                StartCoroutine(AttachRoutine());
            }
            else
            {
                StartCoroutine(DetachRoutine());
            }
        }

        private IEnumerator AttachRoutine()
        {
            // 1. 씬 내의 다른 F키 오브젝트 상호작용 차단
            ToggleOtherInteractions(false);

            // =========================================================
            // ★ [핵심 해결] InputManager의 비트 플래그 시스템 연동
            // 플레이어가 '스스로' 종이 변신 단축키를 눌러 상태를 꼬는 것을 원천 차단합니다.
            // 필요에 따라 이동(Movement)도 같이 잠글 수 있습니다.
            // =========================================================
            if (InputManager.Instance != null)
            {
                // 종이 변신 입력 차단 (canPaperState = false)
                InputManager.Instance.SetSpecificInput(InputControlType.PaperState, false);
                // 필요하다면 벽에 붙은 동안 이동도 차단 가능:
                // InputManager.Instance.SetSpecificInput(InputControlType.Movement, false);
            }

            // 2. 플레이어 물리 고정 및 기존 로코모션 컴포넌트 일시 정지
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            PlayerLocomotion locomotion = playerObject.GetComponent<PlayerLocomotion>();

            if (rb != null) rb.isKinematic = true;
            if (locomotion != null) locomotion.enabled = false;

            // 3. 벽면 위치 정렬 및 부드러운 Lerp 연출
            Vector3 targetPos = surfaceTarget.position + (surfaceTarget.forward * surfaceOffset);
            Quaternion targetRot = Quaternion.LookRotation(-surfaceTarget.forward, surfaceTarget.up);

            Vector3 startPos = playerObject.transform.position;
            Quaternion startRot = playerObject.transform.rotation;

            if (attachDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < attachDuration)
                {
                    elapsed += Time.deltaTime;
                    float smoothT = Mathf.SmoothStep(0f, 1f, elapsed / attachDuration);
                    playerObject.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                    playerObject.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                    yield return null;
                }
            }

            playerObject.transform.position = targetPos;
            playerObject.transform.rotation = targetRot;

            // 4. 시스템 강제로 종이 상태(PaperState)를 세팅하여 납작하게 렌더링
            if (locomotion != null)
            {
                locomotion.PaperState = true;
            }

            _isAttached = true;
            IsOn = true;
        }

        private IEnumerator DetachRoutine()
        {
            PlayerLocomotion locomotion = playerObject.GetComponent<PlayerLocomotion>();
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();

            // 1. 강제 종이 상태 해제 및 위치 이격
            if (locomotion != null) locomotion.PaperState = false;

            Vector3 detachPos = playerObject.transform.position - (playerObject.transform.forward * 0.5f);
            playerObject.transform.position = detachPos;

            // 2. 컴포넌트 복구
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }
            if (locomotion != null)
            {
                locomotion.enabled = false;
                locomotion.enabled = true;
            }

            // 3. 차단했던 월드의 다른 F키 트리거들 원상복구
            ToggleOtherInteractions(true);

            // =========================================================
            // ★ [핵심 해결] 벽에서 떨어질 때 다시 원래 조작 권한 복구
            // =========================================================
            if (InputManager.Instance != null)
            {
                // 종이 변신 입력 다시 허용 (canPaperState = true)
                InputManager.Instance.SetSpecificInput(InputControlType.PaperState, true);
                // InputManager.Instance.SetSpecificInput(InputControlType.Movement, true);
            }

            _isAttached = false;
            IsOn = true;
            yield return null;
        }

        private void ToggleOtherInteractions(bool enableState)
        {
            if (!enableState)
            {
                var allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                System.Collections.Generic.List<MonoBehaviour> foundTriggers = new System.Collections.Generic.List<MonoBehaviour>();

                foreach (var script in allScripts)
                {
                    if (script == this) continue;
                    if (script.GetType().Name == targetTriggerTypeName)
                    {
                        script.enabled = false;
                        foundTriggers.Add(script);
                    }
                }
                _blockedTriggers = foundTriggers.ToArray();
            }
            else
            {
                if (_blockedTriggers != null)
                {
                    foreach (var script in _blockedTriggers)
                    {
                        if (script != null) script.enabled = true;
                    }
                    _blockedTriggers = null;
                }
            }
        }
    }
}
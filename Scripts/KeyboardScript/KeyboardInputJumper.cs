using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInputJumper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("점프시킬 플레이어 캐릭터의 Transform을 할당하세요 (비워둘 경우 자동 탐색).")]
    public Transform playerTransform;

    [Header("Jump Settings")]
    [Tooltip("포물선 점프 최고 높이")]
    public float jumpHeight = 0.5f;
    [Tooltip("점프 이동 시간(초)")]
    public float jumpDuration = 0.3f;
    [Tooltip("키캡 상단 표면 밀착 오프셋 (캐릭터 스케일에 맞게 미세 조절, 기본 0.001)")]
    public float playerYOffset = 0.001f;

    private Dictionary<KeyCode, Transform> keyTransformMap = new Dictionary<KeyCode, Transform>();
    private bool isJumping = false;

    private PlayerLocomotion cachedPlayerLocomotion;
    private Rigidbody cachedPlayerRb;

    void Start()
    {
        InitializePlayerReference();
        MapKeysAutomatically();
    }

    private void InitializePlayerReference()
    {
        if (playerTransform == null)
        {
            var foundPlayer = FindFirstObjectByType<PlayerLocomotion>();
            if (foundPlayer != null)
            {
                playerTransform = foundPlayer.transform;
                cachedPlayerLocomotion = foundPlayer;
                cachedPlayerRb = foundPlayer.GetComponent<Rigidbody>();
            }
        }
        else
        {
            cachedPlayerLocomotion = playerTransform.GetComponent<PlayerLocomotion>()
                                     ?? playerTransform.GetComponentInParent<PlayerLocomotion>()
                                     ?? playerTransform.GetComponentInChildren<PlayerLocomotion>();
            cachedPlayerRb = playerTransform.GetComponent<Rigidbody>()
                             ?? playerTransform.GetComponentInParent<Rigidbody>()
                             ?? playerTransform.GetComponentInChildren<Rigidbody>();
        }

        if (playerTransform == null)
        {
            Debug.LogError("[KeyboardInputJumper] Player Transform을 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        if (isJumping || !Input.anyKeyDown) return;

        if (playerTransform == null)
        {
            InitializePlayerReference();
            if (playerTransform == null) return;
        }

        foreach (var kvp in keyTransformMap)
        {
            try
            {
                if (Input.GetKeyDown(kvp.Key))
                {
                    // --- [모니터 입력 연동] ---
                    ProcessMonitorInput(kvp.Key);
                    // ---------------------------

                    StartCoroutine(JumpToKeyRoutine(kvp.Value));
                    break;
                }
            }
            catch (ArgumentException)
            {
                continue;
            }
        }
    }

    // 키 입력 문자열 가공 및 모니터 전달 메서드
    private void ProcessMonitorInput(KeyCode code)
    {
        if (MonitorManager.Instance == null) return;

        // 1. 백스페이스 처리
        if (code == KeyCode.Backspace)
        {
            MonitorManager.Instance.DeleteLastCharacter();
            return;
        }

        // --- [엔터(확인키) 처리] ---
        if (code == KeyCode.Return || code == KeyCode.KeypadEnter)
        {
            MonitorManager.Instance.CheckPassword();
            return;
        }
        // ---------------------------

        // 2. 일반 텍스트 입력 조합
        string inputStr = "";

        // 알파벳 입력 (A ~ Z)
        if (code >= KeyCode.A && code <= KeyCode.Z)
        {
            inputStr = code.ToString();
        }
        // 상단 숫자키 입력 (Alpha0 ~ Alpha9 -> 0 ~ 9)
        else if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9)
        {
            inputStr = code.ToString().Replace("Alpha", "");
        }
        // 우측 키패드 숫자 입력 (Keypad0 ~ Keypad9 -> 0 ~ 9)
        else if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9)
        {
            inputStr = code.ToString().Replace("Keypad", "");
        }
        // 스페이스바 처리
        else if (code == KeyCode.Space)
        {
            inputStr = " ";
        }

        // 입력할 글자가 있다면 모니터에 추가
        if (!string.IsNullOrEmpty(inputStr))
        {
            MonitorManager.Instance.AppendCharacter(inputStr);
        }
    }

    private void MapKeysAutomatically()
    {
        keyTransformMap.Clear();
        int mappedCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string name = child.name;

            if (!name.StartsWith("Keycap_")) continue;

            string rawKey = name.Substring(7);
            KeyCode targetCode = KeyCode.None;

            if (rawKey.Length == 1 && char.IsLetter(rawKey[0]))
            {
                Enum.TryParse(rawKey, true, out targetCode);
            }
            else if (rawKey.Length == 1 && char.IsDigit(rawKey[0]))
            {
                Enum.TryParse("Alpha" + rawKey, true, out targetCode);
            }
            else if (rawKey.StartsWith("T_") && rawKey.Length == 3 && char.IsDigit(rawKey[2]))
            {
                string num = rawKey.Substring(2);
                Enum.TryParse("Keypad" + num, true, out targetCode);
            }
            else
            {
                switch (rawKey.ToLower())
                {
                    case "esc": targetCode = KeyCode.Escape; break;
                    case "backspace": targetCode = KeyCode.Backspace; break;
                    case "tap": targetCode = KeyCode.Tab; break;
                    case "capslock": targetCode = KeyCode.CapsLock; break;
                    case "enter": targetCode = KeyCode.Return; break;
                    case "t_enter": targetCode = KeyCode.KeypadEnter; break;
                    case "space": targetCode = KeyCode.Space; break;
                    case "l_shift": targetCode = KeyCode.LeftShift; break;
                    case "r_shift": targetCode = KeyCode.RightShift; break;
                    case "l_ctrl": targetCode = KeyCode.LeftControl; break;
                    case "r_ctrl": targetCode = KeyCode.RightControl; break;
                    case "alt": targetCode = KeyCode.LeftAlt; break;
                    case "r_alt": targetCode = KeyCode.RightAlt; break;
                    case "window": targetCode = KeyCode.LeftWindows; break;
                    case "r_window": targetCode = KeyCode.RightWindows; break;
                    case "up": targetCode = KeyCode.UpArrow; break;
                    case "down": targetCode = KeyCode.DownArrow; break;
                    case "left": targetCode = KeyCode.LeftArrow; break;
                    case "right": targetCode = KeyCode.RightArrow; break;
                    case "pgup": targetCode = KeyCode.PageUp; break;
                    case "pgdn": targetCode = KeyCode.PageDown; break;
                    case "home": targetCode = KeyCode.Home; break;
                    case "end": targetCode = KeyCode.End; break;
                    case "delete": targetCode = KeyCode.Delete; break;
                    case "numlock": targetCode = KeyCode.Numlock; break;
                    case "scrlk": targetCode = KeyCode.ScrollLock; break;
                    case "pause": targetCode = KeyCode.Pause; break;
                    case "prtscn": targetCode = KeyCode.Print; break;

                    default:
                        if (Enum.TryParse(rawKey, true, out KeyCode fKey))
                        {
                            targetCode = fKey;
                        }
                        break;
                }
            }

            if (targetCode != KeyCode.None && Enum.IsDefined(typeof(KeyCode), targetCode))
            {
                if (!keyTransformMap.ContainsKey(targetCode))
                {
                    keyTransformMap.Add(targetCode, child);
                    mappedCount++;
                }
            }
        }

        Debug.Log($"[KeyboardInputJumper] 총 {mappedCount}개 키캡 매핑 완료.");
    }

    // [정밀 착지 점프 루틴] 레이캐스트 기반 정확한 키캡 표면 밀착 착지
    private IEnumerator JumpToKeyRoutine(Transform targetKey)
    {
        isJumping = true;

        if (playerTransform == null)
        {
            InitializePlayerReference();
            if (playerTransform == null)
            {
                isJumping = false;
                yield break;
            }
        }

        // 1. 점프 시작: 물리 연산 및 중력 가속도 누적 일시 차단
        if (cachedPlayerLocomotion != null)
        {
            cachedPlayerLocomotion.ResetAllVelocities();
            cachedPlayerLocomotion.enabled = false;
        }

        if (cachedPlayerRb != null)
        {
            cachedPlayerRb.linearVelocity = Vector3.zero;
            cachedPlayerRb.angularVelocity = Vector3.zero;
            cachedPlayerRb.isKinematic = true;
        }

        Vector3 startPos = playerTransform.position;

        // 2. 키캡의 표면 중심 및 정확한 착지 높이 계산 (Raycast 기반 정밀 바닥 감지)
        Vector3 targetCenter = targetKey.position;
        Renderer keyRenderer = targetKey.GetComponent<Renderer>();
        Collider keyCollider = targetKey.GetComponent<Collider>();

        if (keyRenderer != null)
        {
            targetCenter = keyRenderer.bounds.center;
        }
        else if (keyCollider != null)
        {
            targetCenter = keyCollider.bounds.center;
        }

        Vector3 landingSurfacePos = targetCenter;
        bool surfaceFound = false;

        // 키캡 중심 위쪽에서 아래로 레이캐스트하여 실제 충돌 표면 좌표를 찾음
        Vector3 rayStart = targetCenter + (Vector3.up * 1.0f);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 2.0f, ~0, QueryTriggerInteraction.Ignore);

        // 해당 키캡(또는 자식)의 콜라이더와 맞닿은 점 탐색
        foreach (var hit in hits)
        {
            if (hit.transform == targetKey || hit.transform.IsChildOf(targetKey) || hit.transform.parent == targetKey.parent)
            {
                landingSurfacePos = hit.point;
                surfaceFound = true;
                break;
            }
        }

        if (!surfaceFound && hits.Length > 0)
        {
            // 가장 높은 충돌 지점 선택
            float highestY = float.MinValue;
            foreach (var hit in hits)
            {
                if (hit.point.y > highestY)
                {
                    highestY = hit.point.y;
                    landingSurfacePos = hit.point;
                    surfaceFound = true;
                }
            }
        }

        if (!surfaceFound)
        {
            if (keyRenderer != null)
            {
                landingSurfacePos = new Vector3(keyRenderer.bounds.center.x, keyRenderer.bounds.max.y, keyRenderer.bounds.center.z);
            }
            else if (keyCollider != null)
            {
                landingSurfacePos = new Vector3(keyCollider.bounds.center.x, keyCollider.bounds.max.y, keyCollider.bounds.center.z);
            }
        }

        // 3. 최종 착지 목표 위치: 키캡 표면에 밀착 (미세 오프셋 적용)
        Vector3 endPos = landingSurfacePos + (Vector3.up * playerYOffset);

        float elapsed = 0f;

        try
        {
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float percent = Mathf.Clamp01(elapsed / jumpDuration);

                // 수평 이동 보간
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

                // 포물선 곡선 (점프 아치)
                float animatedY = Mathf.Lerp(startPos.y, endPos.y, percent) + (Mathf.Sin(percent * Mathf.PI) * jumpHeight);
                currentPos.y = animatedY;

                playerTransform.position = currentPos;
                yield return null;
            }

            // [착지 완료] 최종 위치 고정 및 Rigidbody 위치 동기화
            playerTransform.position = endPos;
            if (cachedPlayerRb != null)
            {
                cachedPlayerRb.position = endPos;
            }
        }
        finally
        {
            // [안전 복구] 물리 및 로코모션 완벽 동기화 복구
            if (cachedPlayerRb != null)
            {
                cachedPlayerRb.position = endPos;
                cachedPlayerRb.isKinematic = false;
                cachedPlayerRb.linearVelocity = Vector3.zero;
                cachedPlayerRb.angularVelocity = Vector3.zero;
            }

            if (cachedPlayerLocomotion != null)
            {
                cachedPlayerLocomotion.ResetAllVelocities();
                cachedPlayerLocomotion.enabled = true;
                cachedPlayerLocomotion.ResetAllVelocities();
            }

            isJumping = false;
        }
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 안전 복구
        if (cachedPlayerRb != null)
        {
            cachedPlayerRb.isKinematic = false;
            cachedPlayerRb.linearVelocity = Vector3.zero;
            cachedPlayerRb.angularVelocity = Vector3.zero;
        }

        if (cachedPlayerLocomotion != null)
        {
            cachedPlayerLocomotion.enabled = true;
            cachedPlayerLocomotion.ResetAllVelocities();
        }

        isJumping = false;
    }
}

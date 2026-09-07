using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 3D TextMeshPro 사용

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 위치는 월드 상에 완전히 고정된 채, 움직이는 플레이어를 향해 회전만 하여 앞면을 보여주는 3D 말풍선 컴포넌트.
    /// </summary>
    public class FixedPositionLookAtPlayer3D : ProcessBase
    {
        public enum TargetType
        {
            [Tooltip("움직이는 플레이어 캐릭터 오브젝트(targetPlayer)의 위치를 직접 바라봅니다.")]
            PlayerCharacter,
            [Tooltip("플레이어의 메인 카메라(Camera.main) 위치를 바라봅니다.")]
            MainCamera
        }

        [Header("Chatbox 3D References")]
        [Tooltip("말풍선 오브젝트 (배경 이미지 + 텍스트 포함된 부모 오브젝트)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("UI 텍스트가 아닌 3D TextMeshPro 컴포넌트")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Target & Facing Settings")]
        [Tooltip("누구를 바라볼 것인가? (플레이어 캐릭터 또는 카메라)")]
        [SerializeField] private TargetType lookTargetType = TargetType.PlayerCharacter;

        [Tooltip("lookTargetType이 PlayerCharacter일 때: 바라볼 플레이어 Transform (비워두면 'Player' 태그 탐색)")]
        [SerializeField] private Transform targetPlayer;

        [Tooltip("체크 시 말풍선이 위아래로 기울어지지 않고, 수직(Y축)으로 똑바로 선 채 좌우로만 고개를 돌립니다. (권장)")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("텍스트나 메쉬 리소스 방향 때문에 글자가 좌우 반전되어 보일 경우 체크합니다.")]
        [SerializeField] private bool reverseFacing = false;

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display Settings")]
        [Tooltip("말풍선이 떠 있을 시간 (초)")]
        [SerializeField] private float displayDuration = 3f;

        private Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            ParseCSV();

            if (chatboxVisual != null)
            {
                chatboxVisual.SetActive(false);
            }

            // targetPlayer가 지정되어 있지 않다면 'Player' 태그를 가진 오브젝트를 자동으로 찾음
            if (targetPlayer == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    targetPlayer = playerObj.transform;
                }
            }
        }

        private void ParseCSV()
        {
            if (csvFile == null) return;

            string[] lines = csvFile.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] columns = line.Split(new char[] { ',' }, 2);
                if (columns.Length >= 2)
                {
                    string id = columns[0].Trim();
                    string text = columns[1].Trim().Replace("\\n", "\n");
                    _dialogueData[id] = text;
                }
            }
        }

        public override void Execute()
        {
            if (_dialogueData.TryGetValue(targetTextId, out string dialogue))
            {
                if (chatText != null)
                {
                    chatText.text = dialogue;
                }

                if (chatboxVisual != null)
                {
                    chatboxVisual.SetActive(true);
                    
                    // 켜지는 즉시 회전 각도 조정
                    RotateTowardsTarget();
                }

                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(HideRoutine());

                IsOn = true;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CSV에서 '{targetTextId}' ID를 찾을 수 없습니다.");
                IsOn = false;
            }
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(displayDuration);

            if (chatboxVisual != null)
            {
                chatboxVisual.SetActive(false);
            }

            IsOn = false;
            _hideCoroutine = null;
        }

        private void LateUpdate()
        {
            // 말풍선이 켜져 있을 때만 제자리에서 회전 수행
            if (chatboxVisual == null || !chatboxVisual.activeInHierarchy) return;

            RotateTowardsTarget();
        }

        /// <summary>
        /// 위치 이동 없이, 제자리에서 플레이어(또는 카메라) 쪽으로 앞면이 오도록 회전시킵니다.
        /// </summary>
        private void RotateTowardsTarget()
        {
            Vector3 targetPosition = Vector3.zero;

            // 1. 타겟 위치 파악
            if (lookTargetType == TargetType.PlayerCharacter)
            {
                if (targetPlayer == null)
                {
                    // 실시간으로 플레이어 다시 탐색
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) targetPlayer = playerObj.transform;
                }

                if (targetPlayer != null)
                {
                    targetPosition = targetPlayer.position;
                }
                else return;
            }
            else // MainCamera
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    targetPosition = mainCam.transform.position;
                }
                else return;
            }

            // 2. 말풍선 위치에서 타겟 위치를 바라보는 방향 벡터 계산
            Vector3 dirToTarget = targetPosition - chatboxVisual.transform.position;

            // 3. Y축 고정 옵션 (높이 차이 때문에 글자가 상하로 눕지 않게 방지)
            if (lockYAxis)
            {
                dirToTarget.y = 0f;
            }

            // 4. 방향을 바라보도록 회전 적용
            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                // 말풍선의 정면(+Z)이 타겟을 바라보게 설정
                Quaternion targetRotation = Quaternion.LookRotation(dirToTarget.normalized);

                // 리소스나 텍스트가 반대로 보일 경우 180도 회전
                if (reverseFacing)
                {
                    targetRotation *= Quaternion.Euler(0, 180f, 0);
                }

                chatboxVisual.transform.rotation = targetRotation;
            }
        }
    }
}
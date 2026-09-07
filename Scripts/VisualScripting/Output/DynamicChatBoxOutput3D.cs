using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 3D TextMeshPro 사용

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 캐릭터가 이동하고 마우스로 카메라를 회전하는 환경에 최적화된 3D 말풍선 컴포넌트.
    /// Y축 고정 빌보드 방식을 사용하여 카메라 상하 각도에 상관없이 글자가 눕지 않고 항상 정면을 유지합니다.
    /// </summary>
    public class DynamicChatboxOutput3D : ProcessBase
    {
        public enum BillboardMode
        {
            [Tooltip("3인칭/마우스 회전 카메라 권장: 글자가 위아래로 눕지 않고 항상 똑바로 선 채 카메라 좌우 방향만 바라봄")]
            YAxisOnly,
            [Tooltip("카메라 화면과 완전 평행")]
            CameraParallel,
            [Tooltip("카메라 위치 방향을 직접 조준")]
            LookAtCamera
        }

        [Header("Chatbox 3D References")]
        [Tooltip("말풍선 오브젝트 (배경 이미지 + 텍스트 포함된 부모 오브젝트)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("UI 텍스트가 아닌 3D TextMeshPro 컴포넌트")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Target & Position Settings (Moving Character)")]
        [Tooltip("추적할 캐릭터 Transform (비워두면 이 스크립트가 붙은 오브젝트 위치 사용)")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("캐릭터 위치 기준 말풍선 높이 오프셋 (예: Y = 2.2 이면 머리 위 2.2m)")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 2.2f, 0f);

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display & Billboard Settings")]
        [Tooltip("말풍선이 떠 있을 시간 (초)")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("체크 시 카메라 회전에 맞춰 실시간 빌보드 작동")]
        [SerializeField] private bool useBillboard = true;

        [Tooltip("빌보드 동작 모드 (마우스 회전 카메라는 YAxisOnly 권장)")]
        [SerializeField] private BillboardMode billboardMode = BillboardMode.YAxisOnly;

        [Tooltip("텍스트나 메쉬 리소스 방향에 따라 글자가 좌우 반전되어 보일 경우 체크")]
        [SerializeField] private bool reverseFacing = false;

        private Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            ParseCSV();

            if (chatboxVisual != null)
            {
                chatboxVisual.SetActive(false);
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

                    // 실행 순간 즉시 위치 및 회전 맞춤
                    UpdatePosition();
                    UpdateBillboard();
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
            if (chatboxVisual == null || !chatboxVisual.activeInHierarchy) return;

            // 1. 캐릭터 이동에 맞춰 위치 추적
            UpdatePosition();

            // 2. 마우스 카메라 회전에 맞춰 빌보드 추적
            if (useBillboard)
            {
                UpdateBillboard();
            }
        }

        /// <summary>
        /// 캐릭터 이동 시 머리 위 위치를 실시간으로 맞춥니다.
        /// </summary>
        private void UpdatePosition()
        {
            Transform followTarget = targetTransform != null ? targetTransform : transform;
            chatboxVisual.transform.position = followTarget.position + positionOffset;
        }

        /// <summary>
        /// 마우스 회전 카메라 각도에 맞춰 말풍선 방향을 회전시킵니다.
        /// </summary>
        private void UpdateBillboard()
        {
            Camera currentCam = Camera.main;
            if (currentCam == null) return;

            switch (billboardMode)
            {
                case BillboardMode.YAxisOnly:
                    // 카메라 위치 방향을 바라보되, Y축(높이 차이)을 제거하여 상하로 눕지 않게 방지
                    Vector3 directionToCam = currentCam.transform.position - chatboxVisual.transform.position;
                    directionToCam.y = 0f; // Y축 고정 (수직으로 똑바로 서게 함)

                    if (directionToCam.sqrMagnitude > 0.001f)
                    {
                        // 텍스트 정면 방향 계산 (-directionToCam으로 바라보게 함)
                        Quaternion targetRot = Quaternion.LookRotation(-directionToCam.normalized);
                        if (reverseFacing) targetRot *= Quaternion.Euler(0, 180f, 0);
                        chatboxVisual.transform.rotation = targetRot;
                    }
                    break;

                case BillboardMode.CameraParallel:
                    Quaternion parallelRot = currentCam.transform.rotation;
                    if (reverseFacing) parallelRot *= Quaternion.Euler(0, 180f, 0);
                    chatboxVisual.transform.rotation = parallelRot;
                    break;

                case BillboardMode.LookAtCamera:
                    Vector3 dirToCam = chatboxVisual.transform.position - currentCam.transform.position;
                    if (dirToCam.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dirToCam.normalized);
                        if (reverseFacing) lookRot *= Quaternion.Euler(0, 180f, 0);
                        chatboxVisual.transform.rotation = lookRot;
                    }
                    break;
            }
        }
    }
}
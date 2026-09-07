/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 3D TextMeshPro�� ���� ���

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// �Է� ��ȣ�� ������ 3D ������ ��ǳ���� ���� Output ���.
    /// 3D �ؽ�Ʈ(TextMeshPro)�� ����ϸ� Canvas�� �ʿ����� �ʽ��ϴ�.
    /// </summary>
    public class ChatboxOutput3D : ProcessBase
    {
        [Header("Chatbox 3D References")]
        [Tooltip("��ǳ�� ��ü�� �Ѱ� �� �ֻ��� 3D ������Ʈ (��� �̹��� + �ؽ�Ʈ ����)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("UI �ؽ�Ʈ�� �ƴ� 3D TextMeshPro ������Ʈ")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display & Rotation Settings")]
        [Tooltip("��ǳ���� �� �ִ� �ð� (��)")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("üũ �� ī�޶� ��ġ�� ���� �ٶ�(Billboard). üũ ���� �� ī�޶� ȭ��� �Ϻ��� ���� ����(Flat).")]
        [SerializeField] private bool useBillboard = false;

        private Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Camera _mainCamera;
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            _mainCamera = Camera.main;
            ParseCSV();

            if (chatboxVisual != null)
                chatboxVisual.SetActive(false);
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
                chatText.text = dialogue;
                chatboxVisual.SetActive(true);

                // [�߰��� �κ�] 
                // ������ ��尡 �����ִ���, ��ǳ���� ������ '���� 1ȸ'�� ī�޶� �ٶ󺸰� ����ϴ�.
                if (!useBillboard && _mainCamera != null)
                {
                    chatboxVisual.transform.LookAt(
                        chatboxVisual.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                        _mainCamera.transform.rotation * Vector3.up);
                }

                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(HideRoutine());

                IsOn = true;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CSV�� '{targetTextId}' ID�� �����ϴ�.");
                IsOn = false;
            }
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(displayDuration);
            chatboxVisual.SetActive(false);
            IsOn = false;
            _hideCoroutine = null;
        }

        private void LateUpdate()
        {
            if (!chatboxVisual.activeSelf || _mainCamera == null) return;

            if (useBillboard)
            {
                // [������ ���] 
                // ������Ʈ�� ī�޶��� �߽� ��ǥ�� ���� ������ �����ϴ�. (�ؽ�Ʈ�� �������� �ʵ��� ���� ����)
                chatboxVisual.transform.LookAt(
                    chatboxVisual.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
            //else
            //{
            //    // [�÷� ���] 
            //    // ������Ʈ�� ȸ������ ī�޶��� ȸ������ ������ �����ϰ� ����ϴ�.
            //    // 3D ������ ������ ��ġ UIó�� ȭ�鿡 ����/�������� �ݵ��ϰ� �������˴ϴ�.
            //    chatboxVisual.transform.rotation = _mainCamera.transform.rotation;
            //}
        }
    }
}

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 3D TextMeshPro를 사용하기 위함

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 입력 신호를 받으면 3D 월드 공간에 말풍선을 띄우는 Output 컴포넌트.
    /// 3D 텍스트(TextMeshPro)를 사용하여 Canvas가 필요하지 않습니다.
    /// </summary>
    public class ChatboxOutput3D : ProcessBase
    {
        [Header("Chatbox 3D References")]
        [Tooltip("말풍선 오브젝트 (배경 이미지 + 텍스트 포함된 부모 오브젝트)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("UI 텍스트가 아닌 3D TextMeshPro 컴포넌트")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display & Rotation Settings")]
        [Tooltip("말풍선이 떠 있을 시간 (초)")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("체크 시 카메라 위치/각도에 맞춰 항상 정면을 바라봄(Billboard). 체크 해제 시 실행 순간의 카메라 각도로 고정.")]
        [SerializeField] private bool useBillboard = false;

        private Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Camera _mainCamera;
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            _mainCamera = Camera.main;
            ParseCSV();

            if (chatboxVisual != null)
                chatboxVisual.SetActive(false);
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
                chatText.text = dialogue;
                chatboxVisual.SetActive(true);

                // 빌보드가 꺼져 있더라도 실행되는 그 첫 순간에는 카메라를 똑바로 바라보도록 설정
                if (!useBillboard && _mainCamera != null)
                {
                    chatboxVisual.transform.rotation = _mainCamera.transform.rotation;
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
            chatboxVisual.SetActive(false);
            IsOn = false;
            _hideCoroutine = null;
        }

        private void LateUpdate()
        {
            if (!chatboxVisual.activeSelf || _mainCamera == null) return;

            if (useBillboard)
            {
                // [개선된 빌보드 방식]
                // 카메라의 회전 값을 그대로 복사하여 덮어씌웁니다.
                // 이 방식을 사용하면 플레이어가 밑에서 위를 올려다보거나 각도가 완전히 틀어져도 
                // 말풍선이 카메라 화면과 완벽하게 평행을 유지하므로 글자가 칼날처럼 얇아지거나 안 보이는 현상이 해결됩니다.
                chatboxVisual.transform.rotation = _mainCamera.transform.rotation;
            }
        }
    }
}
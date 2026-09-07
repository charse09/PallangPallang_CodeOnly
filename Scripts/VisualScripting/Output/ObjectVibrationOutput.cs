using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 기계 오브젝트를 주기적으로 진동시키고, 위에 접촉해 있는 플레이어도 같이 비주얼적으로 흔들리게 만드는 Output 모듈
    /// </summary>
    public class ObjectVibrationOutput : ProcessBase
    {
        [Header("Vibration Target")]
        [Tooltip("진동시킬 기계의 비주얼 트랜스폼 (부모 껍데기 말고 메쉬 오브젝트 권장)")]
        [SerializeField] private Transform machineVisual;

        [Header("Vibration Settings")]
        [Tooltip("진동이 유지될 총 시간 (초 단위)")]
        [SerializeField] private float duration = 3f;

        [Tooltip("진동 속도/빈도 (높을수록 치직거리며 빠르게 떪)")]
        [SerializeField] private float shakeFrequency = 25f;

        [Tooltip("기계가 흔들리는 최대 반경 크기")]
        [SerializeField] private float machineMagnitude = 0.08f;

        [Tooltip("플레이어가 기계에 닿았을 때 흔들리는 최대 반경 크기")]
        [SerializeField] private float playerMagnitude = 0.05f;

        private Vector3 _originalMachinePos;
        private List<Transform> _playersOnMachine = new List<Transform>();
        private Dictionary<Transform, Vector3> _playerOriginalPosMap = new Dictionary<Transform, Vector3>();
        private bool _isShaking = false;

        private void Start()
        {
            if (machineVisual == null) machineVisual = transform;
            _originalMachinePos = machineVisual.localPosition;
        }

        public override void Execute()
        {
            IsOn = false;

            if (machineVisual == null) machineVisual = transform;
            _originalMachinePos = machineVisual.localPosition;

            if (!_isShaking)
            {
                StartCoroutine(VibrationRoutine());
            }
        }

        private IEnumerator VibrationRoutine()
        {
            _isShaking = true;
            float elapsed = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 기계 물리 진동 작동 시작!");
#endif

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 시간이 갈수록 진동이 서서히 줄어들도록 감쇄값 계산 (원치 않으면 페이드 아웃 삭제 가능)
                float fadeOut = Mathf.Lerp(1f, 0f, progress);

                // 1. 기계 오브젝트 무작위 사인파 진동
                float machineShakeX = Mathf.Sin(Time.time * shakeFrequency) * machineMagnitude * fadeOut * Random.Range(-1f, 1f);
                float machineShakeZ = Mathf.Cos(Time.time * shakeFrequency) * machineMagnitude * fadeOut * Random.Range(-1f, 1f);
                machineVisual.localPosition = _originalMachinePos + new Vector3(machineShakeX, 0f, machineShakeZ);

                // 2. 접촉 중인 플레이어 캐릭터 진동 처리
                // 씬 청소 (오브젝트 파괴 대비)
                _playersOnMachine.RemoveAll(p => p == null);
                foreach (var playerVisual in _playersOnMachine)
                {
                    if (!_playerOriginalPosMap.ContainsKey(playerVisual))
                    {
                        _playerOriginalPosMap[playerVisual] = playerVisual.localPosition;
                    }

                    float playerShakeX = Mathf.Sin(Time.time * (shakeFrequency * 1.2f)) * playerMagnitude * fadeOut * Random.Range(-1f, 1f);
                    float playerShakeZ = Mathf.Cos(Time.time * (shakeFrequency * 1.2f)) * playerMagnitude * fadeOut * Random.Range(-1f, 1f);

                    playerVisual.localPosition = _playerOriginalPosMap[playerVisual] + new Vector3(playerShakeX, 0f, playerShakeZ);
                }

                yield return null;
            }

            // 원래 위치로 완벽 보정 리셋
            machineVisual.localPosition = _originalMachinePos;
            foreach (var playerVisual in _playersOnMachine)
            {
                if (playerVisual != null && _playerOriginalPosMap.ContainsKey(playerVisual))
                {
                    playerVisual.localPosition = _playerOriginalPosMap[playerVisual];
                }
            }

            _playerOriginalPosMap.Clear();
            _isShaking = false;

            // 프레임워크 규칙: 완료 신호 반환
            IsOn = true;
        }

        // --- 트리거 접촉 감지 영역 ---
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                // 프리랩 구조 분석 기반: 플레이어의 비주얼 루트인 'ani_test5 (1)'를 찾거나 최상위 자식을 가져옵니다.
                Transform pVisual = collision.transform.Find("Berry_Ain");
                if (pVisual == null && collision.transform.childCount > 0) pVisual = collision.transform.GetChild(0);

                if (pVisual != null && !_playersOnMachine.Contains(pVisual))
                {
                    _playersOnMachine.Add(pVisual);
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                Transform pVisual = collision.transform.Find("Berry_Ain");
                if (pVisual == null && collision.transform.childCount > 0) pVisual = collision.transform.GetChild(0);

                if (pVisual != null && _playersOnMachine.Contains(pVisual))
                {
                    if (_playerOriginalPosMap.ContainsKey(pVisual))
                    {
                        pVisual.localPosition = _playerOriginalPosMap[pVisual];
                        _playerOriginalPosMap.Remove(pVisual);
                    }
                    _playersOnMachine.Remove(pVisual);
                }
            }
        }
    }
}
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// PaperFlutterOutput과 동일한 펄럭이며 떨어지는 움직임을 사용하지만,
    /// 목적지에 도착할 때 캐릭터가 얼굴이 바닥을 향하도록(엎드린 채로) 착지합니다.
    ///
    /// [PaperFlutterOutput과의 차이점]
    /// - PaperFlutterOutput  : PaperState를 토글하여 캐릭터를 눕힘 → 천장을 바라보며 도착.
    /// - PaperFlutterFaceDownOutput : PaperState를 직접 변경/유지하면서, rotation을 직접 제어하여
    ///   얼굴이 바닥을 향하도록(엎드린 자세로) 착지.
    ///   Face Down Rotation Offset(기본값 180, 0, 0)을 targetDestination.rotation에 추가로 곱해
    ///   뒤집힌 방향을 만듭니다. 캐릭터 모델 구조에 따라 이 값을 조정하세요.
    /// </summary>
    public class PaperFlutterFaceDownPaperStateOutput : ProcessBase
    {
        [Header("References")]
        [Tooltip("펄럭이며 떨어뜨릴 대상 오브젝트 (플레이어 또는 다른 오브젝트)")]
        [SerializeField] private Transform paperObject;

        [Tooltip("최종 착지 위치 / 회전 기준 Transform")]
        [SerializeField] private Transform targetDestination;

        [Header("Flutter Settings")]
        [Tooltip("목적지까지 떨어지는 데 걸리는 시간 (초)")]
        [SerializeField] private float duration = 3f;

        [Tooltip("좌우로 얼마나 넓게 펄럭일지 (단위: 미터)")]
        [SerializeField] private float flutterWidth = 2f;

        [Tooltip("얼마나 빠르게 펄럭일지 (속도)")]
        [SerializeField] private float flutterSpeed = 3f;

        [Tooltip("떨어지면서 기울어질 최대 각도 (도)")]
        [SerializeField] private float flutterRotationAngle = 45f;

        [Header("Face Down Settings")]
        [Tooltip("착지 시 얼굴이 바닥을 향하도록 targetDestination.rotation에 추가로 곱하는 회전값.\n" +
                 "기본값 (180, 0, 0)은 X축 기준으로 뒤집어 얼굴이 아래를 향하게 합니다.\n" +
                 "캐릭터 모델 방향에 따라 값을 조정하세요.")]
        [SerializeField] private Vector3 faceDownRotationOffset = new Vector3(180f, 0f, 0f);

        public override void Execute()
        {
            if (paperObject == null || targetDestination == null)
            {
                Debug.LogError($"<color=red>[{gameObject.name}] PaperFlutterFaceDownOutput: " +
                               "PaperObject 또는 TargetDestination이 없습니다!</color>");
                IsOn = true;
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FlutterFaceDownRoutine());
        }

        private IEnumerator FlutterFaceDownRoutine()
        {
            IsOn = false;

            // paperObject 또는 그 자식/부모 오브젝트에서 PlayerLocomotion 탐색
            PlayerLocomotion playerLocomotion = paperObject.GetComponent<PlayerLocomotion>();
            if (playerLocomotion == null)
            {
                playerLocomotion = paperObject.GetComponentInParent<PlayerLocomotion>();
            }

            // PlayerLocomotion이 존재할 경우 PaperState를 true로 설정
            if (playerLocomotion != null)
            {
                playerLocomotion.SetAbsolutePaperState(true);
            }

            float elapsed = 0f;

            // 시작 위치 / 회전 저장
            Vector3 startPos = paperObject.position;
            Quaternion startRot = paperObject.rotation;

            // 목적지 위치
            Vector3 targetPos = targetDestination.position;

            // 목적지 회전에 faceDownRotationOffset을 추가로 적용 → 얼굴이 바닥을 향하는 rotation
            Quaternion targetRot = targetDestination.rotation * Quaternion.Euler(faceDownRotationOffset);

            // 매번 다른 위상(phase)으로 시작해서 펄럭임이 규칙적으로 보이지 않도록 랜덤 오프셋
            float randomOffset = Random.Range(0f, 100f);

#if UNITY_EDITOR
            Debug.Log($"<color=lime>[{gameObject.name}]</color> [PaperFlutterFaceDownOutput] " +
                      $"펄럭이며 낙하 시작. 목표 엎드린 rotation: {targetRot.eulerAngles}");
#endif

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 진행도 (0 → 1)
                float t = elapsed / duration;

                // 스무스 스텝: 처음엔 천천히, 중간엔 빠르게, 끝엔 천천히 착지
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 1. 기본 이동 위치
                Vector3 basePos = Vector3.Lerp(startPos, targetPos, smoothT);

                // 2. 펄럭임 진폭 감쇠 (착지할수록 흔들림이 줄어듦)
                float damping = 1f - smoothT;
                float currentWidth = flutterWidth * damping;

                // 3. X / Z 방향으로 불규칙한 펄럭임 오프셋 계산
                float offsetX = Mathf.Sin((elapsed + randomOffset) * flutterSpeed) * currentWidth;
                float offsetZ = Mathf.Cos((elapsed + randomOffset) * flutterSpeed * 0.8f) * currentWidth;

                paperObject.position = basePos + new Vector3(offsetX, 0f, offsetZ);

                // 4. 펄럭이며 기울어지는 회전값 계산
                float rotX = Mathf.Sin((elapsed + randomOffset) * flutterSpeed) * flutterRotationAngle * damping;
                float rotZ = Mathf.Cos((elapsed + randomOffset) * flutterSpeed * 1.2f) * flutterRotationAngle * damping;

                Quaternion flutterRot = Quaternion.Euler(rotX, 0f, rotZ);

                // 시작 rotation → 엎드린 targetRot으로 부드럽게 전환하면서 펄럭임 추가
                Quaternion currentBaseRot = Quaternion.Slerp(startRot, targetRot, smoothT);
                paperObject.rotation = currentBaseRot * flutterRot;

                yield return null;
            }

            // 착지: 정확히 목적지 위치 / 엎드린 rotation으로 확정
            paperObject.position = targetPos;
            paperObject.rotation = targetRot;

            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [PaperFlutterFaceDownOutput] " +
                      "착지 완료! (엎드린 자세)");
#endif
        }
    }
}
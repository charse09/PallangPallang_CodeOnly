using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 인스펙터에서 카메라의 각 웨이포인트(목표 지점) 연출을 설정하기 위한 구조체
    /// </summary>
    [System.Serializable]
    public struct CameraWaypoint
    {
        [Tooltip("도달할 목표 지점 오브젝트 (이 오브젝트의 위치와 회전값을 그대로 따라갑니다)")]
        public Transform targetTransform;

        [Tooltip("이 지점에 도달했을 때의 카메라 FOV (시야각, 기본값 60)")]
        [Range(10f, 120f)]
        public float targetFOV;

        [Tooltip("이전 지점에서 현재 지점까지 이동하는 데 걸리는 시간 (초)")]
        public float transitionDuration;

        [Tooltip("이동 속도의 변화(가속/감속)를 조절하는 커브")]
        public AnimationCurve easeCurve;
    }

    /// <summary>
    /// 설정된 여러 개의 웨이포인트를 따라 카메라의 위치, 회전, FOV를 부드럽게 이동시키는 컷신 연출용 Output
    /// </summary>
    public class CameraPathSequenceOutput : ProcessBase
    {
        [Header("Camera Settings")]
        [Tooltip("연출에 사용할 카메라 (비워두면 씬의 Main Camera를 자동으로 찾습니다)")]
        [SerializeField] private Camera targetCamera;

        [Header("Sequence Settings")]
        [Tooltip("카메라가 순차적으로 이동할 경로 및 연출 설정 리스트")]
        [SerializeField] private List<CameraWaypoint> pathSequence = new List<CameraWaypoint>();

        private bool isPlaying = false;

        private void Start()
        {
            // 타겟 카메라가 설정되지 않았다면 메인 카메라를 자동으로 할당
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            // 리스트를 초기화할 때 커브의 기본값을 부드러운 S자(EaseInOut)로 설정
            for (int i = 0; i < pathSequence.Count; i++)
            {
                if (pathSequence[i].easeCurve == null || pathSequence[i].easeCurve.keys.Length == 0)
                {
                    var wp = pathSequence[i];
                    wp.easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    pathSequence[i] = wp;
                }
            }
        }

        public override void Execute()
        {
            if (!isPlaying && targetCamera != null && pathSequence.Count > 0)
            {
                StartCoroutine(PlayCameraSequence());
            }
        }

        private IEnumerator PlayCameraSequence()
        {
            isPlaying = true;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 카메라 연출 시퀀스 시작! (총 {pathSequence.Count}단계)");
#endif

            // 순차적으로 웨이포인트 이동 처리
            for (int i = 0; i < pathSequence.Count; i++)
            {
                var wp = pathSequence[i];
                if (wp.targetTransform == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] {i}번째 웨이포인트의 Target Transform이 비어있어 건너뜁니다.");
                    continue;
                }

                // 현재(시작) 상태 기록
                Vector3 startPos = targetCamera.transform.position;
                Quaternion startRot = targetCamera.transform.rotation;
                float startFOV = targetCamera.fieldOfView;

                float elapsed = 0f;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> Phase {i + 1}: {wp.targetTransform.name}으로 이동 중... (시간: {wp.transitionDuration}초, FOV: {wp.targetFOV})");
#endif

                // 지정된 시간 동안 이동
                while (elapsed < wp.transitionDuration)
                {
                    elapsed += Time.deltaTime;

                    // 진행도 (0.0 ~ 1.0)
                    float t = Mathf.Clamp01(elapsed / wp.transitionDuration);

                    // Ease 커브를 적용하여 가속/감속 처리 (커브가 없으면 SmoothStep 적용)
                    float easeT = (wp.easeCurve != null && wp.easeCurve.keys.Length > 0)
                        ? wp.easeCurve.Evaluate(t)
                        : Mathf.SmoothStep(0f, 1f, t);

                    // 위치, 회전, FOV 보간 적용
                    targetCamera.transform.position = Vector3.Lerp(startPos, wp.targetTransform.position, easeT);
                    targetCamera.transform.rotation = Quaternion.Slerp(startRot, wp.targetTransform.rotation, easeT);
                    targetCamera.fieldOfView = Mathf.Lerp(startFOV, wp.targetFOV, easeT);

                    yield return null;
                }

                // 오차 보정 (정확히 목표치에 맞춤)
                targetCamera.transform.position = wp.targetTransform.position;
                targetCamera.transform.rotation = wp.targetTransform.rotation;
                targetCamera.fieldOfView = wp.targetFOV;
            }

            isPlaying = false;
            IsOn = true; // 프레임워크 상태 갱신 (다음 연출이 있다면 실행됨)

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 카메라 연출 시퀀스 완료.");
#endif
        }
    }
}
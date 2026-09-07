using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트를 지정된 목표 지점까지 종이처럼 팔랑거리며 이동시키는 Output 노드
    /// </summary>
    public class PaperFlutterOutput : ProcessBase
    {
        [Header("References")]
        [Tooltip("팔랑거리며 떨어질 종이 오브젝트")]
        [SerializeField] private Transform paperObject;
        [Tooltip("최종적으로 도착할 목표 위치/회전값을 가진 빈 오브젝트")]
        [SerializeField] private Transform targetDestination;

        [Header("Flutter Settings")]
        [Tooltip("떨어지는 데 걸리는 총 시간")]
        [SerializeField] private float duration = 3f;

        [Tooltip("좌우로 얼마나 넓게 팔랑거릴지 (진폭)")]
        [SerializeField] private float flutterWidth = 2f;

        [Tooltip("얼마나 빨리 팔랑거릴지 (속도)")]
        [SerializeField] private float flutterSpeed = 3f;

        [Tooltip("떨어질 때 종이가 까딱거리는 회전 각도")]
        [SerializeField] private float flutterRotationAngle = 45f;

        public override void Execute()
        {
            if (paperObject == null || targetDestination == null)
            {
                Debug.LogError($"<color=red>[{gameObject.name}] PaperObject 또는 TargetDestination이 비어있습니다!</color>");
                IsOn = true;
                return;
            }

            // [유저 아이디어 적용!] paperObject에 플레이어 스크립트가 있는지 쓱 검사합니다.
            PlayerLocomotion playerScript = paperObject.GetComponent<PlayerLocomotion>();
            if (playerScript != null)
            {
                // 스크립트가 있다면, 작성해두셨던 프로퍼티를 호출해서 딱 한 번 발동시킵니다!
                // (set 프로퍼티 구조상 = true든 = false든 무조건 반대로 뒤집히며 발동됩니다)
                playerScript.PaperState = true;

#if UNITY_EDITOR
                Debug.Log($"<color=lime>[{gameObject.name}]</color> 떨어지는 종이가 플레이어입니다! Paper 상태를 발동시킵니다.");
#endif
            }

            StopAllCoroutines();
            StartCoroutine(FlutterRoutine());
        }

        private IEnumerator FlutterRoutine()
        {
            IsOn = false; // 행동 시작 (대기 걸기)
            float elapsed = 0f;

            // 시작할 때의 위치와 회전값 기억
            Vector3 startPos = paperObject.position;
            Quaternion startRot = paperObject.rotation;

            // 목표 위치와 회전값
            Vector3 targetPos = targetDestination.position;
            Quaternion targetRot = targetDestination.rotation;

            // 매번 똑같이 떨어지면 어색하므로, 사인 곡선의 시작점을 랜덤하게 줍니다.
            float randomOffset = Random.Range(0f, 100f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 진행도 (0 ~ 1)
                float t = elapsed / duration;

                // 스무스하게 떨어지도록 (처음엔 천천히, 중간에 빨랐다가, 끝에 천천히)
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 1. 기본 직선 이동 위치 계산
                Vector3 basePos = Vector3.Lerp(startPos, targetPos, smoothT);

                // 2. 목표에 가까워질수록 팔랑거림을 줄이는 '감쇠(Damping)' 값 (1 -> 0)
                float damping = 1f - smoothT;
                float currentWidth = flutterWidth * damping;

                // 3. X, Z축으로 팔랑거리는 오프셋 계산 (서로 다른 속도를 주어 불규칙하게 만듦)
                float offsetX = Mathf.Sin((elapsed + randomOffset) * flutterSpeed) * currentWidth;
                float offsetZ = Mathf.Cos((elapsed + randomOffset) * flutterSpeed * 0.8f) * currentWidth;

                // 위치 적용
                paperObject.position = basePos + new Vector3(offsetX, 0f, offsetZ);

                // 4. 회전 팔랑거림 계산
                float rotX = Mathf.Sin((elapsed + randomOffset) * flutterSpeed) * flutterRotationAngle * damping;
                float rotZ = Mathf.Cos((elapsed + randomOffset) * flutterSpeed * 1.2f) * flutterRotationAngle * damping;

                Quaternion flutterRot = Quaternion.Euler(rotX, 0f, rotZ);
                Quaternion currentBaseRot = Quaternion.Slerp(startRot, targetRot, smoothT);

                // 기본 회전값에 팔랑거림 회전값을 더해서 적용
                paperObject.rotation = currentBaseRot * flutterRot;

                yield return null;
            }

            // 시간이 다 되면 목표 지점에 오차 없이 100% 찰칵! 맞춤
            paperObject.position = targetPos;
            paperObject.rotation = targetRot;

            // [프레임워크 규칙] 행동이 끝났다고 부모(시퀀스)에게 알림
            IsOn = true;
        }
    }
}
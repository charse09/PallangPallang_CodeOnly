using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PaperFallMoveObject : ProcessBase
    {
        [Header("Target")]
        [SerializeField] private GameObject objectToMove;

        [Header("Input Lock")]
        [Tooltip("이동/점프/공격 등 입력을 받는 스크립트들을 넣으면 실행 중 비활성화됩니다.")]
        [SerializeField] private List<MonoBehaviour> inputScriptsToDisable;

        [Header("Force Lie Down")]
        [Tooltip("F키로 눕는 기능을 가진 스크립트의 함수 이름")]
        [SerializeField] private string lieDownFunctionName = "ForceLieDown";

        [Tooltip("함수가 없을 경우 강제로 적용할 누운 회전값")]
        [SerializeField] private Vector3 fallbackLieRotation = new Vector3(0f, 0f, 90f);

        [Header("Paper Falling")]
        [SerializeField] private float fallDuration = 4f;
        [SerializeField] private float fallSpeed = 1.5f;

        [Tooltip("좌우로 흔들리는 최대 각도")]
        [SerializeField] private float swingAngle = 35f;

        [Tooltip("좌우로 흔들리는 속도")]
        [SerializeField] private float swingSpeed = 3f;

        [Tooltip("피벗과 플레이어 사이의 거리. 클수록 바이킹처럼 크게 흔들립니다.")]
        [SerializeField] private float pivotRadius = 1.5f;

        [Header("Rotation")]
        [Tooltip("종이처럼 팔랑거리는 추가 회전 강도")]
        [SerializeField] private float flutterRotationAmount = 15f;

        [Header("Smooth Transition")]
        [Tooltip("낙하 시작 시 원래 자세에서 누운 자세(fallbackLieRotation)로 눕는 데 걸리는 시간")]
        [SerializeField] private float lieDownTransitionDuration = 0.5f;

        private Coroutine _moveCoroutine;

        public override void Execute()
        {
            IsOn = false;
            if (objectToMove == null)
            {
                IsOn = true;
                return;
            }

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(C_PaperFall());
        }

        private IEnumerator C_PaperFall()
        {
            DisableInput();

            // [변경] 시작 시 즉시 회전을 꺾어버리는 ForceLieDown()을 제거하고, 코루틴 내부에서 서서히 눕도록 처리합니다.
            // 필요하다면 SendMessage만 먼저 호출합니다.
            objectToMove.SendMessage(lieDownFunctionName, SendMessageOptions.DontRequireReceiver);

            Transform targetTransform = objectToMove.transform;

            // ---------------------------------------------------
            // [핵심 1] 자연스러운 위치 승계 (역산 계산)
            // ---------------------------------------------------
            // 시작하는 순간의 플레이어 실제 위치를 기준으로 피벗 위치를 역산합니다.
            // swing 계산(Mathf.Sin)이 t=0일 때 0이므로, 처음에는 pivotRadius만큼 아래에 위치하게 됩니다.
            // 즉, 현재 플레이어 위치에서 위로 pivotRadius만큼 올라간 곳이 자연스러운 시작 피벗 좌표가 됩니다.
            Vector3 startPivot = targetTransform.position + Vector3.up * pivotRadius;

            // ---------------------------------------------------
            // [핵심 2] 자연스러운 회전 승계 (Lerp 준비)
            // ---------------------------------------------------
            Quaternion startRotation = targetTransform.rotation;
            Quaternion targetLieRotation = Quaternion.Euler(fallbackLieRotation);

            float elapsedTime = 0f;

            while (elapsedTime < fallDuration)
            {
                elapsedTime += Time.deltaTime;
                float time = elapsedTime;

                // 1. 피벗이 아래로 하강
                Vector3 currentPivot = startPivot + Vector3.down * (fallSpeed * time);

                // 2. 좌우 흔들림 계산
                float swing = Mathf.Sin(time * swingSpeed) * swingAngle;
                Quaternion swingRotation = Quaternion.Euler(0f, 0f, swing);
                Vector3 radiusVector = Vector3.down * pivotRadius;

                // 3. 최종 위치 적용 (튀는 현상 없이 현재 위치에서 부드럽게 출발)
                targetTransform.position = currentPivot + swingRotation * radiusVector;

                // 4. 회전 계산 (종이 날림)
                float flutter = Mathf.Sin(time * swingSpeed * 2f) * flutterRotationAmount;
                Quaternion currentPaperRotation = Quaternion.Euler(0f, 0f, swing + flutter);

                // 5. [핵심 3] 서서히 눕는 회전 블렌딩
                // 낙하가 시작된 후 lieDownTransitionDuration 동안은 기존 회전(startRotation)에서 누운 상태로 자연스럽게 변합니다.
                float lieProgress = Mathf.Clamp01(time / lieDownTransitionDuration);
                Quaternion blendedBaseRotation = Quaternion.Slerp(startRotation, targetLieRotation, lieProgress);

                targetTransform.rotation = blendedBaseRotation * currentPaperRotation;

                yield return null;
            }

            EnableInput();

            IsOn = true;
            _moveCoroutine = null;
        }

        private void DisableInput()
        {
            foreach (var script in inputScriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        private void EnableInput()
        {
            foreach (var script in inputScriptsToDisable)
            {
                if (script != null)
                    script.enabled = true;
            }
        }
    }
}
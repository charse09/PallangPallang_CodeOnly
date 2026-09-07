using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [Serializable]
    public struct MoveData
    {
        public Transform transform;
        public float time;
    }

    public class MoveObject : ProcessBase
    {
        [SerializeField] private GameObject objectToMove;
        [SerializeField] private List<MoveData> moveTargetPos;

        public override void Execute()
        {
            // 실행 시작 시 상태 초기화 (부모의 Reset을 못 쓸 때를 대비한 안전장치)
            IsOn = false;

            if (CheckNull())
            {
                IsOn = true; // 에러 시 시퀀스가 멈추지 않도록 처리
                return;
            }

            StartCoroutine(C_Move());
        }

        private bool CheckNull()
        {
            return (objectToMove is null) || (moveTargetPos is null);
        }

        private IEnumerator C_Move()
        {
            Rigidbody rb = objectToMove.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
            }

            for (var i = 0; i < moveTargetPos.Count; i++)
            {
                var target = moveTargetPos[i].transform.position;
                var timer = moveTargetPos[i].time;
                var startPosition = objectToMove.transform.position;
                var elapsedTime = 0f;

                while (elapsedTime < timer)
                {
                    elapsedTime += Time.deltaTime;
                    var t = elapsedTime / timer;
                    objectToMove.transform.position = Vector3.Lerp(startPosition, target, t);
                    yield return null;
                }

                objectToMove.transform.position = target;
            }

            // --- 모든 이동이 끝난 후 처리 ---
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.position = objectToMove.transform.position;
                rb.isKinematic = false;

                var locomotion = objectToMove.GetComponent<PlayerLocomotion>();
                if (locomotion != null)
                {
                    locomotion.enabled = false;
                    locomotion.enabled = true;
                }

                Physics.SyncTransforms();
            }

            IsOn = true;

            yield return null;
        }
    }
}
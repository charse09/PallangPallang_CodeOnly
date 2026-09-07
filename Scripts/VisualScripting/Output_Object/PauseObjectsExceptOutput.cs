using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 오브젝트(및 그 자식들)를 제외한 씬 내의 다른 모든 물체들의 작동(물리, 애니메이션, 특정 스크립트)을 멈추거나 복구하는 노드입니다.
    /// </summary>
    public class PauseObjectsExceptOutput : ProcessBase
    {
        [Tooltip("True면 지정한 예외 오브젝트를 뺀 모든 오브젝트를 멈춥니다. False면 멈췄던 오브젝트들을 다시 작동시킵니다.")]
        [SerializeField] private bool isPause = true;

        [Tooltip("멈추지 않고 계속 작동하게 할 예외 오브젝트들의 목록입니다. 이 오브젝트와 모든 자식 오브젝트들은 멈추지 않습니다.")]
        [SerializeField] private List<GameObject> exceptObjects = new List<GameObject>();

        [Header("Pause Options")]
        [Tooltip("씬 전체의 리지드바디(물리 연산)를 멈출지 여부입니다.")]
        [SerializeField] private bool pausePhysics = true;

        [Tooltip("씬 전체의 애니메이터(애니메이션)를 멈출지 여부입니다.")]
        [SerializeField] private bool pauseAnimators = true;

        [Tooltip("작동을 비활성화할 특정 스크립트 이름들을 적어주세요. (기본값으로 카트 로직을 정지시키기 위해 ArcadeKart 등이 들어갑니다)")]
        [SerializeField] private List<string> specificScriptsToDisable = new List<string>() { "ArcadeKart", "PlayerLocomotion" };

        // 복구를 위한 전역 상태 저장용
        private static Dictionary<Animator, float> savedAnimatorsSpeed = new Dictionary<Animator, float>();
        private static Dictionary<Rigidbody, Vector3> savedVelocity = new Dictionary<Rigidbody, Vector3>();
        private static Dictionary<Rigidbody, Vector3> savedAngularVelocity = new Dictionary<Rigidbody, Vector3>();
        private static Dictionary<Rigidbody, bool> savedKinematic = new Dictionary<Rigidbody, bool>();
        private static Dictionary<MonoBehaviour, bool> savedScriptsState = new Dictionary<MonoBehaviour, bool>();

        public override void Execute()
        {
            IsOn = false;

            if (isPause)
            {
                PauseAllExcept();
#if UNITY_EDITOR
                Debug.Log($"<color=orange>[{gameObject.name}]</color> 예외 대상을 제외한 모든 오브젝트의 작동을 멈췄습니다.");
#endif
            }
            else
            {
                ResumeAll();
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> 멈췄던 오브젝트들을 모두 원래대로 복구했습니다.");
#endif
            }

            IsOn = true;
        }

        private void PauseAllExcept()
        {
            // 예외 오브젝트와 그 자식들을 빠르게 찾기 위한 HashSet 구성
            HashSet<Transform> exceptTransforms = new HashSet<Transform>();
            foreach (var obj in exceptObjects)
            {
                if (obj != null)
                {
                    Transform[] childs = obj.GetComponentsInChildren<Transform>(true);
                    foreach (var c in childs)
                    {
                        exceptTransforms.Add(c);
                    }
                }
            }

            // 1. 애니메이터 멈춤
            if (pauseAnimators)
            {
                Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var anim in allAnimators)
                {
                    if (exceptTransforms.Contains(anim.transform)) continue;

                    if (!savedAnimatorsSpeed.ContainsKey(anim))
                    {
                        savedAnimatorsSpeed[anim] = anim.speed;
                        anim.speed = 0f;
                    }
                }
            }

            // 2. 물리(Rigidbody) 멈춤
            if (pausePhysics)
            {
                Rigidbody[] allRbs = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var rb in allRbs)
                {
                    if (exceptTransforms.Contains(rb.transform)) continue;

                    if (!savedKinematic.ContainsKey(rb))
                    {
                        savedVelocity[rb] = rb.linearVelocity;
                        savedAngularVelocity[rb] = rb.angularVelocity;
                        savedKinematic[rb] = rb.isKinematic;

                        rb.isKinematic = true;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            // 3. 특정 스크립트 비활성화
            if (specificScriptsToDisable.Count > 0)
            {
                MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var script in allScripts)
                {
                    if (script == null || exceptTransforms.Contains(script.transform)) continue;

                    string scriptName = script.GetType().Name;
                    if (specificScriptsToDisable.Contains(scriptName))
                    {
                        if (!savedScriptsState.ContainsKey(script))
                        {
                            savedScriptsState[script] = script.enabled;
                            script.enabled = false;
                        }
                    }
                }
            }
        }

        private void ResumeAll()
        {
            if (pauseAnimators)
            {
                foreach (var kvp in savedAnimatorsSpeed)
                {
                    if (kvp.Key != null) kvp.Key.speed = kvp.Value;
                }
                savedAnimatorsSpeed.Clear();
            }

            if (pausePhysics)
            {
                foreach (var rb in savedKinematic.Keys)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = savedKinematic[rb];
                        if (!rb.isKinematic)
                        {
                            rb.linearVelocity = savedVelocity[rb];
                            rb.angularVelocity = savedAngularVelocity[rb];
                        }
                    }
                }
                savedKinematic.Clear();
                savedVelocity.Clear();
                savedAngularVelocity.Clear();
            }

            if (specificScriptsToDisable.Count > 0)
            {
                foreach (var kvp in savedScriptsState)
                {
                    if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
                }
                savedScriptsState.Clear();
            }
        }
    }
}

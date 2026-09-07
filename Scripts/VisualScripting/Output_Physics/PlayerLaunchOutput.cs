/*

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// CharacterController�� Ŀ���� �̵� ��ũ��Ʈ�� ����ϴ� �÷��̾ ������ ���� ��� �ø��� Output ���
    /// </summary>
    public class PlayerLaunchOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("���� �������� �÷��̾� ������Ʈ�� ��������.")]
        [SerializeField] private GameObject targetPlayer;

        [Header("Launch Settings")]
        [Tooltip("���� ���ư� ��ǥ ���� (��: 10�̸� ���� 10m ���ư�)")]
        [SerializeField] private float launchHeight = 10f;

        [Tooltip("��ǥ ���̱��� �����ϴ� �� �ɸ��� �ð� (��)")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("���ư� ���� ����/���� ������ �����ϴ� Ŀ�� (�⺻��: ���� �������ٰ� ������ �ε巴�� ����)")]
        [SerializeField] private AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("üũ ��, �÷��̾ ���߿� �� �ִ� ���¿��� ��� ���� ��带 �����մϴ�.\nüũ ���� ��, ���߿� ������ ������ �� ���� ��带 �����մϴ�.")]
        [SerializeField] private bool executeNextImmediately = false;

        private bool isLaunching = false;

        public override void Execute()
        {
            if (!isLaunching && targetPlayer != null)
            {
                StartCoroutine(LaunchRoutine());
            }
            else if (targetPlayer == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> �������� Target Player�� �������� �ʾҽ��ϴ�!");
#endif
            }
        }

        private IEnumerator LaunchRoutine()
        {
            isLaunching = true;

            // �÷��̾��� �ٽ� ������Ʈ�� ��������
            CharacterController cc = targetPlayer.GetComponent<CharacterController>();

            // ������ �м� ����� ���� PlayerLocomotion ��ũ��Ʈ�� ã�� �ӽ� ��Ȱ��ȭ (�߷� ���� ����)
            MonoBehaviour locomotionScript = targetPlayer.GetComponent("PlayerLocomotion") as MonoBehaviour;
            if (locomotionScript != null)
            {
                locomotionScript.enabled = false;
            }

            if (executeNextImmediately)
            {
                IsOn = true; // ������� �ʰ� ��� ���� ��� ����
            }

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> �÷��̾� ���� �ξ� ����! (����: {launchHeight}m)");
#endif

            float elapsed = 0f;
            float previousCurveValue = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // AnimationCurve�� ����Ͽ� �ڿ������� ���� ���� ���
                float currentCurveValue = flightCurve.Evaluate(t);

                // ���� ������ ��� �̹� �����ӿ��� �������� �� ���� ���� ���
                float deltaHeight = (currentCurveValue - previousCurveValue) * launchHeight;

                if (cc != null)
                {
                    // CharacterController�� ��ġ�� ���� �ٲٴ� �ͺ��� Move() �޼��带 ��� ���� ���� �ʽ��ϴ�.
                    cc.Move(Vector3.up * deltaHeight);
                }
                else
                {
                    // ���� CharacterController�� ���ٸ� �ܼ� Transform �̵����� ��ü
                    targetPlayer.transform.position += Vector3.up * deltaHeight;
                }

                previousCurveValue = currentCurveValue;
                yield return null;
            }

            // ���� �Ϸ� ��: PlayerLocomotion ��ũ��Ʈ �ٽ� Ȱ��ȭ (���� �߷��� �ٽ� ����Ǿ� �ڿ������� ������)
            if (locomotionScript != null)
            {
                locomotionScript.enabled = true;
            }

            isLaunching = false;

            if (!executeNextImmediately)
            {
                IsOn = true; // ������ �Ϸ�� �� ���� ��� ����
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> �÷��̾� ���� �ξ� �Ϸ�.");
#endif
        }
    }
}




*/

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// CharacterController/Rigidbody 기반 플레이어를 지정 높이만큼 위로 띄워 올리는 Output 모듈
    /// </summary>
    public class PlayerLaunchOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("위로 날려보낼 플레이어 오브젝트를 넣으세요.")]
        [SerializeField] private GameObject targetPlayer;

        [Header("Launch Settings")]
        [Tooltip("위로 날아갈 목표 높이")]
        [SerializeField] private float launchHeight = 10f;

        [Tooltip("목표 높이까지 도달하는 데 걸리는 시간")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("비행 커브")]
        [SerializeField] private AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("도착 후 남길 위쪽 속도. 0이면 바로 낙하, 2~5면 살짝 떠 있음")]
        [SerializeField] private float endUpVelocity = 2f;

        [Tooltip("체크 시, 플레이어가 공중에 떠 있는 상태에서 즉시 다음 노드를 실행합니다.")]
        [SerializeField] private bool executeNextImmediately = false;

        private bool isLaunching = false;

        public override void Execute()
        {
            if (isLaunching) return;

            if (targetPlayer == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> Target Player가 지정되지 않았습니다!");
#endif
                return;
            }

            StartCoroutine(LaunchRoutine());
        }

        private IEnumerator LaunchRoutine()
        {
            isLaunching = true;

            CharacterController cc = targetPlayer.GetComponent<CharacterController>();
            PlayerLocomotion locomotion = targetPlayer.GetComponent<PlayerLocomotion>();

            Rigidbody rb = targetPlayer.GetComponent<Rigidbody>();

            if (locomotion != null)
                locomotion.enabled = false;

            if (rb != null)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }

            if (executeNextImmediately)
                IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 플레이어 공중 부양 시작! 높이: {launchHeight}m");
#endif

            float elapsed = 0f;
            float previousCurveValue = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float currentCurveValue = flightCurve.Evaluate(t);

                float deltaHeight = (currentCurveValue - previousCurveValue) * launchHeight;

                if (cc != null)
                {
                    cc.Move(Vector3.up * deltaHeight);
                }
                else if (rb != null)
                {
                    rb.MovePosition(rb.position + Vector3.up * deltaHeight);
                }
                else
                {
                    targetPlayer.transform.position += Vector3.up * deltaHeight;
                }

                previousCurveValue = currentCurveValue;

                yield return null;
            }

            if (locomotion != null)
            {
                locomotion.enabled = true;
                Vector3 endVelocity = new Vector3(0, endUpVelocity, 0);
                locomotion.ExternalLaunch(endVelocity);
            }
            else if (rb != null)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = endUpVelocity;
                rb.linearVelocity = velocity;
            }

            isLaunching = false;

            if (!executeNextImmediately)
                IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 플레이어 공중 부양 완료.");
#endif
        }
    }
}
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ResetRigidbodyPhysics : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("�������� �ʱ�ȭ(����)�� ������Ʈ")]
        [SerializeField] private GameObject targetObject;

        [Header("Rigidbody State Settings")]
        [Tooltip("üũ�ϸ� ���� �� �Ʒ��� �߷�(Use Gravity) ���������� �����ϴ�.")]
        [SerializeField] private bool applyGravitySetting = false;
        [Tooltip("������ �߷�(Use Gravity) ����")]
        [SerializeField] private bool useGravity = true;

        [Space(5)]
        [Tooltip("üũ�ϸ� ���� �� �Ʒ��� Ű�׸�ƽ(Is Kinematic) ���������� �����ϴ�.")]
        [SerializeField] private bool applyKinematicSetting = false;
        [Tooltip("������ Ű�׸�ƽ(Is Kinematic) ����")]
        [SerializeField] private bool isKinematic = false;

        private void Awake()
        {
            // Ÿ���� ��������� �ڱ� �ڽ��� �Ҵ�
            if (!targetObject) targetObject = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetObject == null)
            {
                IsOn = true;
                return;
            }

            Rigidbody rb = targetObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // 1. �ӵ� �� ���ӵ��� 0���� ����� ������(����) �ʱ�ȭ
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // 2. �߷� ���� ���� (�����ϱ�� üũ�� ��쿡�� ����)
                if (applyGravitySetting)
                {
                    rb.useGravity = useGravity;
                }

                // 3. Ű�׸�ƽ ���� ���� (�����ϱ�� üũ�� ��쿡�� ����)
                if (applyKinematicSetting)
                {
                    rb.isKinematic = isKinematic;
                }
            }
            else
            {
                Debug.LogWarning($"[ResetRigidbodyPhysics] '{targetObject.name}'�� Rigidbody ������Ʈ�� �����ϴ�.");
            }

            // ��� ���� �Ϸ� �� �������� ����� �ѱ�
            IsOn = true;
        }
    }
}
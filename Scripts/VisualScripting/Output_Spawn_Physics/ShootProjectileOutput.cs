using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// ������ ����(����)���� ����ü �������� �����ϰ� �ӵ��� ���Ͽ� �߻��ϴ� Output ���
    /// </summary>
    public class ShootProjectileOutput : ProcessBase
    {
        [Header("Projectile Settings")]
        [Tooltip("�߻��� ����ü �������� ��������. (�ݵ�� Rigidbody ������Ʈ�� �־�� ���ư��ϴ�!)")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("�ѱ� ��ġ �� �߻� ������ ������\n(����θ� �� ��ũ��Ʈ�� ���� ������Ʈ�� ��ġ�� Z�� ���� ������ ����մϴ�)")]
        [SerializeField] private Transform firePoint;

        [Tooltip("�߻�Ǵ� �ӵ� (�ʴ� �̵� �Ÿ�)")]
        [SerializeField] private float fireSpeed = 20f;

        [Tooltip("���̳� ������ ���� �ʰ� ������� ���ư��� ��, �޸� Ȯ���� ���� �ڵ����� �ı��Ǵ� �ð�(��)")]
        [SerializeField] private float autoDestroyTime = 5.0f;

        public override void Execute()
        {
            if (projectilePrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> �߻��� ����ü ������(Projectile Prefab)�� �������� �ʾҽ��ϴ�.");
#endif
                return;
            }

            // �߻� ��ġ�� ���� ���� (firePoint�� ������ �ڱ� �ڽ��� Transform ���)
            Transform origin = firePoint != null ? firePoint : transform;

            // 1. ����ü ��ȯ (�������� ��ġ�� ȸ�������� �״�� �����ؼ� ����)
            GameObject projectile = Instantiate(projectilePrefab, origin.position, origin.rotation);

            // 2. ����ü�� ������ �ӵ�(Velocity) ���ϱ�
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // ���� ������Ʈ�� ����(Z��, forward) �������� ������ �ӵ���ŭ ���� �ݴϴ�.
                rb.linearVelocity = origin.forward * fireSpeed;
            }
            else
            {
                // ���� 2D ������ ����� ��� ��츦 ����� Rigidbody2D ����
                Rigidbody2D rb2d = projectile.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    // 2D������ ���� ����(up)�̳� ������(right)�� �������� ����մϴ�. ���⼱ ������ ����.
                    rb2d.linearVelocity = origin.right * fireSpeed;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"<color=orange>[{gameObject.name}]</color> ����ü �����տ� Rigidbody�� ���� �ӵ��� ������ �� �����ϴ�. ���ڸ����� ������ �˴ϴ�.");
#endif
                }
            }

            // 3. ������ġ: ���� �� �°� ��� ���ư��� 5�� �ڿ� �����Ͽ� �޸� ���� ����
            if (autoDestroyTime > 0f)
            {
                Destroy(projectile, autoDestroyTime);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> ����ü �߻�! (�ӵ�: {fireSpeed})");
#endif

            // 4. �߻縦 �Ϸ������Ƿ� ���� ���� ��ȣ ���� (��: �߻� �� �ѱ� ���� ��ƼŬ ��� ��)
            IsOn = true;
        }
    }
}
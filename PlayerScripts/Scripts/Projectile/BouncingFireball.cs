using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BouncingFireball : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 moveDirection;
    private float currentForwardSpeed;
    private float currentBounceHeight;

    [SerializeField] private bool DestroyOnStart = false;
    [SerializeField] private float DestroyTime = 10.0f;
    


    [Header("Visual Effects")]
    [Tooltip("바닥에 튕길 때 터질 스파크/불티 프리팹 (ParticleSystem)")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Tooltip("바닥에 생길 프리팹이 사라질 시간")]
    [SerializeField] private float particleDestroyTime = 2.0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (DestroyOnStart)
        {
            Destroy(gameObject, DestroyTime);
        }
        
    }

    /// <summary>
    /// Spawner���� �ʱ� �߻� �� ȣ���ϴ� �Լ�
    /// </summary>
    public void Launch(Vector3 direction, float speed, float height)
    {
        moveDirection = direction;
        currentForwardSpeed = speed;
        currentBounceHeight = height;

        ApplyBounceVelocity();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 충돌 지점에 임팩트 이펙트 생성
        if (impactEffectPrefab != null)
        {
            // 부딪힌 정확한 지점과, 바닥이 바라보는 방향(Normal)을 가져옵니다.
            ContactPoint contact = collision.GetContact(0);
            
            // 바닥 방향을 향해 스파크가 튀도록 회전값을 맞춰 생성합니다.
            GameObject particleprefab = Instantiate(impactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(particleprefab, particleDestroyTime);
        }

        // 2. 다시 튀어 오르는 물리 로직 실행
        ApplyBounceVelocity();
    }

    /// <summary>
    /// ���� �ӵ��� ������ ä, ��Ȯ�� ��ǥ ����(H)��ŭ Ƣ��������� Y�� �ӵ��� �����Ͽ� �����
    /// </summary>
    private void ApplyBounceVelocity()
    {
        // v = sqrt(2 * g * h)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float upVelocity = Mathf.Sqrt(2f * gravity * currentBounceHeight);

        // X, Z���� ������ ����� �ӵ� ����, Y���� ���� Ƣ������� �ӵ� ����
        rb.linearVelocity = (moveDirection * currentForwardSpeed) + (Vector3.up * upVelocity);
    }
}
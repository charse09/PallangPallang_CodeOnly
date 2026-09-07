using System.Collections;
using UnityEngine;

public class Stamp : MonoBehaviour
{
    [Header("Speed & Distance")]
    public float chaseSpeed = 5f;
    public float dropSpeed = 20f;
    public float returnSpeed = 5f;
    public float stopDistance = 0.5f;

    [Header("Timing & Raycast")]
    public float waitTime = 1f;
    public LayerMask groundLayer;

    [Header("Roll (구르기) Settings")]
    [Tooltip("굴러가는 이동 속도")]
    public float rollSpeed = 10f;
    [Tooltip("굴러갈 때의 회전 속도 (높을수록 방정맞게 굴러감)")]
    public float rollRotationSpeed = 720f;

    [Header("Current Target")]
    private Transform currentTarget;

    private float startY;
    private float bottomOffset;
    private bool isAttacking = false;
    private bool isRolling = false; // 구르는 중인지 체크하는 변수 추가

    // 외부에서 공격 중인지 확인할 수 있는 프로퍼티
    public bool IsAttacking => isAttacking;

    void Start()
    {
        startY = transform.position.y;
        bottomOffset = transform.localScale.y / 2f;
    }

    void Update()
    {
        // 🚨 타겟이 없거나, 스탬프 찍는 중이거나, 구르는 중이면 기본 추적을 멈춥니다.
        if (currentTarget == null || isAttacking || isRolling) return;

        ChaseTargetXZ();
        CheckDistanceToDrop();
    }

    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
    }

    private void ChaseTargetXZ()
    {
        Vector3 targetPos = new Vector3(currentTarget.position.x, startY, currentTarget.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);
    }

    private void CheckDistanceToDrop()
    {
        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetXZ = new Vector2(currentTarget.position.x, currentTarget.position.z);

        if (Vector2.Distance(currentXZ, targetXZ) <= stopDistance)
        {
            StartCoroutine(StampAttackSequence());
        }
    }

    private IEnumerator StampAttackSequence()
    {
        isAttacking = true;

        yield return new WaitForSeconds(waitTime);

        float targetGroundY = transform.position.y - 50f;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            float pressOverShoot = 0.01f;
            targetGroundY = hit.point.y + bottomOffset - pressOverShoot;
        }

        Vector3 groundPos = new Vector3(transform.position.x, targetGroundY, transform.position.z);

        while (transform.position.y > targetGroundY + 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, groundPos, dropSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = groundPos;

        yield return new WaitForSeconds(0.5f);

        Vector3 startPos = new Vector3(transform.position.x, startY, transform.position.z);
        while (transform.position.y < startY)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, returnSpeed * Time.deltaTime);
            yield return null;
        }

        isAttacking = false;
        currentTarget = null;
    }

    public IEnumerator RollSequence(Transform[] pathNodes)
    {
        if (pathNodes == null || pathNodes.Length == 0) yield break;

        isRolling = true;
        currentTarget = null;

        // 1. 바닥으로 떨어지기
        float targetGroundY = transform.position.y - 50f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            targetGroundY = hit.point.y + bottomOffset;

        Vector3 groundPos = new Vector3(transform.position.x, targetGroundY, transform.position.z);
        while (transform.position.y > targetGroundY + 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, groundPos, dropSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = groundPos;

        foreach (Transform node in pathNodes)
        {
            Vector3 targetPos = new Vector3(node.position.x, targetGroundY, node.position.z);

            // 1) 이동 방향을 먼저 바라보게 설정
            transform.LookAt(targetPos);

            // [추가] Z축으로 90도 돌려 눕히기 전에, 피벗 문제로 파고들지 않도록 Y축을 보정
            // 'bottomOffset'만큼 Y를 높여주면 원통형 장승이 바닥에 딱 붙어서 굴러갑니다.
            transform.position += Vector3.up * bottomOffset;
            transform.Rotate(0, 0, 90f, Space.Self);

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, rollSpeed * Time.deltaTime);

                // 3) 이제 옆으로 누운 상태에서 Z축(또는 회전축)을 기준으로 
                //    Rotate를 주면 원통이 굴러가는 것처럼 보입니다.
                //    'Space.Self'를 사용해야 누운 상태가 유지되며 구릅니다.
                transform.Rotate(0, -rollRotationSpeed * Time.deltaTime, 0, Space.Self);

                yield return null;
            }
            transform.position = targetPos;
        }

        // 3. 복귀
        transform.rotation = Quaternion.identity;

        // [추가] 눕혔던 만큼 다시 Y축을 낮춰서 원래 피벗 상태로 복구
        transform.position -= Vector3.up * bottomOffset;
        yield return new WaitForSeconds(0.2f);

        Vector3 startPos = new Vector3(transform.position.x, startY, transform.position.z);
        while (transform.position.y < startY)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, returnSpeed * Time.deltaTime);
            yield return null;
        }
        isRolling = false;
    }
}
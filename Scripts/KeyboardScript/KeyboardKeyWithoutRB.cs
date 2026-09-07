using UnityEngine;
using System.Collections;

public class KeyboardKeyWithoutRB : MonoBehaviour
{
    [Header("Movement Settings")]
    public float pressDepth = 0.1f;
    public float pressSpeed = 15f;
    public float releaseSpeed = 5f;
    public float stayDelay = 0.1f;

    [Header("Press Direction")]
    // 키보드 키 자체의 로컬 방향 기준 (Vector3.down = 아래쪽, Vector3.back = Z축 뒤쪽)
    public Vector3 pressDirection = Vector3.down;

    [Header("Detection Settings (Rigidbody 미사용)")]
    public float detectionRadius = 0.5f;
    public Vector3 detectionOffset = new Vector3(0f, 0.2f, 0f);
    public string playerTag = "Player";

    private Vector3 unpressedPos;
    private Vector3 pressedPos;
    private bool isPressed = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        // 1. 기준이 될 초기 로컬 위치 저장
        unpressedPos = transform.localPosition;

        // 2. 오브젝트의 회전을 고려하여 눌렸을 때의 로컬 위치를 '미리' 계산 (순간이동 방지)
        pressedPos = unpressedPos + (pressDirection.normalized * pressDepth);
    }

    void Update()
    {
        CheckPlayerOverlap();
    }

    private void CheckPlayerOverlap()
    {
        // 현재 위치(월드 좌표) 기준으로 감지 구체 생성
        Vector3 checkCenter = transform.TransformPoint(detectionOffset);
        Collider[] hitColliders = Physics.OverlapSphere(checkCenter, detectionRadius);

        bool playerFound = false;

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag(playerTag))
            {
                playerFound = true;
                break;
            }
        }

        if (playerFound && !isPressed)
        {
            isPressed = true;
            StartMove(pressedPos, pressSpeed);
        }
        else if (!playerFound && isPressed)
        {
            isPressed = false;
            StartMove(unpressedPos, releaseSpeed, stayDelay);
        }
    }

    private void StartMove(Vector3 target, float speed, float delay = 0f)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(target, speed, delay));
    }

    private IEnumerator MoveRoutine(Vector3 target, float speed, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        while (Vector3.Distance(transform.localPosition, target) > 0.001f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                target,
                speed * Time.deltaTime
            );
            yield return null;
        }
        transform.localPosition = target;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 checkCenter = transform.TransformPoint(detectionOffset);
        Gizmos.DrawWireSphere(checkCenter, detectionRadius);
    }
}


using UnityEngine;
using System.Collections;

public class TerminalKey : MonoBehaviour
{
    [Header("Movement Settings")]
    public float pressDepth = 0.1f;
    public float pressSpeed = 15f;    // ������ �� �ӵ�
    public float releaseSpeed = 5f;   // �ö�� �� �ӵ� (�� õõ��)
    public float stayDelay = 0.1f;    // ���� ���¿��� ������ �ּ� �ð�

    private Vector3 unpressedPos;
    private Vector3 pressedPos;
    private int occupantCount = 0;
    private Coroutine moveCoroutine;

    void Start()
    {
        unpressedPos = transform.localPosition;
        pressedPos = unpressedPos + (Vector3.down * pressDepth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        //if (collision.gameObject.CompareTag("Human"))
        {
            occupantCount++;
            StartMove(pressedPos, pressSpeed);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        //if (collision.gameObject.CompareTag("Human"))
        {
            occupantCount--;
            if (occupantCount <= 0)
            {
                occupantCount = 0;
                // �ణ�� ������ �� ���� ��ġ�� ����
                StartMove(unpressedPos, releaseSpeed, stayDelay);
            }
        }
    }

    private void StartMove(Vector3 target, float speed, float delay = 0f)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(target, speed, delay));
    }

    private IEnumerator MoveRoutine(Vector3 target, float speed, float delay)
    {
        // �����̰� �����Ǿ� �ִٸ� ���
        if (delay > 0) yield return new WaitForSeconds(delay);

        while (Vector3.Distance(transform.localPosition, target) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localPosition = target;
    }
}

/*
using UnityEngine;
using System.Collections;

public class KeyboardKey : MonoBehaviour
{
    [Header("Movement Settings")]
    public float pressDepth = 0.1f;
    public float pressSpeed = 15f;
    public float releaseSpeed = 5f;
    public float stayDelay = 0.1f;

    private Vector3 unpressedPos;
    private Vector3 pressedPos;
    private int occupantCount = 0;
    private Coroutine moveCoroutine;

    void Start()
    {
        unpressedPos = transform.localPosition;
        pressedPos = unpressedPos + (Vector3.left * pressDepth);
    }

    // [����] OnCollisionEnter -> OnTriggerEnter ����
    private void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("Player") || other.CompareTag("Human"))
        if (other.CompareTag("Human"))
        {
            occupantCount++;
            StartMove(pressedPos, pressSpeed);
            Debug.Log($"{gameObject.name}��(��) {other.name}�� ���� ����!");
        }
    }

    // [����] OnCollisionExit -> OnTriggerExit ����
    private void OnTriggerExit(Collider other)
    {
        //if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        if (other.CompareTag("Human"))
        {
            occupantCount--;
            if (occupantCount <= 0)
            {
                occupantCount = 0;
                StartMove(unpressedPos, releaseSpeed, stayDelay);
            }
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
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localPosition = target;
    }
}
*/
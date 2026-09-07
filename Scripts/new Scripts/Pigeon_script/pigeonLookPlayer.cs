

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class pigeonLookPlayer : MonoBehaviour
{
    [Header("Pigeon HardCoading")]
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject neck;

    [Header("Environment")]
    [SerializeField] private float jumpGravity = -30.0f;

    [Header("Targeting")]
    [SerializeField] private List<GameObject> targetlist = new List<GameObject>();
    [SerializeField] private GameObject currentTarget;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float stoppingDistance = 1.2f;

    [Header("Route Detect")]
    [SerializeField] private float routeArriveDistance = 2.0f;

    [Header("Snack Detect")]
    [SerializeField] private float snackDetectDistance = 1.5f;

    [Header("Snack Priority")]
    [SerializeField] private float snackPriorityDistance = 8.0f;

    [Header("Snack Time")]
    [SerializeField] private float snackEatTime = 2f;
    [SerializeField] private bool EatingSnack = false;

    [Header("Game Over")]
    [SerializeField] private float restartDelay = 1.0f;

    private CharacterController characterController;
    private float verticalVelocity = 0.0f;
    private Coroutine currentCoroutine = null;
    private Vector3 moveVector = Vector3.zero;
    private bool isGameOver = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (isGameOver) return;

        if (!EatingSnack)
        {
            SearchTarget();
            MoveToTarget();
            ApplyMovement();
        }

        BeingGravity();
    }

    // 🔥 Snack 범위 기반 우선순위
    void SearchTarget()
    {
        if (targetlist.Count == 0)
        {
            currentTarget = null;
            return;
        }

        GameObject closestSnack = null;
        GameObject closestNormal = null;

        float minSnackDist = Mathf.Infinity;
        float minNormalDist = Mathf.Infinity;

        float snackPriorityDistanceSqr = snackPriorityDistance * snackPriorityDistance;

        Vector3 pos = transform.position;

        foreach (GameObject target in targetlist)
        {
            if (target == null) continue;

            float dist = (target.transform.position - pos).sqrMagnitude;

            if (target.CompareTag("Snack"))
            {
                // 범위 안에 있을 때만 Snack 고려
                if (dist <= snackPriorityDistanceSqr && dist < minSnackDist)
                {
                    minSnackDist = dist;
                    closestSnack = target;
                }
            }
            else if (target.CompareTag("route") || target.CompareTag("Player"))
            {
                if (dist < minNormalDist)
                {
                    minNormalDist = dist;
                    closestNormal = target;
                }
            }
        }

        currentTarget = closestSnack != null ? closestSnack : closestNormal;
    }

    void MoveToTarget()
    {
        if (currentTarget == null)
        {
            moveVector = Vector3.zero;
            return;
        }

        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;

        // 🔥 Snack 먹기
        if (currentTarget.CompareTag("Snack") && distance <= snackDetectDistance)
        {
            GameObject snack = currentTarget;

            targetlist.Remove(snack);
            currentTarget = null;
            moveVector = Vector3.zero;

            Destroy(snack);
            StartEating();
            return;
        }

        // 🔥 route 도착
        if (currentTarget.CompareTag("route") && distance <= routeArriveDistance)
        {
            GameObject route = currentTarget;

            targetlist.Remove(route);
            currentTarget = null;
            moveVector = Vector3.zero;

            Destroy(route);
            return;
        }

        // 🔥 Player 도착 → 게임오버
        if (currentTarget.CompareTag("Player") && distance <= stoppingDistance)
        {
            GameObject player = currentTarget;

            targetlist.Remove(player);
            currentTarget = null;
            moveVector = Vector3.zero;
            isGameOver = true;

            Destroy(player);
            StartCoroutine(RestartSceneAfterDelay());
            return;
        }

        // 회전
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // 이동
        if (distance > stoppingDistance)
        {
            moveVector = direction.normalized * moveSpeed;
        }
        else
        {
            moveVector = Vector3.zero;
        }
    }

    public void Jump(float force)
    {
        verticalVelocity = force;
    }

    void BeingGravity()
    {
        verticalVelocity += jumpGravity * Time.deltaTime;
        verticalVelocity = Mathf.Clamp(verticalVelocity, -50f, 50f);
    }

    void ApplyMovement()
    {
        Vector3 velocity = moveVector + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    void StartEating()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(EatSnack_Pigeon());
    }

    IEnumerator EatSnack_Pigeon()
    {
        EatingSnack = true;
        moveVector = Vector3.zero;

        Vector3 startBodyRot = body.transform.localEulerAngles;
        Vector3 startNeckRot = neck.transform.localEulerAngles;

        float duration = snackEatTime * 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            float z = Mathf.Lerp(0, -60f, percent);
            body.transform.localEulerAngles =
                new Vector3(startBodyRot.x, startBodyRot.y, startBodyRot.z + z);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            float z = Mathf.Lerp(0, -30f, percent);
            neck.transform.localEulerAngles =
                new Vector3(startNeckRot.x, startNeckRot.y, startNeckRot.z + z);

            yield return null;
        }

        yield return new WaitForSeconds(snackEatTime * 0.1f);

        elapsed = 0f;
        float returnDuration = snackEatTime * 0.4f;

        Vector3 currentBody = body.transform.localEulerAngles;
        Vector3 currentNeck = neck.transform.localEulerAngles;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / returnDuration;

            body.transform.localEulerAngles = Vector3.Lerp(currentBody, startBodyRot, percent);
            neck.transform.localEulerAngles = Vector3.Lerp(currentNeck, startNeckRot, percent);

            yield return null;
        }

        body.transform.localEulerAngles = startBodyRot;
        neck.transform.localEulerAngles = startNeckRot;

        EatingSnack = false;
        currentCoroutine = null;
    }

    private IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}


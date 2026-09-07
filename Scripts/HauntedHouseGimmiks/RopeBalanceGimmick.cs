using UnityEngine;
using System.Collections;

public class RopeBalanceGimmick : MonoBehaviour
{
    [Header("=== Essential ===")]
    public Transform playerTransform;
    public PlayerLocomotion playerLocomotion;
    public Collider zoneCollider;

    [Header("=== Base Settings ===")]
    [SerializeField] private float ropeWalkSpeed = 1.5f;

    [Header("=== Balance Settings ===")]
    [SerializeField] private float balanceDrainSpeed = 0.25f;
    [SerializeField] private float balanceRecoverSpeed = 0.9f;
    [SerializeField] private float fallThreshold = 0.95f;

    [Header("=== Random Tilt Settings ===")]
    [SerializeField] private float tiltIntervalMin = 2.0f;
    [SerializeField] private float tiltIntervalMax = 4.0f;
    [SerializeField] private float tiltImpulseMin = 0.2f;
    [SerializeField] private float tiltImpulseMax = 0.4f;

    [Header("=== Visual / Fall Settings ===")]
    [SerializeField] private float maxTiltAngle = 30.0f;
    [SerializeField] private float tiltLerpSpeed = 5.0f;
    [SerializeField] private float fallSideOffset = 1.5f;
    [SerializeField] private Transform fallRespawnPoint;

    [Header("=== UI ===")]
    [SerializeField] private RopeBalanceUI balanceUI;

    private PlayerLocomotion playerMove;
    private Transform playerVisual;

    private bool isActive = false;
    private bool isFalling = false;
    private float balance = 0f;
    private float tiltTimer = 0f;
    private float currentTiltDir = 1f;

    private bool wasInZone = false;

    private void Start()
    {
        if (balanceUI != null) balanceUI.Hide();
    }

    private void Update()
    {
        CheckZone();

        if (isActive && !isFalling)
        {
            HandleBalanceInput();
            HandleRandomTilt();
            UpdateVisualTilt();

            if (balanceUI != null)
                balanceUI.UpdateBalance(balance);

            if (Mathf.Abs(balance) >= fallThreshold)
            {
                TriggerFall();
            }
        }
    }

            private void CheckZone()
    {
        if (zoneCollider == null || playerTransform == null || isFalling) return;

        Collider playerCol = playerTransform.GetComponent<Collider>();
        bool isInZone = false;
        if (playerCol != null) {
            isInZone = zoneCollider.bounds.Intersects(playerCol.bounds);
        } else {
            isInZone = zoneCollider.bounds.Contains(playerTransform.position + Vector3.up * 0.5f);
        }

        if (isInZone && !wasInZone && !isActive && !isFalling)
        {
            TryEnterRopeMode();
        }
        else if (!isInZone && wasInZone && isActive)
        {
            ExitRopeMode();
        }

        wasInZone = isInZone;
    }

    private void TryEnterRopeMode()
    {
        PlayerLocomotion pmc = playerLocomotion;

        if (pmc == null) pmc = playerTransform.GetComponent<PlayerLocomotion>();
        if (pmc == null) pmc = playerTransform.GetComponentInParent<PlayerLocomotion>();
        if (pmc == null) pmc = playerTransform.GetComponentInChildren<PlayerLocomotion>();
        if (pmc == null) pmc = FindObjectOfType<PlayerLocomotion>();

        if (pmc == null)
        {
            Debug.LogWarning("[RopeGimmick] PlayerLocomotion NOT FOUND!");
            return;
        }

        playerMove = pmc;

        Transform visual = pmc.transform.Find("Character");
        if (visual == null && pmc.transform.childCount > 0) visual = pmc.transform.GetChild(0);
        playerVisual = visual != null ? visual : pmc.transform;

        balance = 0f; pendingTilt = 0f;
        ScheduleNextTilt();
        isActive = true;

        playerMove.EnterRopeWalk(ropeWalkSpeed, this);
        if (balanceUI != null) balanceUI.Show();
    }

    private void ExitRopeMode()
    {
        isActive = false;
        ResetVisualTilt();

        if (playerMove != null) playerMove.ExitRopeWalk();
        if (balanceUI != null) balanceUI.Hide();
    }

    private void HandleBalanceInput()
    {
        // Use InputManager if available, otherwise fallback to raw input
        float moveX = 0f;
        // In case InputManager is fully customized, fallback for now:
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX = 1f;

                if (Mathf.Abs(moveX) > 0.1f)
        {
            balance += moveX * balanceRecoverSpeed * Time.deltaTime;
        }
        else
        {
            balance += currentTiltDir * balanceDrainSpeed * Time.deltaTime;
        }

        balance = Mathf.Clamp(balance, -1f, 1f);
    }

        private float pendingTilt = 0f;

    private void HandleRandomTilt()
    {
        tiltTimer -= Time.deltaTime;
        if (tiltTimer <= 0f)
        {
            ApplyTiltImpulse();
            ScheduleNextTilt();
        }

        if (Mathf.Abs(pendingTilt) > 0.001f)
        {
            float shift = Mathf.Sign(pendingTilt) * Mathf.Min(Mathf.Abs(pendingTilt), 1.5f * Time.deltaTime);
            balance += shift;
            pendingTilt -= shift;
            balance = Mathf.Clamp(balance, -1f, 1f);
        }
    }

    private void ApplyTiltImpulse()
    {
        float dir = (Random.value > 0.5f) ? 1f : -1f;
        float impulse = Random.Range(tiltImpulseMin, tiltImpulseMax);
        currentTiltDir = dir;
        pendingTilt += dir * impulse;
    }

    private void ScheduleNextTilt()
    {
        tiltTimer = Random.Range(tiltIntervalMin, tiltIntervalMax);
    }

            private float currentVisualTilt = 0f;

    private void UpdateVisualTilt()
    {
        if (playerMove != null)
        {
            float targetZ = -balance * maxTiltAngle;
            currentVisualTilt = Mathf.LerpAngle(currentVisualTilt, targetZ, Time.deltaTime * tiltLerpSpeed);
            playerMove.SetRopeTilt(currentVisualTilt);
        }
    }

    private void ResetVisualTilt()
    {
        if (playerMove != null)
        {
            currentVisualTilt = 0f;
            playerMove.SetRopeTilt(0f);
        }
    }

    private void TriggerFall()
    {
        if (isFalling) return;
        isFalling = true;
        isActive = false;
        wasInZone = false;

        StartCoroutine(FallRoutine());
    }

        private IEnumerator FallRoutine()
    {
        if (playerMove != null)
        {
            float fallAngle = balance > 0 ? maxTiltAngle : -maxTiltAngle;
            playerMove.SetRopeTilt(fallAngle);
        }

        if (playerMove != null) playerMove.ExitRopeWalk();

        yield return new WaitForSeconds(0.3f);

        if (playerTransform != null)
        {
            PlayerLocomotion cc = playerTransform.GetComponent<PlayerLocomotion>();
            if (cc == null) cc = playerTransform.GetComponentInParent<PlayerLocomotion>();

            GameObject playerGo = cc != null ? cc.gameObject : playerTransform.gameObject;

            if (cc != null) cc.enabled = false;

            if (fallRespawnPoint != null)
                playerGo.transform.position = fallRespawnPoint.position;
            else
                playerGo.transform.position += transform.right * (balance > 0 ? 1f : -1f) * fallSideOffset;

            if (cc != null) cc.enabled = true;
        }

        ResetVisualTilt();
        balance = 0f; pendingTilt = 0f;
        isFalling = false;
        if (balanceUI != null) balanceUI.Hide();
    }
}







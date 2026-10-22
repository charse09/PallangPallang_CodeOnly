using System.Collections;
using UnityEngine;

public class EnemyVisuals : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform characterVisual;
    [SerializeField] private GameObject sphereObject;

    [Header("Visual Controllers")]
    [SerializeField] private PlayerColorController colorController;

    [Header("Paper Sway Settings")]
    [SerializeField] private float swayAmount = 15f;
    [SerializeField] private float swaySpeed = 5f;
    [SerializeField] private Vector3 swayPivot = new Vector3(0f, 0f, 1f);
    [SerializeField] private float visualTiltAngle = 30.0f;

    private float swayTimer = 0f;
    private float swayWeight = 0f;
    private Quaternion baseMoveRotation = Quaternion.identity;
    private Coroutine rotationCoroutine;

    // 현재 몬스터의 상태를 캐싱하여 매 프레임 업데이트에 사용
    private bool isPaper;
    private bool isSlim;
    private bool isSlimCompletely;

    private void Awake()
    {
        baseMoveRotation = transform.rotation;
    }

    // --- State Updaters ---
    // WindRoll 파라미터 제거됨
    public void UpdateStates(bool paper, bool slim, bool completelySlim)
    {
        isPaper = paper;
        isSlim = slim;
        isSlimCompletely = completelySlim;
    }

    // --- Color ---
    public void ChangeColorByState(PlayerConditionState state)
    {
        if (colorController != null) colorController.ChangeColorByState(state);
    }

    // --- Rotation & Sway (Called from Locomotion LateUpdate) ---
    public void HandleVisualsLateUpdate(float deltaTime, bool isGrounded, bool isRidingRail)
    {
        if (isRidingRail) return;

        if (!isGrounded && isPaper) swayWeight = Mathf.MoveTowards(swayWeight, 1f, deltaTime * 3f);
        else swayWeight = Mathf.MoveTowards(swayWeight, 0f, deltaTime * 8f);

        if (isPaper)
        {
            if (sphereObject && !sphereObject.activeSelf) sphereObject.SetActive(true);
            swayTimer += deltaTime * swaySpeed;
        }
        else
        {
            if (sphereObject && sphereObject.activeSelf) sphereObject.SetActive(false);
        }

        if (rotationCoroutine == null) ApplyFinalRotation();
    }

    public void UpdateBaseRotation(Vector3 inputMoveDir, bool isInsideSlideZone, Vector3 fixedSlideDirection, bool isSliding, Vector3 slideDirection)
    {
        if (isInsideSlideZone) { baseMoveRotation = Quaternion.LookRotation(fixedSlideDirection); return; }
        if (isSliding) { baseMoveRotation = Quaternion.LookRotation(slideDirection); return; }

        if (inputMoveDir.sqrMagnitude > 0.01f)
            baseMoveRotation = Quaternion.LookRotation(inputMoveDir);
    }

    public void AddBaseRotationY(float deltaY)
    {
        baseMoveRotation = Quaternion.Euler(0, deltaY, 0) * baseMoveRotation;
    }

    public void SetBaseRotationForward(Vector3 lookForward)
    {
        lookForward.y = 0;
        if (lookForward.sqrMagnitude > 0.01f)
        {
            baseMoveRotation = Quaternion.LookRotation(lookForward.normalized);
            transform.rotation = baseMoveRotation;
        }
    }

    // --- Resets & Transitions ---
    public void ResetRotationAndVisuals()
    {
        StopRotationTransition();
        baseMoveRotation = Quaternion.identity;
        transform.rotation = Quaternion.identity;
        swayWeight = 0f;

        if (characterVisual != null)
        {
            characterVisual.localPosition = Vector3.zero;
            characterVisual.localRotation = Quaternion.identity;
        }
        if (sphereObject) sphereObject.SetActive(false);
    }

    public void StopRotationTransition()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    public void TriggerStateChangeTransition()
    {
        StopRotationTransition();
        rotationCoroutine = StartCoroutine(RotationRoutine());
    }

    private IEnumerator RotationRoutine()
    {
        float t = 0;
        Quaternion startRot = transform.rotation;
        Quaternion startVisualRot = (characterVisual != null) ? characterVisual.localRotation : Quaternion.identity;

        while (t < 1.0f)
        {
            t += Time.deltaTime * 8f;
            Quaternion stateOffset = isPaper ? Quaternion.Euler(-90f, 0, 0) : (isSlim ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity);
            transform.rotation = Quaternion.Slerp(startRot, baseMoveRotation * stateOffset, t);

            if (characterVisual != null)
            {
                float currentTilt = isSlimCompletely ? 0f : visualTiltAngle;
                float swayY = isPaper ? Mathf.Sin(swayTimer) * swayAmount * swayWeight : 0f;
                Quaternion targetVisualRot = isPaper ? Quaternion.Euler(currentTilt, swayY, 0) :
                                            (isSlim ? Quaternion.Euler(0, -currentTilt, 0) : Quaternion.identity);
                characterVisual.localRotation = Quaternion.Slerp(startVisualRot, targetVisualRot, t);
            }
            yield return null;
        }
        rotationCoroutine = null;
    }

    private void ApplyFinalRotation()
    {
        Quaternion stateOffset = Quaternion.identity;
        if (isPaper) stateOffset = Quaternion.Euler(-90f, 0, 0);
        else if (isSlim) stateOffset = Quaternion.Euler(0, 90f, 0);
        transform.rotation = baseMoveRotation * stateOffset;

        if (characterVisual != null)
        {
            float currentTilt = isSlimCompletely ? 0.0f : visualTiltAngle;
            if (isPaper)
            {
                float angle = Mathf.Sin(swayTimer) * swayAmount * swayWeight;
                Quaternion swayRot = Quaternion.Euler(currentTilt, angle, 0f);
                Vector3 swayPos = swayPivot + (swayRot * -swayPivot);

                characterVisual.localPosition = Vector3.Lerp(Vector3.zero, swayPos, swayWeight);
                characterVisual.localRotation = Quaternion.Slerp(Quaternion.Euler(10f, 0f, 0f), swayRot, swayWeight);
            }
            else if (isSlim)
            {
                characterVisual.localPosition = Vector3.zero;
                characterVisual.localRotation = Quaternion.Euler(0, -currentTilt, 0);
            }
            else
            {
                characterVisual.localPosition = Vector3.zero;
                characterVisual.localRotation = Quaternion.identity;
            }
        }
    }
}
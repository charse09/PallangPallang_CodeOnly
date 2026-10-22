using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum PatternPhase
{
    Phase1_BothHands,   // 양손 패턴
    Phase2_SingleHand,  // 한 손 패턴
    Phase3_BodyOnly,    // 몸통 패턴
    Phase4_Desperation  // 발악 패턴
}

public class PatternBossStateMachine : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Animator bossAnim;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private BossFaceController faceController;

    [Header("Phase Settings")]
    [SerializeField] private PatternPhase currentPhase = PatternPhase.Phase1_BothHands;

    [Header("Rig Settings")]
    [SerializeField] private Rig leftArmRig;
    [SerializeField] private Rig rightArmRig;
    [SerializeField] private Transform leftArmIKTarget;
    [SerializeField] private Transform rightArmIKTarget;
    [SerializeField] private float rigBlendSpeed = 5f;

    [Header("Combat Settings")]
    [SerializeField] private float baseAttackCooltime = 4.0f;
    [SerializeField] private float bossRotationSpeed = 5.0f;

    private float currentCooltimeTimer = 0f;
    private bool isAttacking = false;
    private bool isAiming = false;

    void Start()
    {
        currentCooltimeTimer = baseAttackCooltime;
        if (leftArmRig != null) leftArmRig.weight = 0f;
        if (rightArmRig != null) rightArmRig.weight = 0f;
    }

    void Update()
    {
        if (targetTransform == null) return;

        // 조준 중이 아니거나 공격 중이 아닐 때는 플레이어를 바라봄
        if (!isAttacking || isAiming)
        {
            Vector3 lookDirection = targetTransform.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * bossRotationSpeed);
            }
        }

        if (isAttacking) return;

        // 쿨타임 계산
        if (currentCooltimeTimer > 0)
        {
            currentCooltimeTimer -= Time.deltaTime;
            return;
        }

        // 쿨타임이 다 돌면 현재 페이즈에 맞는 패턴 실행
        DecideCurrentState();
    }

    // =========================================================
    // 상태 판단 (유한 상태 머신)
    // =========================================================
    private void DecideCurrentState()
    {
        switch (currentPhase)
        {
            case PatternPhase.Phase1_BothHands:
                ExecutePhase1Patterns();
                break;

            case PatternPhase.Phase2_SingleHand:
                ExecutePhase2Patterns();
                break;

            case PatternPhase.Phase3_BodyOnly:
                ExecutePhase3Patterns();
                break;

            case PatternPhase.Phase4_Desperation:
                ExecutePhase4Patterns();
                break;
        }
    }

    // =========================================================
    // 페이즈별 패턴 실행기 (랜덤 혹은 순차적 패턴 호출)
    // =========================================================
    private void ExecutePhase1Patterns()
    {
        // 1페이즈: 양손을 사용하는 스킬들 중 하나를 랜덤 실행
        int rand = Random.Range(0, 2);
        if (rand == 0) StartCoroutine(AttackRoutine(BothHandsClapAttack()));
        else StartCoroutine(AttackRoutine(BothHandsSmashAttack()));
    }

    private void ExecutePhase2Patterns()
    {
        // 2페이즈: 남은 한 손(오른손)만 사용하는 스킬
        StartCoroutine(AttackRoutine(SingleHandSweepAttack()));
    }

    private void ExecutePhase3Patterns()
    {
        // 3페이즈: 팔을 쓰지 않고 몸통이나 머리를 이용하는 스킬
        StartCoroutine(AttackRoutine(BodyTackleAttack()));
    }

    private void ExecutePhase4Patterns()
    {
        // 4페이즈: 쿨타임이 매우 짧은 광역/발악 패턴
        StartCoroutine(AttackRoutine(DesperationAOEAttack()));
    }

    // =========================================================
    // 스킬 코루틴 래퍼 (공격 중 플래그 및 쿨타임 초기화)
    // =========================================================
    private IEnumerator AttackRoutine(IEnumerator attackMethod)
    {
        isAttacking = true;

        yield return StartCoroutine(attackMethod);

        // 페이즈 4(발악)일 때는 쿨타임을 대폭 감소
        currentCooltimeTimer = (currentPhase == PatternPhase.Phase4_Desperation) ? 1.0f : baseAttackCooltime;
        isAttacking = false;
    }

    // =========================================================
    // 실제 스킬 구현부 (임시 예시)
    // =========================================================

    // [1페이즈] 양손 박수 공격
    private IEnumerator BothHandsClapAttack()
    {
        if (faceController != null) faceController.ChangeFace(BossFaceState.Mad);
        if (bossAnim != null) bossAnim.SetTrigger("BothHandsClap");

        // 양팔 IK 활성화
        Coroutine leftRoutine = StartCoroutine(LerpRigWeight(leftArmRig, 1f));
        Coroutine rightRoutine = StartCoroutine(LerpRigWeight(rightArmRig, 1f));
        yield return leftRoutine;
        yield return rightRoutine;

        // 애니메이션 대기 및 데미지 판정 로직
        yield return new WaitForSeconds(2.0f);

        // IK 초기화
        yield return StartCoroutine(LerpRigWeight(leftArmRig, 0f));
        yield return StartCoroutine(LerpRigWeight(rightArmRig, 0f));

        if (faceController != null) faceController.ChangeFace(BossFaceState.Excited);
    }

    // [1페이즈] 양손 내려찍기
    private IEnumerator BothHandsSmashAttack()
    {
        Debug.Log("양손 내려찍기!");
        yield return new WaitForSeconds(1.5f);
    }

    // [2페이즈] 한 손 휩쓸기
    private IEnumerator SingleHandSweepAttack()
    {
        Debug.Log("한 손 휩쓸기!");
        yield return StartCoroutine(LerpRigWeight(rightArmRig, 1f));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(LerpRigWeight(rightArmRig, 0f));
    }

    // [3페이즈] 몸통 박치기
    private IEnumerator BodyTackleAttack()
    {
        Debug.Log("몸통 박치기!");
        yield return new WaitForSeconds(2.0f);
    }

    // [4페이즈] 발악 광역기
    private IEnumerator DesperationAOEAttack()
    {
        if (faceController != null) faceController.ChangeFace(BossFaceState.Destroied);
        Debug.Log("맵 전체 무작위 폭발!");
        yield return new WaitForSeconds(1.0f);
    }

    // =========================================================
    // 페이즈 전환 함수 (외부에서 호출)
    // =========================================================
    public void ChangePhase(PatternPhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"보스 페이즈 전환: {newPhase}");

        // 페이즈가 넘어갈 때 망가지는 연출이나 특정 팔의 IK를 강제 종료하는 로직 추가
        if (newPhase == PatternPhase.Phase2_SingleHand)
        {
            // 예: 왼팔이 부서짐 -> 왼팔 IK 강제 0
            StartCoroutine(LerpRigWeight(leftArmRig, 0f));
        }
        else if (newPhase == PatternPhase.Phase3_BodyOnly)
        {
            // 예: 오른팔마저 부서짐
            StartCoroutine(LerpRigWeight(rightArmRig, 0f));
        }

        if (bossAnim != null) bossAnim.SetTrigger("PhaseChange");
    }

    // =========================================================
    // 유틸리티 함수
    // =========================================================
    private IEnumerator LerpRigWeight(Rig targetRig, float targetWeight)
    {
        if (targetRig == null) yield break;
        while (Mathf.Abs(targetRig.weight - targetWeight) > 0.01f)
        {
            targetRig.weight = Mathf.Lerp(targetRig.weight, targetWeight, Time.deltaTime * rigBlendSpeed);
            yield return null;
        }
        targetRig.weight = targetWeight;
    }
}
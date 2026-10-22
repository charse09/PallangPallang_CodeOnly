using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class StaffAttackIK : MonoBehaviour
{
    [Header("IK Settings")]
    public Rig attackRig;           // Rig_Attack 컴포넌트를 드래그해서 넣기
    public Transform ikTarget;      // 손이 향할 목표 빈 오브젝트 (실제로 이동될 녀석)

    [Header("Target & Combat")]
    public Transform paperDoll;     // 도망치는 종이인형(플레이어) 할당
    public float maxReach = 1.5f;   // 직원의 팔이 닿는 최대 거리 (기즈모를 보며 조절해)
    public float attackSpeed = 0.2f; // 손이 타겟으로 내려꽂히는(Weight 0 -> 1) 시간

    private Coroutine attackCoroutine;

    void Start()
    {
        // 평소에는 IK가 간섭하지 않도록 0으로 꺼두기
        attackRig.weight = 0f;
    }

    // 공격 애니메이션이 재생될 때 이 함수를 호출해! 
    // (보통 Animation Event로 특정 프레임에 호출하는 것이 제일 좋아)
    public void StartAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AttackSequence());
    }
    

    private IEnumerator AttackSequence()
    {
        // 1. 공격할 목표 지점(Target) 설정 (사거리 제한 로직)
        Vector3 direction = paperDoll.position - transform.position;
        direction.y = 0; // 높이는 무시 (앞뒤좌우 거리만 계산)
        float distance = direction.magnitude;

        // 종이인형이 팔 길이(maxReach)보다 멀리 있다면, 타겟을 최대 사거리까지만 밀어냄
        if (distance > maxReach)
        {
            ikTarget.position = transform.position + (direction.normalized * maxReach);
        }
        else
        {
            // 닿는 거리라면 종이인형의 위치가 곧 타겟 위치
            ikTarget.position = paperDoll.position;
        }

        // (선택) 손이 항상 바닥을 쳐야 한다면, 여기서 ikTarget의 Y축 높이를 강제로 바닥 높이(예: 0)로 고정할 수 있어.
        // ikTarget.position = new Vector3(ikTarget.position.x, 0f, ikTarget.position.z);

        // 2. 내리치기! Weight를 0에서 1로 빠르게 올리기
        float timer = 0f;
        while (timer < attackSpeed)
        {
            timer += Time.deltaTime;
            // 스무스하게 0 -> 1로 변환
            attackRig.weight = Mathf.Lerp(0f, 1f, timer / attackSpeed);
            yield return null; // 다음 프레임까지 대기
        }
        attackRig.weight = 1f; // 확실하게 1로 고정

        // (선택) 손이 땅에 닿은 채로 아주 잠깐 멈칫하게 해서 타격감을 주고 싶다면 주석 해제
        yield return new WaitForSeconds(0.1f);

        // 3. 팔 거둬들이기! Weight를 다시 1에서 0으로 내리기
        timer = 0f;
        while (timer < attackSpeed)
        {
            timer += Time.deltaTime;
            attackRig.weight = Mathf.Lerp(1f, 0f, timer / attackSpeed);
            yield return null;
        }
        attackRig.weight = 0f; // 확실하게 0으로 끄기
    }
}
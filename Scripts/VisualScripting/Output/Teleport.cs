using System.Collections; // IEnumerator 사용을 위해 필요
using _Project.Scripts.VisualScripting;
using UnityEngine;

public class Teleport : ProcessBase
{
    [SerializeField] Trigger trigger;
    [SerializeField] float timeDelay = 0.0f;

    // WaitForSeconds 객체를 캐싱하여 가비지 생성을 줄임 (선택 사항이나 좋은 습관)
    private WaitForSeconds _wait;

    private void Start()
    {
        if (timeDelay > 0)
        {
            _wait = new WaitForSeconds(timeDelay);
        }
    }

    public override void Execute()
    {
        // 딜레이가 있다면 코루틴 실행, 없다면 즉시 실행
        if (timeDelay > 0.0f)
        {
            StartCoroutine(TeleportRoutine());
        }
        else
        {
            PerformTeleport();
        }
    }

    // 실제 텔레포트 로직을 분리
    private void PerformTeleport()
    {
        // 실행 시점에도 타겟이 유효한지 반드시 확인해야 함
        if (trigger != null && trigger.GetTarget())
        {
            trigger.GetTarget().transform.position = this.transform.position;
            Debug.Log($"{trigger.GetTarget().name} Teleport!");
        }
        else
        {
            Debug.LogWarning("Trigger Target Error or Target is missing!");
        }
    }

    // 지연 처리를 위한 코루틴
    private IEnumerator TeleportRoutine()
    {
        // 지정된 시간만큼 대기
        yield return _wait;

        // 대기 후 실제 로직 수행
        PerformTeleport();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero, 1);
    }
}
using UnityEngine;

// 오브젝트에 반드시 Collider가 있고 'Is Trigger'가 체크되어 있어야 합니다.
[RequireComponent(typeof(Collider))]
public class StampTrigger : MonoBehaviour
{
    [Header("Connection")]
    [Tooltip("명령을 내릴 스탬프 본체를 연결하세요.")]
    public Stamp targetStamp;

    [Header("Settings")]
    [Tooltip("감지할 대상의 태그입니다.")]
    public string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트의 태그가 타겟 태그와 일치하는지 확인
        if (other.CompareTag(targetTag))
        {
            if (targetStamp != null)
            {
                targetStamp.SetTarget(other.transform); // 본체에 타겟 전달
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 타겟이 범위 밖으로 나가면 추적 중지
        if (other.CompareTag(targetTag))
        {
            if (targetStamp != null)
            {
                targetStamp.SetTarget(null); // 본체의 타겟을 비움
            }
        }
    }
}
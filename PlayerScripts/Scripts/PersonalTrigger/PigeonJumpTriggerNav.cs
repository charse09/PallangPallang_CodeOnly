using UnityEngine;

public class PigeonJumpTriggerNav : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15.0f;

    private void OnTriggerEnter(Collider other)
    {
        // 비둘기 오브젝트의 Tag가 "Pigeon"으로 설정되어 있어야 합니다.
        if (other.CompareTag("Pigeon"))
        {
            ApplyJump(other.gameObject);
        }
    }

    private void ApplyJump(GameObject pigeonObj)
    {
        // 이제 NavMesh 기반인 PigeonNavAI만 찾습니다.
        PigeonNavAI navAI = pigeonObj.GetComponent<PigeonNavAI>();

        if (navAI != null && navAI.enabled)
        {
            navAI.Jump(jumpForce);
            Debug.Log("PigeonNavAI를 통해 비둘기 점프!");
        }
        else
        {
            Debug.LogWarning("비둘기에서 PigeonNavAI 스크립트를 찾을 수 없습니다.");
        }
    }
}
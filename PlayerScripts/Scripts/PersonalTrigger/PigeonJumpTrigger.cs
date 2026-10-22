

/*
using UnityEngine;

public class PigeonJumpTrigger : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 25.0f; // 인스펙터에서 점프 높이 조절

    // CharacterController는 물리 충돌(OnCollisionEnter)보다 
    // 트리거(OnTriggerEnter)나 OnControllerColliderHit에 더 잘 반응합니다.
    // 만약 점프대가 'Is Trigger' 체크되어 있다면 아래 함수를 사용하세요.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pigeon"))
        {
            ApplyJump(other.gameObject);
        }
    }


    private void ApplyJump(GameObject pigeonObj)
    {
        // 비둘기에 붙어있는 locomotion 스크립트를 가져옵니다.
        pigeonLocomotion locomotion = pigeonObj.GetComponent<pigeonLocomotion>();

        if (locomotion != null)
        {
            // locomotion 스크립트의 public 메서드를 호출하여 점프시킵니다.
            locomotion.Jump(jumpForce);
            Debug.Log("비둘기 점프!");
        }
    }
}

*/

using UnityEngine;

public class PigeonJumpTrigger : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 25.0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pigeon"))
        {
            ApplyJump(other.gameObject);
        }
    }

    private void ApplyJump(GameObject pigeonObj)
    {
        pigeonLocomotion locomotion = pigeonObj.GetComponent<pigeonLocomotion>();
        pigeonLookPlayer lookPlayer = pigeonObj.GetComponent<pigeonLookPlayer>();

        // 현재 켜져 있는 lookPlayer 우선
        if (lookPlayer != null && lookPlayer.enabled)
        {
            lookPlayer.Jump(jumpForce);
            Debug.Log("켜져 있는 pigeonLookPlayer 점프!");
            return;
        }

        // 현재 켜져 있는 locomotion 실행
        if (locomotion != null && locomotion.enabled)
        {
            locomotion.Jump(jumpForce);
            Debug.Log("켜져 있는 pigeonLocomotion 점프!");
            return;
        }

        Debug.LogWarning("켜져 있는 비둘기 점프 스크립트가 없습니다.");
    }
}
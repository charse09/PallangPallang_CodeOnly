using UnityEngine;

public class CharacterPush : MonoBehaviour
{
    public float pushPower = 2.0f; // 밀어내는 힘의 세기

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // 1. 리지드바디가 없거나 Kinematic 상태면 무시
        if (body == null || body.isKinematic)
        {
            return;
        }

        // 2. 캐릭터 발 아래에 있는 오브젝트는 밀지 않음 (선택 사항)
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }

        // 3. 밀 방향 계산 (캐릭터가 움직이는 방향)
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // 4. 힘 가하기 (질량에 따라 밀리도록 Impulse 방식 권장)
        body.AddForceAtPosition(pushDir * pushPower, hit.point, ForceMode.Impulse);
    }
}
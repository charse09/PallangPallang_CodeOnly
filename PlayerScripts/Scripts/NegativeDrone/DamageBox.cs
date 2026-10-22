using UnityEngine;

public class DamageBox : MonoBehaviour
{
    [Tooltip("이 공격이 입힐 데미지 수치입니다.")]
    [SerializeField] private float damage = 10f;

    // Is Trigger가 체크된 콜라이더에 무언가 닿았을 때 자동으로 실행되는 유니티 함수
    private void OnTriggerEnter(Collider other)
    {
        // 닿은 오브젝트의 레이어가 'Player'인지 확인
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("플레이어가 데미지 박스에 닿았습니다! 피격 처리!");

            // 실제 게임에서는 플레이어의 체력 스크립트를 가져와 데미지를 줍니다.
            /*
            PlayerHealth playerStats = other.GetComponent<PlayerHealth>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }
            */
        }
    }
}
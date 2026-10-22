using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그나 레이어를 확인하여 GameManager의 리스폰 호출
        if (other.CompareTag("Player"))
        {
            GameManager.instance.StartRespawnSequence();
        }
    }
}
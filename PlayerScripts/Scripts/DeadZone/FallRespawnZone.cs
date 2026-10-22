using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FallRespawnZone : MonoBehaviour
{
    [Header("Respawn Target")]
    [SerializeField] private Transform targetCheckpoint;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그 확인 후 GameManager에 지정된 체크포인트를 넘겨주며 리스폰 호출
        if (other.CompareTag("Player"))
        {
            GameManager.instance.StartRespawnSequence(targetCheckpoint);
        }
    }
}
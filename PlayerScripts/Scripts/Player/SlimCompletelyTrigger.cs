using UnityEngine;

public class SlimCompletelyTrigger : MonoBehaviour
{
    [SerializeField] private PlayerLocomotion locomotion;

    // 벽과 접촉 중일 때 계속 true 유지
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            locomotion.SlimCompletely = true;
        }
    }

    // 벽에서 떨어지면 false
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            locomotion.SlimCompletely = false;
        }
    }
}
using UnityEngine;

public class AttackZoneWatcher : MonoBehaviour
{
    [Header("직원 본체 연결")]
    public StaffTriggerAttackTest staffScript;

    private Transform GetPlayerRoot(Collider other)
    {
        if (other.CompareTag(staffScript.playerTag))
        {
            return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
        }
        if (other.transform.root.CompareTag(staffScript.playerTag))
        {
            return other.transform.root;
        }
        return null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (staffScript == null) return;

        Transform playerTransform = GetPlayerRoot(other);
        if (playerTransform != null)
        {
            staffScript.SetTarget(playerTransform);
            staffScript.TryAttack(playerTransform);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (staffScript == null) return;

        Transform playerTransform = GetPlayerRoot(other);
        if (playerTransform != null)
        {
            staffScript.SetTarget(playerTransform);
            staffScript.TryAttack(playerTransform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (staffScript == null) return;

        Transform playerTransform = GetPlayerRoot(other);
        if (playerTransform != null)
        {
            staffScript.ClearTarget(playerTransform);
        }
    }
}
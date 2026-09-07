using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeeRescueTrigger : MonoBehaviour
{
    [Header("=== Essential Setup ===")]
    [Tooltip("Yenbeol (Bee) Transform in the Scene")]
    public Transform yenbeolTransform;

    [Tooltip("Final destination Transform for the player")]
    public Transform destination;

    [Tooltip("Optional waypoints before reaching destination")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("=== Flight Settings ===")]
    [Tooltip("Flight speed (m/s)")]
    public float flySpeed = 8.0f;

    [Tooltip("Offset of the player relative to Yenbeol (e.g. Y = -1.2 means player hangs under bee)")]
    public Vector3 playerOffset = new Vector3(0f, -1.2f, 0f);

    private bool isRescuing = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isRescuing) return;

        PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
        if (player == null) player = other.GetComponent<PlayerLocomotion>();

        if (player != null)
        {
            if (destination == null)
            {
                Debug.LogError($"[BeeRescueTrigger] '{gameObject.name}'의 Destination(목표 위치)이 Inspector에 할당되지 않았습니다!");
                return;
            }
            if (yenbeolTransform == null)
            {
                Debug.LogError($"[BeeRescueTrigger] '{gameObject.name}'의 YenbeolTransform(벌)이 Inspector에 할당되지 않았습니다!");
                return;
            }

            StartCoroutine(RescueRoutine(player));
        }
    }

    private IEnumerator RescueRoutine(PlayerLocomotion player)
    {
        isRescuing = true;

        Debug.Log($"[BeeRescueTrigger] 구출 시작! 벌: {yenbeolTransform.name}, 목표: {destination.name}");

        // 1. Completely reset velocities and lock physics/inputs
        player.ResetAllVelocities();
        player.SetRespawnMode(true);

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        // Disable Rigidbody on Yenbeol if present so physics doesn't interfere
        Rigidbody yenbeolRb = yenbeolTransform.GetComponent<Rigidbody>();
        if (yenbeolRb != null)
        {
            yenbeolRb.linearVelocity = Vector3.zero;
            yenbeolRb.angularVelocity = Vector3.zero;
            yenbeolRb.isKinematic = true;
        }

        // Disable any other scripts on Yenbeol that might interfere with movement
        var existingBeeScripts = yenbeolTransform.GetComponents<MonoBehaviour>();
        foreach (var s in existingBeeScripts)
        {
            if (s != null && s != this && !(s is Animator))
            {
                s.enabled = false;
            }
        }

        // 2. Position Yenbeol at player's current position (accounting for offset)
        yenbeolTransform.gameObject.SetActive(true);
        yenbeolTransform.position = player.transform.position - playerOffset;

        // 3. Build path transforms
        List<Transform> pathTransforms = new List<Transform>();
        foreach (var wp in waypoints)
        {
            if (wp != null) pathTransforms.Add(wp);
        }
        if (destination != null)
        {
            pathTransforms.Add(destination);
        }

        // 4. Move along path
        for (int i = 0; i < pathTransforms.Count; i++)
        {
            Transform targetTransform = pathTransforms[i];

            while (yenbeolTransform != null && targetTransform != null && Vector3.Distance(yenbeolTransform.position, targetTransform.position) > 0.1f)
            {
                Vector3 targetPos = targetTransform.position;
                Vector3 moveDir = (targetPos - yenbeolTransform.position).normalized;

                yenbeolTransform.position = Vector3.MoveTowards(yenbeolTransform.position, targetPos, flySpeed * Time.deltaTime);

                // Update player position to hang from Yenbeol
                player.transform.position = yenbeolTransform.position + playerOffset;

                // Smooth rotation of Yenbeol towards travel direction
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    yenbeolTransform.rotation = Quaternion.Slerp(yenbeolTransform.rotation, targetRot, Time.deltaTime * 10f);
                }

                yield return null;
            }

            if (targetTransform != null)
            {
                yenbeolTransform.position = targetTransform.position;
                player.transform.position = yenbeolTransform.position + playerOffset;
            }
        }

        // 5. Final positioning
        if (destination != null)
        {
            player.transform.position = destination.position;
        }

        yield return new WaitForSeconds(0.1f);

        // 6. Restore player control and locomotion
        player.ResetAllVelocities();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = false;
        }

        player.SetRespawnMode(false);

        Debug.Log($"[BeeRescueTrigger] 구출 완료! 플레이어를 {destination.name} 위치에 하차시켰습니다.");

        isRescuing = false;
    }
}

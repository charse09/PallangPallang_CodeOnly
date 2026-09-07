using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosestObjectFinder : MonoBehaviour
{
    [Header("탐색 설정 (Scan Settings)")]
    public float scanRadius = 5f;
    public LayerMask targetLayer;
    public string targetTag = "Interactable";
    public bool filterByTag = true;

    [Header("최적화 설정 (Optimization)")]
    public float scanInterval = 0.2f;
    public int maxDetectables = 20;

    [Header("상호작용 설정 (Interaction)")]
    [Tooltip("상호작용을 실행할 키를 설정합니다.")]
    public KeyCode interactKey = KeyCode.E; // 상호작용 키 추가

    [Header("탐색 결과 (Results)")]
    public List<GameObject> detectedObjects = new List<GameObject>();
    public GameObject closestObject;

    private Collider[] hitColliders;

    private void Start()
    {
        hitColliders = new Collider[maxDetectables];
        StartCoroutine(ContinuousScanRoutine());
    }

    // --- [새로 추가된 부분] ---
    private void Update()
    {
        // 1. 지정된 상호작용 키(E)를 눌렀고
        // 2. 현재 내 주변에 가장 가까운 오브젝트(closestObject)가 존재한다면
        if (Input.GetKeyDown(interactKey) && closestObject != null)
        {
            // 가장 가까운 오브젝트에서 IInteractable 규칙을 가진 스크립트만 쏙 빼옵니다.
            // (그것이 SmoothRotator든, 다른 스크립트든 상관없이 IInteractable만 있으면 됩니다!)
            IInteractable interactableScript = closestObject.GetComponent<IInteractable>();

            if (interactableScript != null)
            {
                // 찾은 스크립트의 작동 함수를 실행합니다.
                interactableScript.Interact();
            }
            else
            {
                Debug.LogWarning($"[{closestObject.name}] 오브젝트에 상호작용 기능(IInteractable)이 없습니다.");
            }
        }
    }
    // ---------------------------

    private IEnumerator ContinuousScanRoutine()
    {
        while (true)
        {
            FindClosestTargetOptimized();
            yield return new WaitForSeconds(scanInterval);
        }
    }

    private void FindClosestTargetOptimized()
    {
        detectedObjects.Clear();
        closestObject = null;

        int hits = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, hitColliders, targetLayer);

        for (int i = 0; i < hits; i++)
        {
            Collider col = hitColliders[i];

            if (!filterByTag || col.CompareTag(targetTag))
            {
                detectedObjects.Add(col.gameObject);
            }
        }

        if (detectedObjects.Count > 0)
        {
            float shortestDistance = Mathf.Infinity;

            foreach (GameObject obj in detectedObjects)
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestObject = obj;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, scanRadius);
    }
}
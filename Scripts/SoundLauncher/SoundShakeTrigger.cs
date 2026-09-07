using System.Collections;
using System.Collections.Generic; // List를 사용하기 위해 추가
using UnityEngine;
using Unity.Cinemachine;

public class SpeakerCameraShake : MonoBehaviour
{
    [Header("Standard Camera Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.3f;

    [SerializeField] private bool isPlayerInZone = false;
    private bool isShaking = false;

    // [핵심 변경] 단순 숫자가 아니라, 들어온 콜라이더들을 직접 명단으로 관리합니다.
    // [수정] 인스펙터에서 명단을 직접 볼 수 있도록 SerializeField 추가!
    [SerializeField] private List<Collider> playerCollidersInZone = new List<Collider>();
    private Camera mainCam;
    private CinemachineBrain cmBrain;
    private CinemachineImpulseSource impulseSource;

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) cmBrain = mainCam.GetComponent<CinemachineBrain>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        // [자동 청소 로직] 
        // 명단에 있는 콜라이더가 파괴되었거나(null), 꺼졌거나(!enabled), 
        // 오브젝트 자체가 비활성화되었다면(!activeInHierarchy) 명단에서 즉시 삭제합니다.
        // 이 한 줄이 OnTriggerExit가 무시되는 버그를 완벽하게 막아줍니다.
        playerCollidersInZone.RemoveAll(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);

        // 명단에 콜라이더가 하나라도 남아있으면 플레이어가 구역 안에 있는 것으로 판정!
        isPlayerInZone = playerCollidersInZone.Count > 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 명단에 없다면 추가
            if (!playerCollidersInZone.Contains(other))
            {
                playerCollidersInZone.Add(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 명단에 있다면 제거
            if (playerCollidersInZone.Contains(other))
            {
                playerCollidersInZone.Remove(other);
            }
        }
    }

    public void TriggerShake()
    {
        if (isPlayerInZone && !isShaking)
        {
            if (cmBrain != null && cmBrain.isActiveAndEnabled && impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }
            else if (mainCam != null)
            {
                StartCoroutine(ShakeCoroutine());
            }
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        Transform camTransform = mainCam.transform;
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * shakeMagnitude;
            camTransform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
        isShaking = false;
    }
}
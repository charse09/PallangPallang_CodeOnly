using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public class TelekinesisOutput : ProcessBase
    {
        [Header("Telekinesis Settings")]
        [SerializeField] private string targetTag = "Grabbable";
        [SerializeField] private float maxGrabDistance = 20f;
        [SerializeField] private float pullForce = 15f;
        [SerializeField] private float grabRadius = 0.5f;

        [Header("Crosshair Settings")]
        [Tooltip("조준점의 위치 오프셋 (X는 좌우, Y는 상하)")]
        [SerializeField] private Vector2 crosshairOffset = new Vector2(0f, 150f);

        [Header("Control Settings")]
        [SerializeField] private float verticalMoveSensitivity = 5f;
        [SerializeField] private float scrollSpeed = 2f;

        private Camera mainCam;
        private GameObject crosshairUI;
        private RectTransform crosshairRect; // ★ 실제 렌더링된 조준점의 위치를 추적하기 위한 변수
        private Rigidbody grabbedObject;

        private float originalDrag;
        private float originalAngularDrag;
        private bool originalUseGravity;
        private float currentHoldDistance;
        private float currentVerticalOffset;

        public override void Execute()
        {
            mainCam = Camera.main;
            if (mainCam == null) return;

            StartCoroutine(TelekinesisRoutine());
        }

        private IEnumerator TelekinesisRoutine()
        {
            SetupCrosshair();

            while (true)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryGrabObject();
                }
                else if (Input.GetMouseButton(0) && grabbedObject != null)
                {
                    HoldAndMoveObject();
                }
                else if (Input.GetMouseButtonUp(0) && grabbedObject != null)
                {
                    ReleaseObject();
                }

                yield return null;
            }
        }

        /*
        private void TryGrabObject()
        {
            if (crosshairRect == null) return;

            // ★ 완벽한 동기화: Overlay Canvas에서 UI 오브젝트의 position은 실제 화면 픽셀 좌표와 완벽히 일치합니다.
            // 더 이상 복잡한 계산을 하지 않고 빨간 점 UI의 위치를 그대로 가져옵니다.
            Vector3 screenPoint = crosshairRect.position;
            Ray ray = mainCam.ScreenPointToRay(screenPoint);

            // SphereCast로 관대한 조준 반경 적용
            if (Physics.SphereCast(ray, grabRadius, out RaycastHit hit, maxGrabDistance))
            {
                if (hit.collider.CompareTag(targetTag))
                {
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        grabbedObject = rb;
                        currentHoldDistance = Vector3.Distance(mainCam.transform.position, grabbedObject.position);
                        currentVerticalOffset = 0f;

                        originalUseGravity = grabbedObject.useGravity;
                        originalDrag = grabbedObject.linearDamping;
                        originalAngularDrag = grabbedObject.angularDamping;

                        grabbedObject.useGravity = false;
                        grabbedObject.linearDamping = 10f;
                        grabbedObject.angularDamping = 10f;

                        crosshairUI.GetComponentInChildren<Image>().color = Color.green;
                    }
                }
            }
        }

        */

        private void TryGrabObject()
        {
            if (crosshairRect == null) return;

            // 1. UI의 위치를 실제 화면 픽셀 좌표로 정확하게 변환 (CanvasScaler 호환)
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, crosshairRect.position);
            Ray ray = mainCam.ScreenPointToRay(screenPoint);

            // 2. SphereCastAll을 사용하여 경로 상의 모든 물체를 감지
            // (플레이어 자신이나 투명한 콜라이더, 바닥 등에 막혀 무시되는 현상 방지)
            RaycastHit[] hits = Physics.SphereCastAll(ray, grabRadius, maxGrabDistance);

            // 3. 카메라와 가까운 순서대로 정렬
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // 4. 감지된 물체들을 가까운 순서대로 확인하며 태그 검사
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag(targetTag))
                {
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        grabbedObject = rb;
                        currentHoldDistance = Vector3.Distance(mainCam.transform.position, grabbedObject.position);
                        currentVerticalOffset = 0f;

                        originalUseGravity = grabbedObject.useGravity;
                        originalDrag = grabbedObject.linearDamping; // Unity 구버전의 경우 drag
                        originalAngularDrag = grabbedObject.angularDamping; // Unity 구버전의 경우 angularDrag

                        grabbedObject.useGravity = false;
                        grabbedObject.linearDamping = 10f;
                        grabbedObject.angularDamping = 10f;

                        crosshairUI.GetComponentInChildren<Image>().color = Color.green;

                        // 타겟을 찾았으므로 루프 종료
                        break; 
                    }
                }
            }
        }

        private void HoldAndMoveObject()
        {
            currentHoldDistance += Input.mouseScrollDelta.y * scrollSpeed;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, 2f, maxGrabDistance);

            float mouseY = Input.GetAxis("Mouse Y");
            currentVerticalOffset += mouseY * verticalMoveSensitivity * Time.deltaTime * 50f;

            Vector3 targetPosition = mainCam.transform.position + (mainCam.transform.forward * currentHoldDistance) + (Vector3.up * currentVerticalOffset);

            Vector3 directionToTarget = targetPosition - grabbedObject.position;
            grabbedObject.linearVelocity = directionToTarget * pullForce;
        }

        private void ReleaseObject()
        {
            grabbedObject.useGravity = originalUseGravity;
            grabbedObject.linearDamping = originalDrag;
            grabbedObject.angularDamping = originalAngularDrag;
            grabbedObject = null;
            crosshairUI.GetComponentInChildren<Image>().color = Color.red;
        }

        private void SetupCrosshair()
        {
            if (crosshairUI != null)
            {
                crosshairRect.anchoredPosition = crosshairOffset;
                return;
            }

            crosshairUI = new GameObject("TelekinesisCrosshair");
            Canvas canvas = crosshairUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            crosshairUI.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(crosshairUI.transform, false);
            Image img = dot.AddComponent<Image>();
            img.color = Color.red;

            crosshairRect = img.rectTransform;
            crosshairRect.sizeDelta = new Vector2(8f, 8f);
            crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.anchoredPosition = crosshairOffset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null || grabbedObject != null) return;

            Gizmos.color = Color.red;

            Vector3 screenPoint;

            // 게임 실행 중일 때는 실제 빨간 점의 위치를 사용하고, 에디터 화면일 때는 임시로 계산하여 보여줌
            if (Application.isPlaying && crosshairRect != null)
            {
                screenPoint = crosshairRect.position;
            }
            else
            {
                screenPoint = new Vector3(Screen.width * 0.5f + crosshairOffset.x, Screen.height * 0.5f + crosshairOffset.y, 0f);
            }

            Ray ray = mainCam.ScreenPointToRay(screenPoint);

            Gizmos.DrawWireSphere(mainCam.transform.position, grabRadius);
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * maxGrabDistance);
            Gizmos.DrawWireSphere(ray.origin + ray.direction * maxGrabDistance, grabRadius);
        }
#endif
    }
}
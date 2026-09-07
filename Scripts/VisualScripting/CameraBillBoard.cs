using UnityEngine;

namespace _Project.Scripts.Utils
{
    /// <summary>
    /// 오브젝트에 부착하면 카메라(기본 Main Camera)를 항상 정면으로 바라보도록 회전시키는 빌보드 컴포넌트입니다.
    /// </summary>
    public class CameraBillboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            [Tooltip("카메라 렌즈 평면의 회전값과 완전히 동기화합니다 (2D 스프라이트, UI, 말풍선 등에 가장 자연스러움).")]
            MatchCameraRotation,

            [Tooltip("카메라의 3D 렌즈 위치를 직접 조준하여 바라봅니다.")]
            LookAtCameraPosition
        }

        [Header("Billboard Settings")]
        [Tooltip("빌보드 회전 방식 선택")]
        [SerializeField] private BillboardMode billboardMode = BillboardMode.MatchCameraRotation;

        [Tooltip("체크 시 위아래로 기울지 않고 Y축(좌우)으로만 꼿꼿하게 회전합니다.")]
        [SerializeField] private bool lockYAxis = false;

        [Tooltip("메쉬나 스프라이트의 앞뒤가 반대로 보일 경우 체크 (180도 반전)")]
        [SerializeField] private bool reverseFacing = false;

        [Header("Target Camera (선택사항)")]
        [Tooltip("특정 카메라를 바라보게 하려면 할당 (비워두면 Camera.main을 자동 탐색)")]
        [SerializeField] private Camera targetCamera;

        private Transform _camTransform;

        private void Awake()
        {
            CacheCamera();
        }

        private void OnEnable()
        {
            CacheCamera();
        }

        private void CacheCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                _camTransform = targetCamera.transform;
            }
        }

        private void LateUpdate()
        {
            if (_camTransform == null)
            {
                CacheCamera();
                if (_camTransform == null) return;
            }

            if (billboardMode == BillboardMode.MatchCameraRotation)
            {
                // 카메라 평면과 평행하게 회전 동기화
                if (lockYAxis)
                {
                    transform.rotation = Quaternion.Euler(0f, _camTransform.rotation.eulerAngles.y, 0f);
                }
                else
                {
                    transform.rotation = _camTransform.rotation;
                }
            }
            else // LookAtCameraPosition
            {
                // 카메라 렌즈 위치를 직접 바라봄
                Vector3 dirToCam = _camTransform.position - transform.position;

                if (lockYAxis)
                {
                    dirToCam.y = 0f;
                }

                if (dirToCam.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(dirToCam.normalized);
                }
            }

            // 앞뒤 반전 보정
            if (reverseFacing)
            {
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }
}
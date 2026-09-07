using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [ExecuteInEditMode] // 에디터에서도 보이게 설정
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(LineRenderer))]
    public class BoxColliderVisualizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color lineColor = Color.green;
        [SerializeField] private float lineWidth = 0.05f;

        private BoxCollider _boxCollider;
        private LineRenderer _lineRenderer;

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            // 인스펙터에서 값 변경 시 즉시 반영
            Initialize();
            DrawBox();
        }

        private void Update()
        {
            // 런타임에 콜라이더 크기가 변할 수 있으므로 매 프레임 갱신 (최적화 필요시 조건문 추가)
            DrawBox();
        }

        private void Initialize()
        {
            _boxCollider = GetComponent<BoxCollider>();
            _lineRenderer = GetComponent<LineRenderer>();

            // 라인 렌더러 기본 설정
            _lineRenderer.useWorldSpace = false; // 로컬 좌표 사용
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;

            // 머티리얼이 없으면 기본 스프라이트 머티리얼 할당 (보라색 방지)
            if (_lineRenderer.sharedMaterial == null)
            {
                _lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            // 색상 설정
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
        }

        private void DrawBox()
        {
            if (_boxCollider == null || _lineRenderer == null) return;

            // 라인 두께 실시간 반영
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;

            Vector3 center = _boxCollider.center;
            Vector3 size = _boxCollider.size;
            Vector3 half = size * 0.5f;

            // 박스의 8개 꼭짓점 계산 (로컬 좌표)
            Vector3 p0 = center + new Vector3(-half.x, -half.y, -half.z); // L-B-Back
            Vector3 p1 = center + new Vector3(half.x, -half.y, -half.z);  // R-B-Back
            Vector3 p2 = center + new Vector3(half.x, -half.y, half.z);   // R-B-Fwd
            Vector3 p3 = center + new Vector3(-half.x, -half.y, half.z);  // L-B-Fwd

            Vector3 p4 = center + new Vector3(-half.x, half.y, -half.z);  // L-T-Back
            Vector3 p5 = center + new Vector3(half.x, half.y, -half.z);   // R-T-Back
            Vector3 p6 = center + new Vector3(half.x, half.y, half.z);    // R-T-Fwd
            Vector3 p7 = center + new Vector3(-half.x, half.y, half.z);   // L-T-Fwd

            // 한 붓 그리기로 모든 모서리를 연결하는 경로
            // (LineRenderer는 끊어진 선을 지원하지 않으므로 경로를 겹쳐서 이동함)
            Vector3[] positions = new Vector3[]
            {
                // 밑면 루프 (4 -> 1)
                p0, p1, p2, p3, p0, 
                
                // 윗면으로 이동 (1)
                p4, 
                
                // 윗면 루프 (4 -> 1)
                p5, p6, p7, p4,
                
                // 나머지 수직 기둥들을 그리기 위해 되돌아가며 연결
                p5, p1, // 기둥 2
                p1, p2, // 이동 (밑면 선 겹침)
                p2, p6, // 기둥 3
                p6, p7, // 이동 (윗면 선 겹침)
                p7, p3  // 기둥 4
            };

            _lineRenderer.positionCount = positions.Length;
            _lineRenderer.SetPositions(positions);
        }
    }
}
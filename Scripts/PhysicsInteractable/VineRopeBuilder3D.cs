using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.PhysicsInteractable
{
    public class VineLinkNode3D : MonoBehaviour
    {
        [HideInInspector] public float normalizedPosition;
        [HideInInspector] public VineRopeBuilder3D parentBuilder;
    }

    /// <summary>
    /// 하위 오브젝트들을 3D 전방향 구면 진자(Spherical Pendulum)로 구성하여
    /// A/D(좌우)뿐만 아니라 W/S(앞뒤) 및 대각선 방향 외력까지 모두 자연스럽게 흔들리는 3D 덩굴 로프 컴포넌트입니다.
    /// </summary>
    public class VineRopeBuilder3D : MonoBehaviour
    {
        [Header("=== 3D Pendulum Physics Settings ===")]
        [Tooltip("진자의 감쇄(마찰) 계수. 높을수록 빨리 멈춥니다 (0이면 무한 왕복)")]
        [Range(0f, 2f)]
        public float damping = 0.3f;

        [Tooltip("진자에 작용할 중력 가속도 크기")]
        public float gravity = 9.81f;

        [Tooltip("최대 스윙 각도 제한 (도 단위)")]
        [Range(10f, 90f)]
        public float maxSwingAngle = 60f;

        [Tooltip("체크 시 특정 축으로 제한하지 않고 360도 전방향(A/D 좌우 + W/S 앞뒤) 자유 스윙을 허용합니다.")]
        public bool allow3DSphericalSwing = true;

        [Tooltip("allow3DSphericalSwing이 false일 때만 사용되는 1차원 제한 축")]
        public Vector3 fallbackSwingAxis = Vector3.right;

        [Header("=== Visual Curve Settings ===")]
        [Tooltip("사슬의 휘어짐 정도. 높을수록 끝부분이 부드럽게 많이 휘어집니다.")]
        [Range(0f, 1f)]
        public float flexibility = 0.4f;

        [Tooltip("관성 지연 효과. 덩굴이 움직일 때 중간 부분이 뒤늦게 따라오는 느낌을 줍니다.")]
        [Range(0f, 0.5f)]
        public float inertiaLag = 0.15f;

        [Header("=== Interaction Settings ===")]
        [Tooltip("플레이어가 잡을 수 있는 고리 트리거 반경 (Trigger Radius)")]
        public float grabTriggerRadius = 1.5f;

        [Tooltip("플레이어가 오직 끝 고리(또는 지정된 고리)에만 잡히도록 설정합니까?")]
        public bool onlyAttachToEndRing = true;

        [Tooltip("잡을 수 있는 끝 고리 오브젝트 이름 (예: Object002 (1)).\n비워두면 Y좌표가 가장 낮은 맨 아래 고리에 자동으로 생성됩니다.")]
        public string targetEndObjectName = "Object002 (1)";

        // 3D 진자 물리 상태
        [HideInInspector] public Vector3 endPosition;
        [HideInInspector] public Vector3 endVelocity;

        public float RopeLength => ropeLength;
        public Vector3 PivotWorldPosition => pivotWorldPos;

        private class SegmentData
        {
            public Transform segTransform;
            public float normalizedPosition;
            public float distFromPivot;
            public Vector3 restBoundsCenter;
            public Vector3 restVectorFromPivot;
            public Vector3 originalLocalPosition;
            public Quaternion originalLocalRotation;
        }

        private readonly List<SegmentData> segments = new List<SegmentData>();
        private Vector3 pivotWorldPos;
        private float ropeLength;

        // 시각적 지연 스무딩
        private Vector3 smoothedEndPosition;
        private Vector3 endPositionVelocity;

        private void Awake()
        {
            BuildVineRope();
        }

        [ContextMenu("Build Vine Rope Now (Editor)")]
        public void BuildVineRope()
        {
            segments.Clear();

            if (transform.childCount == 0) return;

            List<Transform> childTransforms = new List<Transform>();
            List<Vector3> childBoundsCenters = new List<Vector3>();
            List<float> childYPositions = new List<float>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                MeshRenderer rend = child.GetComponentInChildren<MeshRenderer>();
                MeshCollider meshCol = child.GetComponentInChildren<MeshCollider>();

                Vector3 center = child.position;
                if (rend != null) center = rend.bounds.center;
                else if (meshCol != null) center = meshCol.bounds.center;

                childTransforms.Add(child);
                childBoundsCenters.Add(center);
                childYPositions.Add(center.y);
            }

            int[] sortedIndices = new int[childTransforms.Count];
            for (int i = 0; i < sortedIndices.Length; i++) sortedIndices[i] = i;
            System.Array.Sort(sortedIndices, (a, b) => childYPositions[b].CompareTo(childYPositions[a]));

            pivotWorldPos = childBoundsCenters[sortedIndices[0]];

            float topY = childYPositions[sortedIndices[0]];
            float bottomY = childYPositions[sortedIndices[sortedIndices.Length - 1]];
            ropeLength = Mathf.Abs(topY - bottomY);

            if (ropeLength < 0.01f)
            {
                ropeLength = childTransforms.Count * 0.5f;
                for (int order = 0; order < sortedIndices.Length; order++)
                {
                    int idx = sortedIndices[order];
                    float t = sortedIndices.Length > 1 ? (float)order / (sortedIndices.Length - 1) : 0f;

                    segments.Add(new SegmentData
                    {
                        segTransform = childTransforms[idx],
                        normalizedPosition = t,
                        distFromPivot = t * ropeLength,
                        restBoundsCenter = childBoundsCenters[idx],
                        restVectorFromPivot = childBoundsCenters[idx] - pivotWorldPos,
                        originalLocalPosition = childTransforms[idx].localPosition,
                        originalLocalRotation = childTransforms[idx].localRotation
                    });
                }
            }
            else
            {
                for (int order = 0; order < sortedIndices.Length; order++)
                {
                    int idx = sortedIndices[order];
                    float dist = Mathf.Abs(childYPositions[idx] - topY);
                    float t = dist / ropeLength;

                    segments.Add(new SegmentData
                    {
                        segTransform = childTransforms[idx],
                        normalizedPosition = t,
                        distFromPivot = dist,
                        restBoundsCenter = childBoundsCenters[idx],
                        restVectorFromPivot = childBoundsCenters[idx] - pivotWorldPos,
                        originalLocalPosition = childTransforms[idx].localPosition,
                        originalLocalRotation = childTransforms[idx].localRotation
                    });
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                GameObject linkObj = segments[i].segTransform.gameObject;

                foreach (var joint in linkObj.GetComponents<Joint>()) DestroyImmediate(joint);
                Rigidbody existingRb = linkObj.GetComponent<Rigidbody>();
                if (existingRb != null) DestroyImmediate(existingRb);

                MeshCollider meshCol = linkObj.GetComponent<MeshCollider>();
                if (meshCol != null)
                {
                    meshCol.convex = true;
                    meshCol.isTrigger = true;
                }

                bool isGrabTarget = IsTargetGrabRing(linkObj, i, segments.Count);

                if (isGrabTarget)
                {
                    bool hasTrigger = false;
                    foreach (var col in linkObj.GetComponents<SphereCollider>())
                    {
                        if (col.isTrigger) { hasTrigger = true; col.radius = grabTriggerRadius; break; }
                    }
                    if (!hasTrigger)
                    {
                        SphereCollider trigger = linkObj.AddComponent<SphereCollider>();
                        trigger.isTrigger = true;
                        trigger.radius = grabTriggerRadius;
                    }

                    VineLinkNode3D vineNode = linkObj.GetComponent<VineLinkNode3D>();
                    if (vineNode == null) vineNode = linkObj.AddComponent<VineLinkNode3D>();
                    vineNode.normalizedPosition = segments[i].normalizedPosition;
                    vineNode.parentBuilder = this;
                }
                else
                {
                    VineLinkNode3D vineNode = linkObj.GetComponent<VineLinkNode3D>();
                    if (vineNode != null) DestroyImmediate(vineNode);

                    foreach (var col in linkObj.GetComponents<SphereCollider>())
                    {
                        if (col.isTrigger) DestroyImmediate(col);
                    }
                }
            }

            endPosition = pivotWorldPos + Vector3.down * ropeLength;
            endVelocity = Vector3.zero;
            smoothedEndPosition = endPosition;
        }

        private bool IsTargetGrabRing(GameObject linkObj, int index, int totalCount)
        {
            if (!onlyAttachToEndRing) return true;

            if (!string.IsNullOrEmpty(targetEndObjectName))
            {
                if (linkObj.name.Equals(targetEndObjectName, System.StringComparison.OrdinalIgnoreCase) ||
                    linkObj.name.Contains(targetEndObjectName))
                {
                    return true;
                }
            }

            return (index == totalCount - 1);
        }

        private void FixedUpdate()
        {
            Simulate3DPendulum(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            UpdateVisualPositions();
        }

        /// <summary>
        /// A/D(좌우 X)와 W/S(앞뒤 Z) 3차원 전방향으로 흔들리는 3D 구면 진자 물리 연산
        /// </summary>
        private void Simulate3DPendulum(float dt)
        {
            if (ropeLength <= 0f) return;

            // 1. 중력 및 감쇄 적용
            endVelocity += Vector3.down * gravity * dt;
            endVelocity -= endVelocity * damping * dt;

            // 2. 축 제한 모드가 아닐 때는 X, Z(좌우, 앞뒤) 모든 속도 보존
            if (!allow3DSphericalSwing)
            {
                Vector3 axis = fallbackSwingAxis.normalized;
                if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;

                endVelocity = axis * Vector3.Dot(endVelocity, axis)
                            + Vector3.up * Vector3.Dot(endVelocity, Vector3.up);
            }

            endPosition += endVelocity * dt;

            // 3. 로프 고정 길이 구면 구속 (Spherical Constraint)
            Vector3 fromPivot = endPosition - pivotWorldPos;
            float currentDist = fromPivot.magnitude;

            if (currentDist > 0.001f)
            {
                Vector3 ropeDir = fromPivot.normalized;
                endPosition = pivotWorldPos + ropeDir * ropeLength;

                // 로프를 늘리거나 줄이는 방향(반경 방향)의 속도 성분을 제거하여 구면 접선 운동만 유지
                float radialSpeed = Vector3.Dot(endVelocity, ropeDir);
                endVelocity -= ropeDir * radialSpeed;
            }
            else
            {
                endPosition = pivotWorldPos + Vector3.down * ropeLength;
                endVelocity = Vector3.zero;
            }

            // 4. 3D 원뿔 형태의 최대 스윙 각도(maxSwingAngle) 제한
            Vector3 finalFromPivot = endPosition - pivotWorldPos;
            float currentAngle = Vector3.Angle(Vector3.down, finalFromPivot);

            if (currentAngle > maxSwingAngle)
            {
                Vector3 horizontalOffset = new Vector3(finalFromPivot.x, 0f, finalFromPivot.z);
                if (horizontalOffset.sqrMagnitude > 0.0001f)
                {
                    float rad = maxSwingAngle * Mathf.Deg2Rad;
                    Vector3 limitedDir = (horizontalOffset.normalized * Mathf.Sin(rad) + Vector3.down * Mathf.Cos(rad)).normalized;

                    endPosition = pivotWorldPos + limitedDir * ropeLength;

                    // 한계에 부딪혔을 때의 속도 감쇄
                    float dot = Vector3.Dot(endVelocity, limitedDir);
                    if (dot > 0f) endVelocity -= limitedDir * dot;
                    endVelocity *= 0.7f;
                }
            }
        }

        /// <summary>
        /// 플레이어의 A/D(좌우) 및 W/S(앞뒤) 입력을 3D 물리 속도로 즉시 가산합니다.
        /// </summary>
        public void ApplyForce(Vector3 force)
        {
            if (allow3DSphericalSwing)
            {
                endVelocity += force;
            }
            else
            {
                Vector3 axis = fallbackSwingAxis.normalized;
                if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;
                endVelocity += axis * Vector3.Dot(force, axis);
            }
        }

        public Vector3 GetVelocityAtNormalized(float t)
        {
            if (ropeLength <= 0f) return Vector3.zero;
            return endVelocity * t;
        }

        private void UpdateVisualPositions()
        {
            if (ropeLength <= 0f) return;

            float lagTime = inertiaLag > 0f ? inertiaLag : 0.01f;
            smoothedEndPosition = Vector3.SmoothDamp(smoothedEndPosition, endPosition, ref endPositionVelocity, lagTime, Mathf.Infinity, Time.deltaTime);

            Vector3 targetDir = (endPosition - pivotWorldPos).normalized;
            Vector3 smoothedDir = (smoothedEndPosition - pivotWorldPos).normalized;

            if (targetDir.sqrMagnitude < 0.001f) targetDir = Vector3.down;
            if (smoothedDir.sqrMagnitude < 0.001f) smoothedDir = Vector3.down;

            Vector3 currentTargetCenter = pivotWorldPos;
            Vector3 prevRestCenter = segments.Count > 0 ? segments[0].restBoundsCenter : pivotWorldPos;

            for (int i = 0; i < segments.Count; i++)
            {
                SegmentData seg = segments[i];

                if (seg.normalizedPosition < 0.001f)
                {
                    currentTargetCenter = pivotWorldPos;
                    prevRestCenter = seg.restBoundsCenter;
                    continue;
                }

                float blend = Mathf.Lerp(1f - flexibility, 1f, seg.normalizedPosition);
                Vector3 tangentDir = Vector3.Slerp(smoothedDir, targetDir, blend).normalized;
                if (tangentDir.sqrMagnitude < 0.001f) tangentDir = Vector3.down;

                Quaternion swingRot = Quaternion.FromToRotation(Vector3.down, tangentDir);

                Vector3 restDelta = seg.restBoundsCenter - prevRestCenter;
                currentTargetCenter = currentTargetCenter + (swingRot * restDelta);

                prevRestCenter = seg.restBoundsCenter;

                Transform parent = seg.segTransform.parent;
                Vector3 originalWorldOrigin = parent != null
                    ? parent.TransformPoint(seg.originalLocalPosition)
                    : seg.originalLocalPosition;

                Vector3 worldOM = seg.restBoundsCenter - originalWorldOrigin;
                Vector3 newWorldOM = swingRot * worldOM;
                Vector3 newWorldOrigin = currentTargetCenter - newWorldOM;

                Vector3 worldDelta = newWorldOrigin - originalWorldOrigin;
                Vector3 localDelta = parent != null ? parent.InverseTransformVector(worldDelta) : worldDelta;

                seg.segTransform.localPosition = seg.originalLocalPosition + localDelta;

                if (parent != null)
                {
                    Quaternion parentInverse = Quaternion.Inverse(parent.rotation);
                    seg.segTransform.localRotation = parentInverse * swingRot * parent.rotation * seg.originalLocalRotation;
                }
                else
                {
                    seg.segTransform.localRotation = swingRot * seg.originalLocalRotation;
                }
            }
        }
    }
}
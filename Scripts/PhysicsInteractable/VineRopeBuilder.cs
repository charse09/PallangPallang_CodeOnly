using UnityEngine;
using System.Collections.Generic;

namespace _Project.Scripts.PhysicsInteractable
{
    public class VineLinkNode : MonoBehaviour
    {
        [HideInInspector] public float normalizedPosition;
        [HideInInspector] public VineRopeBuilder parentBuilder;
    }

    /// <summary>
    /// ?섏쐞 ?ㅻ툕?앺듃?ㅼ쓣 "????앹엥"泥섎읆 ?붾뱾由щ뒗 3D ?⑥씪 吏꾩옄(Pendulum)濡?援ъ꽦?⑸땲??
    /// </summary>
    public class VineRopeBuilder : MonoBehaviour
    {
        [Header("=== Pendulum Settings ===")]
        [Tooltip("吏꾩옄??媛먯뇿 怨꾩닔. ?믪쓣?섎줉 鍮⑤━ 硫덉땄 (0?대㈃ 媛먯뇿 ?놁쓬)")]
        [Range(0f, 2f)]
        public float damping = 0.3f;

        [Tooltip("吏꾩옄???ъ슜??以묐젰 媛?띾룄 ?ш린")]
        public float gravity = 9.81f;

        [Tooltip("理쒕? ?ㅼ쐷 媛곷룄 ?쒗븳 (???⑥쐞)")]
        [Range(10f, 90f)]
        public float maxSwingAngle = 60f;

        [Header("=== Visual Curve Settings ===")]
        [Tooltip("?ъ뒳???섏뼱吏??뺣룄. ?믪쓣?섎줉 ?앸?遺꾩씠 留롮씠 ?섏뼱吏묐땲??")]
        [Range(0f, 1f)]
        public float flexibility = 0.4f;

        [Tooltip("愿??吏???④낵. ?앹엥???吏곸씪 ??以묎컙 遺遺꾩씠 ?ㅻ뒭寃??곕씪?ㅻ뒗 ?먮굦??以띾땲??")]
        [Range(0f, 0.5f)]
        public float inertiaLag = 0.15f;

        [Header("=== Swing Axis ===")]
        [Tooltip("?붾뱾由?諛⑺뼢 異뺤쓣 ?붾뱶 湲곗??쇰줈 ?ㅼ젙?⑸땲?? (1,0,0) = X異?醫뚯슦), (0,0,1) = Z異??욌뮘)\n??酉곗뿉??珥덈줉 ?좎쑝濡??쒖떆?⑸땲??")]
        public Vector3 swingAxis = Vector3.right;

                [Header("=== Interaction Settings ===")]
        [Tooltip("플레이어가 잡을 수 있는 고리 트리거 반경 (Trigger Radius)")]
        public float grabTriggerRadius = 1.5f;

        [Tooltip("플레이어가 오직 끝 고리(또는 지정된 고리)에만 잡히도록 설정합니까?")]
        public bool onlyAttachToEndRing = true;

        [Tooltip("잡을 수 있는 끝 고리 오브젝트 이름 (예: Object002 (1)).\n비워두면 Y좌표가 가장 낮은 맨 아래 고리에 자동으로 생성됩니다.")]
        public string targetEndObjectName = "Object002 (1)";

        // ??? ?고????곹깭 (3D Pendulum) ???
        [HideInInspector] public Vector3 endPosition;
        [HideInInspector] public Vector3 endVelocity;

        public float RopeLength => ropeLength;
        public Vector3 PivotWorldPosition => pivotWorldPos;

        // ??? ?대? ?곗씠?????
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

        // ?쒓컖??愿??吏?곗슜
        private Vector3 smoothedEndPosition;
        private Vector3 endPositionVelocity;

        private void Awake()
        {
            BuildVineRope();
        }

        [ContextMenu("Build Vine Rope Now (Editor)")]
        private void BuildVineRope()
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

                    VineLinkNode vineNode = linkObj.GetComponent<VineLinkNode>();
                    if (vineNode == null) vineNode = linkObj.AddComponent<VineLinkNode>();
                    vineNode.normalizedPosition = segments[i].normalizedPosition;
                    vineNode.parentBuilder = this;
                }
                else
                {
                    // 중간 고리에 생성된 기존 트리거 및 VineLinkNode 제거
                    VineLinkNode vineNode = linkObj.GetComponent<VineLinkNode>();
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

            // 끝 오브젝트 이름이 매칭되지 않거나 비어있는 경우, 맨 아래 고리(index == totalCount - 1) 선택
            return (index == totalCount - 1);
        }

        private void FixedUpdate()
        {
            SimulatePendulum(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            UpdateVisualPositions();
        }

        private void SimulatePendulum(float dt)
        {
            if (ropeLength <= 0f) return;

            // swingAxis瑜??뺢퇋?뷀븯???ъ슜 (?뱀떆 ?ㅼ뿼??媛?諛⑹뼱)
            Vector3 axis = swingAxis.normalized;
            if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;

            // 以묐젰 ?곸슜
            endVelocity += Vector3.down * gravity * dt;
            // 媛먯뇿 ?곸슜
            endVelocity -= endVelocity * damping * dt;

            // ???듭떖: ?띾룄瑜?swingAxis 諛⑺뼢 ?깅텇留??④린怨??섎㉧吏 ?쒓굅 (?됰㈃ ?쒗븳)
            endVelocity = axis * Vector3.Dot(endVelocity, axis)
                        + Vector3.up * Vector3.Dot(endVelocity, Vector3.up);

            endPosition += endVelocity * dt;

            // Pivot?쇰줈遺??濡쒗봽 湲몄씠 ?좎? (嫄곕━ ?쒗븳)
            Vector3 fromPivot = endPosition - pivotWorldPos;
            float currentDist = fromPivot.magnitude;

            if (currentDist > 0.001f)
            {
                // ??fromPivot??swingAxis + ?섏쭅 ?됰㈃?쇰줈 ?ъ쁺?섏뿬 ?쒗븳
                Vector3 horizontal = axis * Vector3.Dot(fromPivot, axis);
                Vector3 vertical = Vector3.up * Vector3.Dot(fromPivot, Vector3.up);
                fromPivot = horizontal + vertical;
                if (fromPivot.sqrMagnitude < 0.001f) fromPivot = Vector3.down;

                Vector3 ropeDir = fromPivot.normalized;
                endPosition = pivotWorldPos + ropeDir * ropeLength;

                float dot = Vector3.Dot(endVelocity, ropeDir);
                endVelocity -= ropeDir * dot;
            }
            else
            {
                endPosition = pivotWorldPos + Vector3.down * ropeLength;
                endVelocity = Vector3.zero;
            }

            // 理쒕? ?ㅼ쐷 媛곷룄 ?쒗븳
            Vector3 finalFromPivot = endPosition - pivotWorldPos;
            float angle = Vector3.Angle(Vector3.down, finalFromPivot);
            if (angle > maxSwingAngle)
            {
                Vector3 limitAxis = Vector3.Cross(Vector3.down, finalFromPivot).normalized;
                if (limitAxis.sqrMagnitude > 0.001f)
                {
                    Quaternion maxRot = Quaternion.AngleAxis(maxSwingAngle, limitAxis);
                    Vector3 limitedDir = maxRot * Vector3.down;
                    endPosition = pivotWorldPos + limitedDir * ropeLength;

                    Vector3 newDir = limitedDir.normalized;
                    endVelocity -= newDir * Vector3.Dot(endVelocity, newDir);
                    endVelocity *= 0.5f;
                }
            }
        }

        public void ApplyForce(Vector3 force)
        {
            // 媛?댁????섎룄 swingAxis 諛⑺뼢 ?깅텇留??④꺼 ?쇨????좎?
            Vector3 axis = swingAxis.normalized;
            if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;
            endVelocity += axis * Vector3.Dot(force, axis);
        }

        public Vector3 GetVelocityAtNormalized(float t)
        {
            if (ropeLength <= 0f) return Vector3.zero;
            return endVelocity * t;
        }

        private void UpdateVisualPositions()
        {
            if (ropeLength <= 0f) return;

            // ?쒓컖?곸씤 而ㅻ툕(?섏뼱吏?瑜??꾪븳 愿??泥섎━
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
                    // pivot? ?쒖옄由??좎?
                    currentTargetCenter = pivotWorldPos;
                    prevRestCenter = seg.restBoundsCenter;
                    continue;
                }

                // 1. 媛??ъ뒳 議곌컖蹂??섏뼱吏?諛⑺뼢 怨꾩궛
                float blend = Mathf.Lerp(1f - flexibility, 1f, seg.normalizedPosition);
                Vector3 tangentDir = Vector3.Slerp(smoothedDir, targetDir, blend).normalized;
                if (tangentDir.sqrMagnitude < 0.001f) tangentDir = Vector3.down;

                Quaternion swingRot = Quaternion.FromToRotation(Vector3.down, tangentDir);

                // 2. FK (Forward Kinematics): ?댁쟾 議곌컖 ?꾩튂?먯꽌 ?먮옒 嫄곕━(restDelta)留뚰겮 ?뚯쟾?쒖폒 ?뷀븿
                // ?대젃寃??댁빞 而ㅻ툕媛 ?ㅼ뼱媛???ъ뒳 ?ъ씠媛 ?덈? 踰뚯뼱吏嫄곕굹 ?딆뼱吏吏 ?딆쓬
                Vector3 restDelta = seg.restBoundsCenter - prevRestCenter;
                currentTargetCenter = currentTargetCenter + (swingRot * restDelta);

                prevRestCenter = seg.restBoundsCenter;

                // 3. 硫붿돩 ?먯젏 ?ㅽ봽??OM) 怨꾩궛?섏뿬 Transform ?곸슜
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


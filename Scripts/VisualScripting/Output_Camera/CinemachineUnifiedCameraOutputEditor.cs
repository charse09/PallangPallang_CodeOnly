#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [CustomEditor(typeof(CinemachineUnifiedCameraOutput))]
    public class CinemachineUnifiedCameraOutputEditor : Editor
    {
        // 1. 카메라 Switch & Blend
        private SerializedProperty targetVirtualCamera;
        private SerializedProperty enableCameraSwitch;
        private SerializedProperty blendStyle;
        private SerializedProperty blendTime;

        // 2. Motion Mode & Params
        private SerializedProperty motionMode;
        private SerializedProperty presetType;
        private SerializedProperty distance;
        private SerializedProperty angle;
        private SerializedProperty arcTarget;
        private SerializedProperty arcRadius;

        private SerializedProperty pointATransform;
        private SerializedProperty pointBTransform;

        private SerializedProperty offsetA;
        private SerializedProperty rotationOffsetA;
        private SerializedProperty offsetB;
        private SerializedProperty rotationOffsetB;

        // 3. Timing & Easing
        private SerializedProperty holdDurationA;
        private SerializedProperty transitionDuration;
        private SerializedProperty easeType;
        private SerializedProperty customEaseCurve;
        private SerializedProperty holdDurationB;

        private void OnEnable()
        {
            targetVirtualCamera = serializedObject.FindProperty("targetVirtualCamera");
            enableCameraSwitch = serializedObject.FindProperty("enableCameraSwitch");
            blendStyle = serializedObject.FindProperty("blendStyle");
            blendTime = serializedObject.FindProperty("blendTime");

            motionMode = serializedObject.FindProperty("motionMode");
            presetType = serializedObject.FindProperty("presetType");
            distance = serializedObject.FindProperty("distance");
            angle = serializedObject.FindProperty("angle");
            arcTarget = serializedObject.FindProperty("arcTarget");
            arcRadius = serializedObject.FindProperty("arcRadius");

            pointATransform = serializedObject.FindProperty("pointATransform");
            pointBTransform = serializedObject.FindProperty("pointBTransform");

            offsetA = serializedObject.FindProperty("offsetA");
            rotationOffsetA = serializedObject.FindProperty("rotationOffsetA");
            offsetB = serializedObject.FindProperty("offsetB");
            rotationOffsetB = serializedObject.FindProperty("rotationOffsetB");

            holdDurationA = serializedObject.FindProperty("holdDurationA");
            transitionDuration = serializedObject.FindProperty("transitionDuration");
            easeType = serializedObject.FindProperty("easeType");
            customEaseCurve = serializedObject.FindProperty("customEaseCurve");
            holdDurationB = serializedObject.FindProperty("holdDurationB");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // -------------------------------------------------------------
            // SECTION 1. 카메라 진입 & 전환(Blend) 설정
            // -------------------------------------------------------------
            EditorGUILayout.LabelField("1. 카메라 진입 & 전환(Blend) 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(targetVirtualCamera, new GUIContent("대상 가상 카메라"));
            EditorGUILayout.PropertyField(enableCameraSwitch, new GUIContent("카메라 전환 활성화"));

            if (enableCameraSwitch.boolValue)
            {
                EditorGUILayout.PropertyField(blendStyle, new GUIContent("전환 블렌드 방식"));
                EditorGUILayout.PropertyField(blendTime, new GUIContent("전환 블렌드 시간(초)"));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);

            // -------------------------------------------------------------
            // SECTION 2. 카메라 모션 연출 설정
            // -------------------------------------------------------------
            EditorGUILayout.LabelField("2. 카메라 모션 연출 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(motionMode, new GUIContent("모션 방식"));

            CameraMotionMode mode = (CameraMotionMode)motionMode.enumValueIndex;

            if (mode == CameraMotionMode.PresetMotion)
            {
                EditorGUILayout.PropertyField(presetType, new GUIContent("프리셋 종류"));
                CameraPresetType preset = (CameraPresetType)presetType.enumValueIndex;

                switch (preset)
                {
                    case CameraPresetType.DollyIn:
                    case CameraPresetType.DollyOut:
                        EditorGUILayout.PropertyField(distance, new GUIContent("전진/후진 거리 (Z축)"));
                        break;

                    case CameraPresetType.Slide:
                        EditorGUILayout.PropertyField(distance, new GUIContent("좌우 이동 거리 (X축)"));
                        break;

                    case CameraPresetType.BoomUp:
                    case CameraPresetType.BoomDown:
                        EditorGUILayout.PropertyField(distance, new GUIContent("상하 이동 거리 (Y축)"));
                        break;

                    case CameraPresetType.Pan:
                        EditorGUILayout.PropertyField(angle, new GUIContent("좌우 회전 각도 (Y축 회전 / Yaw)"));
                        break;

                    case CameraPresetType.Tilt:
                        EditorGUILayout.PropertyField(angle, new GUIContent("상하 회전 각도 (X축 회전 / Pitch)"));
                        break;

                    case CameraPresetType.Arc:
                        EditorGUILayout.PropertyField(arcTarget, new GUIContent("중심 타겟"));
                        EditorGUILayout.PropertyField(arcRadius, new GUIContent("회전 반경 (Radius)"));
                        EditorGUILayout.PropertyField(angle, new GUIContent("호 각도 (Y축 회전 / Yaw)"));
                        break;
                }
            }
            else if (mode == CameraMotionMode.TwoPointTransform)
            {
                EditorGUILayout.PropertyField(pointATransform, new GUIContent("Point A Target"));
                EditorGUILayout.PropertyField(pointBTransform, new GUIContent("Point B Target"));
            }
            else if (mode == CameraMotionMode.TwoPointOffset)
            {
                EditorGUILayout.PropertyField(offsetA, new GUIContent("Point A 위치 오프셋"));
                EditorGUILayout.PropertyField(rotationOffsetA, new GUIContent("Point A 회전 오프셋"));
                EditorGUILayout.PropertyField(offsetB, new GUIContent("Point B 위치 오프셋"));
                EditorGUILayout.PropertyField(rotationOffsetB, new GUIContent("Point B 회전 오프셋"));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);

            // -------------------------------------------------------------
            // SECTION 3. 타이밍 & 가속도(Easing) 설정
            // -------------------------------------------------------------
            EditorGUILayout.LabelField("3. 타이밍 & 가속도(Easing) 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(holdDurationA, new GUIContent("1st 유지 시간 (Point A)"));

            if (mode != CameraMotionMode.None)
            {
                EditorGUILayout.PropertyField(transitionDuration, new GUIContent("모션 전환 속도/시간(초)"));
                EditorGUILayout.PropertyField(easeType, new GUIContent("가속도(Easing) 방식"));

                CameraEaseType selectedEase = (CameraEaseType)easeType.enumValueIndex;
                if (selectedEase == CameraEaseType.CustomCurve)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(customEaseCurve, new GUIContent("커스텀 커브 그래프"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(holdDurationB, new GUIContent("2nd 유지 시간 (Point B)"));
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();

            // 강제 실행 테스트 버튼
            EditorGUILayout.Space(10);
            if (GUILayout.Button("강제 실행 (Execute Test)"))
            {
                CinemachineUnifiedCameraOutput node = (CinemachineUnifiedCameraOutput)target;
                node.ForceExecute();
            }
        }
    }
}
#endif
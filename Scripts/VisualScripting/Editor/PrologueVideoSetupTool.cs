#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using _Project.Scripts.VisualScripting;
using _Project.Scripts.VisualScripting.Output_UI;

namespace _Project.Scripts.VisualScripting.Editor
{
    public class PrologueVideoSetupWindow : EditorWindow
    {
        private string videoResourcesPath = "Assets/Scenes/Prologue/3__Prologue_Video_Resources";
        private bool keepExistingLeadingOutputs = true; // SetCursorOutput, ActivateSkipButton 유지
        private bool keepExistingTrailingOutputs = true; // 컷씬 이후 다른 Output이 있다면 유지
        private bool allowSkip = true;

        [MenuItem("Tools/PallangPallang/Prologue Video Auto Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrologueVideoSetupWindow>("Prologue Video Setup");
            window.minSize = new Vector2(480, 380);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3__Prologue 비디오 시퀀스 자동 세팅", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cut1부터 Cut11까지 비디오가 재생될 때 생기는 깜빡임이나 틈을 완벽하게 없앱니다.\n\n" +
                "★ [추천: 심리스 연속 재생]: 1개 노드에서 Cut1~Cut11을 틈/꺼짐 전혀 없이 물 흐르듯 연속 재생합니다.\n" +
                "★ [개별 노드 11개 재생]: 11개의 개별 노드로 분할하되, 중간에 창을 닫지 않고 심리스하게 이어갑니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);
            videoResourcesPath = EditorGUILayout.TextField("비디오 리소스 경로", videoResourcesPath);
            keepExistingLeadingOutputs = EditorGUILayout.Toggle("이전 노드 유지 (Cursor/Skip 등)", keepExistingLeadingOutputs);
            keepExistingTrailingOutputs = EditorGUILayout.Toggle("이후 노드 유지", keepExistingTrailingOutputs);
            allowSkip = EditorGUILayout.Toggle("스킵 허용 (Space/Click)", allowSkip);

            EditorGUILayout.Space(15);

            // 1. 추천 방식 (심리스 플레이리스트)
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("✨ [추천] 심리스 연속 재생 세팅 (틈/깜빡임 0%, Cut1~11)", GUILayout.Height(45)))
            {
                ExecuteSetupSeamlessPlaylist();
            }

            EditorGUILayout.Space(5);

            // 2. 개별 노드 방식
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.95f);
            if (GUILayout.Button("🎬 개별 노드 11개 분할 세팅 (노드 간 화면 유지 심리스)", GUILayout.Height(35)))
            {
                ExecuteSetupIndividualNodes();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(15);
            if (GUILayout.Button("기존 Dissolve 컷씬으로 원복(복원)하기", GUILayout.Height(28)))
            {
                RestoreOriginalDissolve();
            }
        }

        /// <summary>
        /// 방식 1: Playlist 모드로 1개 노드에서 Cut1~Cut11을 단 1프레임의 꺼짐/틈도 없이 완벽하게 연속 재생
        /// </summary>
        public void ExecuteSetupSeamlessPlaylist()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Setup Seamless Playlist Video");
            int undoGroup = Undo.GetCurrentGroup();

            RectTransform cutInRenderArea = FindCutInRenderArea();
            if (cutInRenderArea == null)
            {
                EditorUtility.DisplayDialog("오류", "DissolveCutInDialoguePrologueCanvas 내의 CutInRenderArea를 찾을 수 없습니다.", "확인");
                return;
            }

            GameObject videoScreenObj = GetOrCreateVideoScreen(cutInRenderArea, out RawImage rawImage, out VideoPlayer videoPlayer, out AudioSource audioSource);

            GameObject triggerObj = GameObject.Find("AutoTimerTrigger_2DCUTSCN01");
            if (triggerObj == null)
            {
                EditorUtility.DisplayDialog("오류", "Hierarchy에서 'AutoTimerTrigger_2DCUTSCN01'을 찾을 수 없습니다.", "확인");
                return;
            }

            SequenceConditional seq = triggerObj.GetComponentInChildren<SequenceConditional>(true);
            if (seq == null)
            {
                EditorUtility.DisplayDialog("오류", "AutoTimerTrigger_2DCUTSCN01 하위에서 SequenceConditional을 찾을 수 없습니다.", "확인");
                return;
            }

            Undo.RecordObject(seq, "Update SequenceConditional");

            // 기존 Dissolve 노드 끄기
            DisableOriginalDissolveNodes(triggerObj);

            // 비디오 클립 로드
            List<VideoClip> videoClips = LoadVideoClips(videoResourcesPath);
            if (videoClips.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", $"'{videoResourcesPath}' 경로에서 비디오 클립을 찾지 못했습니다.", "확인");
                return;
            }

            // VideoPlayOutputs 컨테이너 정리 및 단일 심리스 노드 생성
            Transform videoOutputsContainerTr = seq.transform.Find("VideoPlayOutputs");
            GameObject videoOutputsContainer;
            if (videoOutputsContainerTr == null)
            {
                videoOutputsContainer = new GameObject("VideoPlayOutputs");
                Undo.RegisterCreatedObjectUndo(videoOutputsContainer, "Create VideoPlayOutputs Container");
                videoOutputsContainer.transform.SetParent(seq.transform, false);
            }
            else
            {
                videoOutputsContainer = videoOutputsContainerTr.gameObject;
                videoOutputsContainer.SetActive(true);
                for (int i = videoOutputsContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(videoOutputsContainer.transform.GetChild(i).gameObject);
                }
            }

            GameObject seamlessNodeObj = new GameObject("VideoPlayOutput_SeamlessSequence");
            Undo.RegisterCreatedObjectUndo(seamlessNodeObj, "Create VideoPlayOutput_SeamlessSequence");
            seamlessNodeObj.transform.SetParent(videoOutputsContainer.transform, false);

            Transform dialoguePanelTr = cutInRenderArea.parent;
            GameObject rootWindowObj = (dialoguePanelTr != null) ? dialoguePanelTr.gameObject : videoScreenObj;

            VideoPlayOutput vOut = seamlessNodeObj.AddComponent<VideoPlayOutput>();
            vOut.SetPlaylist(videoClips);
            vOut.SetTargetDisplay(rawImage);
            vOut.SetWindowRoot(rootWindowObj);
            vOut.SetHideWindowOnComplete(true);
            vOut.SetClearScreenOnComplete(true);

            SerializedObject so = new SerializedObject(vOut);
            so.FindProperty("playMode").enumValueIndex = (int)VideoPlayOutput.VideoPlayMode.Playlist;
            so.FindProperty("targetDisplay").objectReferenceValue = rawImage;
            so.FindProperty("windowRoot").objectReferenceValue = rootWindowObj;
            so.FindProperty("activateParentPanels").boolValue = true;
            so.FindProperty("showWindowOnPlay").boolValue = true;
            so.FindProperty("hideWindowOnComplete").boolValue = true;
            so.FindProperty("clearScreenOnComplete").boolValue = true;
            so.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("allowSkip").boolValue = allowSkip;
            so.FindProperty("allowMouseClickNextCut").boolValue = true; // 마우스 클릭 시 다음 Cut으로 1개씩 넘김

            SerializedProperty playlistProp = so.FindProperty("playlist");
            playlistProp.ClearArray();
            for (int i = 0; i < videoClips.Count; i++)
            {
                playlistProp.InsertArrayElementAtIndex(i);
                playlistProp.GetArrayElementAtIndex(i).objectReferenceValue = videoClips[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // SequenceConditional에 단일 심리스 노드 등록
            UpdateSequenceConditional(seq, new List<ProcessBase> { vOut });

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog(
                "심리스 연속 재생 세팅 완료!",
                $"총 {videoClips.Count}개의 비디오(Cut1~Cut{videoClips.Count})가 단 하나의 심리스 노드에 등록되었습니다.\n\n" +
                $"✓ 비디오가 시작될 때 VideoScreen 및 상위 패널이 자동으로 켜집니다.\n" +
                $"✓ Cut1에서 Cut11까지 비디오가 바뀔 때 창이 꺼지지 않고 0%의 틈 없이 매끄럽게 이어집니다!\n" +
                $"✓ 모든 비디오가 끝났을 때만 창이 자동으로 닫힙니다.\n" +
                $"✓ 재생 중 Space나 클릭을 누르면 다음 Cut으로 즉시 스킵됩니다.\n\n" +
                "씬을 저장(Ctrl + S) 후 테스트해 보세요!",
                "확인");
        }

        /// <summary>
        /// 방식 2: 11개의 개별 노드로 구성하되, 중간 노드들은 창을 끄지 않고 마지막 노드만 창을 끔
        /// </summary>
        public void ExecuteSetupIndividualNodes()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Setup Individual Video Nodes");
            int undoGroup = Undo.GetCurrentGroup();

            RectTransform cutInRenderArea = FindCutInRenderArea();
            if (cutInRenderArea == null)
            {
                EditorUtility.DisplayDialog("오류", "DissolveCutInDialoguePrologueCanvas 내의 CutInRenderArea를 찾을 수 없습니다.", "확인");
                return;
            }

            GameObject videoScreenObj = GetOrCreateVideoScreen(cutInRenderArea, out RawImage rawImage, out VideoPlayer videoPlayer, out AudioSource audioSource);

            GameObject triggerObj = GameObject.Find("AutoTimerTrigger_2DCUTSCN01");
            if (triggerObj == null)
            {
                EditorUtility.DisplayDialog("오류", "Hierarchy에서 'AutoTimerTrigger_2DCUTSCN01'을 찾을 수 없습니다.", "확인");
                return;
            }

            SequenceConditional seq = triggerObj.GetComponentInChildren<SequenceConditional>(true);
            if (seq == null)
            {
                EditorUtility.DisplayDialog("오류", "AutoTimerTrigger_2DCUTSCN01 하위에서 SequenceConditional을 찾을 수 없습니다.", "확인");
                return;
            }

            Undo.RecordObject(seq, "Update SequenceConditional");
            DisableOriginalDissolveNodes(triggerObj);

            List<VideoClip> videoClips = LoadVideoClips(videoResourcesPath);
            if (videoClips.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", $"'{videoResourcesPath}' 경로에서 비디오 클립을 찾지 못했습니다.", "확인");
                return;
            }

            Transform videoOutputsContainerTr = seq.transform.Find("VideoPlayOutputs");
            GameObject videoOutputsContainer;
            if (videoOutputsContainerTr == null)
            {
                videoOutputsContainer = new GameObject("VideoPlayOutputs");
                Undo.RegisterCreatedObjectUndo(videoOutputsContainer, "Create VideoPlayOutputs Container");
                videoOutputsContainer.transform.SetParent(seq.transform, false);
            }
            else
            {
                videoOutputsContainer = videoOutputsContainerTr.gameObject;
                videoOutputsContainer.SetActive(true);
                for (int i = videoOutputsContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(videoOutputsContainer.transform.GetChild(i).gameObject);
                }
            }

            List<ProcessBase> createdOutputs = new List<ProcessBase>();
            for (int i = 0; i < videoClips.Count; i++)
            {
                var clip = videoClips[i];
                bool isLastClip = (i == videoClips.Count - 1);

                GameObject nodeObj = new GameObject($"VideoPlayOutput_{clip.name}");
                Undo.RegisterCreatedObjectUndo(nodeObj, $"Create {nodeObj.name}");
                nodeObj.transform.SetParent(videoOutputsContainer.transform, false);

                Transform dialoguePanelTr = cutInRenderArea.parent;
                GameObject rootWindowObj = (dialoguePanelTr != null) ? dialoguePanelTr.gameObject : videoScreenObj;

                VideoPlayOutput vOut = nodeObj.AddComponent<VideoPlayOutput>();
                vOut.SetVideoClip(clip);
                vOut.SetTargetDisplay(rawImage);
                vOut.SetWindowRoot(rootWindowObj);
                // ★ 핵심: 중간 노드는 창을 끄지 않고 화면을 유지! 마지막 노드만 창을 끔!
                vOut.SetHideWindowOnComplete(isLastClip);
                vOut.SetClearScreenOnComplete(isLastClip);

                SerializedObject so = new SerializedObject(vOut);
                so.FindProperty("playMode").enumValueIndex = (int)VideoPlayOutput.VideoPlayMode.SingleClip;
                so.FindProperty("videoClip").objectReferenceValue = clip;
                so.FindProperty("targetDisplay").objectReferenceValue = rawImage;
                so.FindProperty("windowRoot").objectReferenceValue = rootWindowObj;
                so.FindProperty("activateParentPanels").boolValue = true;
                so.FindProperty("showWindowOnPlay").boolValue = (i == 0); // 첫 번째만 켜기 호출
                so.FindProperty("hideWindowOnComplete").boolValue = isLastClip; // 마지막만 끄기
                so.FindProperty("clearScreenOnComplete").boolValue = isLastClip;
                so.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
                so.FindProperty("audioSource").objectReferenceValue = audioSource;
                so.FindProperty("allowSkip").boolValue = allowSkip;
                so.ApplyModifiedPropertiesWithoutUndo();

                createdOutputs.Add(vOut);
            }

            UpdateSequenceConditional(seq, createdOutputs);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog(
                "개별 노드 세팅 완료!",
                $"총 {createdOutputs.Count}개의 개별 노드가 등록되었습니다.\n\n" +
                $"✓ Cut1~Cut10은 비디오가 끝날 때 창을 닫지 않고 화면을 유지합니다.\n" +
                $"✓ 마지막 Cut11이 끝날 때 창이 자동으로 꺼집니다.\n\n" +
                "씬을 저장(Ctrl + S) 후 테스트해 보세요!",
                "확인");
        }

        private GameObject GetOrCreateVideoScreen(RectTransform cutInRenderArea, out RawImage rawImage, out VideoPlayer videoPlayer, out AudioSource audioSource)
        {
            Transform videoScreenTr = cutInRenderArea.Find("VideoScreen");
            GameObject videoScreenObj;
            if (videoScreenTr == null)
            {
                videoScreenObj = new GameObject("VideoScreen");
                Undo.RegisterCreatedObjectUndo(videoScreenObj, "Create VideoScreen");
                videoScreenObj.transform.SetParent(cutInRenderArea, false);
            }
            else
            {
                videoScreenObj = videoScreenTr.gameObject;
                Undo.RecordObject(videoScreenObj, "Modify VideoScreen");
            }

            RectTransform screenRect = videoScreenObj.GetComponent<RectTransform>();
            if (screenRect == null) screenRect = videoScreenObj.AddComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0.5f, 0.5f);
            screenRect.anchorMax = new Vector2(0.5f, 0.5f);
            screenRect.pivot = new Vector2(0.5f, 0.5f);
            screenRect.anchoredPosition = Vector2.zero;
            screenRect.sizeDelta = new Vector2(1920f, 1080f);
            screenRect.localScale = Vector3.one;

            rawImage = videoScreenObj.GetComponent<RawImage>();
            if (rawImage == null) rawImage = videoScreenObj.AddComponent<RawImage>();
            rawImage.color = Color.black;
            rawImage.texture = null;
            rawImage.enabled = false;

            CanvasGroup canvasGroup = videoScreenObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = videoScreenObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            videoPlayer = videoScreenObj.GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = videoScreenObj.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            audioSource = videoScreenObj.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = videoScreenObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            videoScreenObj.SetActive(false);
            return videoScreenObj;
        }

        private void DisableOriginalDissolveNodes(GameObject triggerObj)
        {
            DissolveCutInDialogueOutput[] dissolveOutputs = triggerObj.GetComponentsInChildren<DissolveCutInDialogueOutput>(true);
            foreach (var dOut in dissolveOutputs)
            {
                Undo.RecordObject(dOut.gameObject, "Disable DissolveCutIn Output");
                dOut.gameObject.SetActive(false);
            }
        }

        private void UpdateSequenceConditional(SequenceConditional seq, List<ProcessBase> newOutputs)
        {
            SerializedObject seqSo = new SerializedObject(seq);
            SerializedProperty outputDataProp = seqSo.FindProperty("outputData");

            List<ProcessBase> leadingProcesses = new List<ProcessBase>();
            List<ProcessBase> trailingProcesses = new List<ProcessBase>();

            bool passedTargetZone = false;
            for (int i = 0; i < outputDataProp.arraySize; i++)
            {
                SerializedProperty elementProp = outputDataProp.GetArrayElementAtIndex(i);
                SerializedProperty procProp = elementProp.FindPropertyRelative("process");
                ProcessBase proc = procProp.objectReferenceValue as ProcessBase;

                if (proc is DissolveCutInDialogueOutput || proc is VideoPlayOutput)
                {
                    passedTargetZone = true;
                    continue;
                }

                if (!passedTargetZone)
                {
                    if (keepExistingLeadingOutputs && proc != null)
                    {
                        leadingProcesses.Add(proc);
                    }
                }
                else
                {
                    if (keepExistingTrailingOutputs && proc != null)
                    {
                        trailingProcesses.Add(proc);
                    }
                }
            }

            outputDataProp.ClearArray();
            int curIndex = 0;

            foreach (var lp in leadingProcesses)
            {
                outputDataProp.InsertArrayElementAtIndex(curIndex);
                var elem = outputDataProp.GetArrayElementAtIndex(curIndex);
                elem.FindPropertyRelative("process").objectReferenceValue = lp;
                elem.FindPropertyRelative("isNot").boolValue = false;
                curIndex++;
            }

            foreach (var vp in newOutputs)
            {
                outputDataProp.InsertArrayElementAtIndex(curIndex);
                var elem = outputDataProp.GetArrayElementAtIndex(curIndex);
                elem.FindPropertyRelative("process").objectReferenceValue = vp;
                elem.FindPropertyRelative("isNot").boolValue = false;
                curIndex++;
            }

            foreach (var tp in trailingProcesses)
            {
                outputDataProp.InsertArrayElementAtIndex(curIndex);
                var elem = outputDataProp.GetArrayElementAtIndex(curIndex);
                elem.FindPropertyRelative("process").objectReferenceValue = tp;
                elem.FindPropertyRelative("isNot").boolValue = false;
                curIndex++;
            }

            seqSo.ApplyModifiedProperties();
        }

        public void RestoreOriginalDissolve()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            GameObject triggerObj = GameObject.Find("AutoTimerTrigger_2DCUTSCN01");
            if (triggerObj == null) return;

            SequenceConditional seq = triggerObj.GetComponentInChildren<SequenceConditional>(true);
            if (seq == null) return;

            Undo.SetCurrentGroupName("Restore Dissolve CutIn");
            int undoGroup = Undo.GetCurrentGroup();

            DissolveCutInDialogueOutput[] dissolveOutputs = triggerObj.GetComponentsInChildren<DissolveCutInDialogueOutput>(true);
            foreach (var dOut in dissolveOutputs)
            {
                Undo.RecordObject(dOut.gameObject, "Enable DissolveCutIn Output");
                dOut.gameObject.SetActive(true);
            }

            Transform videoOutputsContainerTr = seq.transform.Find("VideoPlayOutputs");
            if (videoOutputsContainerTr != null)
            {
                Undo.RecordObject(videoOutputsContainerTr.gameObject, "Disable VideoPlayOutputs");
                videoOutputsContainerTr.gameObject.SetActive(false);
            }

            SerializedObject seqSo = new SerializedObject(seq);
            SerializedProperty outputDataProp = seqSo.FindProperty("outputData");

            List<ProcessBase> nonVideoProcesses = new List<ProcessBase>();
            for (int i = 0; i < outputDataProp.arraySize; i++)
            {
                SerializedProperty elementProp = outputDataProp.GetArrayElementAtIndex(i);
                SerializedProperty procProp = elementProp.FindPropertyRelative("process");
                ProcessBase proc = procProp.objectReferenceValue as ProcessBase;

                if (proc != null && !(proc is VideoPlayOutput))
                {
                    nonVideoProcesses.Add(proc);
                }
            }

            outputDataProp.ClearArray();
            int curIndex = 0;
            foreach (var p in nonVideoProcesses)
            {
                outputDataProp.InsertArrayElementAtIndex(curIndex);
                var elem = outputDataProp.GetArrayElementAtIndex(curIndex);
                elem.FindPropertyRelative("process").objectReferenceValue = p;
                elem.FindPropertyRelative("isNot").boolValue = false;
                curIndex++;
            }

            foreach (var dOut in dissolveOutputs)
            {
                if (!nonVideoProcesses.Contains(dOut))
                {
                    outputDataProp.InsertArrayElementAtIndex(curIndex);
                    var elem = outputDataProp.GetArrayElementAtIndex(curIndex);
                    elem.FindPropertyRelative("process").objectReferenceValue = dOut;
                    elem.FindPropertyRelative("isNot").boolValue = false;
                    curIndex++;
                }
            }

            seqSo.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog("복원 완료", "기존 Dissolve 컷씬 출력이 다시 활성화되었습니다.", "확인");
        }

        private RectTransform FindCutInRenderArea()
        {
            GameObject areaObj = GameObject.Find("CutInRenderArea");
            if (areaObj != null) return areaObj.GetComponent<RectTransform>();

            GameObject canvasObj = GameObject.Find("DissolveCutInDialoguePrologueCanvas");
            if (canvasObj != null)
            {
                Transform tr = canvasObj.transform.Find("DissolveCutInDialoguePanel/CutInRenderArea");
                if (tr != null) return tr.GetComponent<RectTransform>();

                var found = canvasObj.GetComponentInChildren<DissolveCutInDialogueManager>(true);
                if (found != null)
                {
                    Transform t = found.transform.Find("CutInRenderArea");
                    if (t != null) return t.GetComponent<RectTransform>();
                }
            }

            DissolveCutInDialogueManager mgr = Object.FindFirstObjectByType<DissolveCutInDialogueManager>(FindObjectsInactive.Include);
            if (mgr != null)
            {
                Transform parentPanel = mgr.transform.Find("DissolveCutInDialoguePanel");
                if (parentPanel != null)
                {
                    Transform tr = parentPanel.Find("CutInRenderArea");
                    if (tr != null) return tr.GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private List<VideoClip> LoadVideoClips(string folderPath)
        {
            List<VideoClip> list = new List<VideoClip>();

            for (int i = 1; i <= 100; i++)
            {
                string[] possibleExtensions = new string[] { ".mp4", ".mov", ".avi", ".webm" };
                VideoClip clip = null;

                foreach (var ext in possibleExtensions)
                {
                    string path = $"{folderPath}/Cut{i}{ext}";
                    clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                    if (clip != null) break;
                }

                if (clip != null)
                {
                    list.Add(clip);
                }
                else if (i > 11)
                {
                    break;
                }
            }

            return list;
        }
    }
}
#endif

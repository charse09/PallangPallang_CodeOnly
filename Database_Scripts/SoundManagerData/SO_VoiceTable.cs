using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "VoiceTable", menuName = "Audio/Voice Table")]
public class SO_VoiceTable : ScriptableObject
{
#if UNITY_EDITOR
    [Header("Folder Auto-Registration")]
    [Tooltip("자동 등록할 사운드 파일들이 모여있는 폴더를 드래그 앤 드롭하세요.")]
    [SerializeField] private DefaultAsset voiceFolder;

    [Tooltip("체크 시 하위 폴더의 사운드 파일까지 모두 탐색합니다.")]
    [SerializeField] private bool includeSubfolders = true;

    [Tooltip("체크 시 폴더에서 삭제된 오디오 클립을 테이블에서도 자동으로 제거합니다.")]
    [SerializeField] private bool removeMissingClips = true;
#endif

    [Header("Voice Data List")]
    [SerializeField] private List<VoiceData> voiceDataList = new List<VoiceData>();

    public List<VoiceData> VoiceDataList => voiceDataList;

    private Dictionary<string, VoiceData> _voiceDict;

    public void Initialize()
    {
        _voiceDict = new Dictionary<string, VoiceData>();
        foreach (var data in voiceDataList)
        {
            if (data != null && !string.IsNullOrEmpty(data.voiceKey))
            {
                _voiceDict[data.voiceKey] = data;
            }
        }
    }

    public VoiceData GetVoiceData(string key)
    {
        if (_voiceDict == null) Initialize();
        return _voiceDict.TryGetValue(key, out var data) ? data : null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 지정된 폴더 내의 모든 AudioClip을 탐색하여 테이블을 동기화합니다.
    /// 기존에 설정된 볼륨, 더킹, 3D 값은 덮어쓰지 않고 유지됩니다.
    /// </summary>
    [ContextMenu("폴더 내 사운드 자동 동기화 (Sync Folder)")]
    public void SyncAudioClipsFromFolder()
    {
        if (voiceFolder == null)
        {
            Debug.LogWarning($"[{name}] 동기화할 폴더(voiceFolder)가 지정되지 않았습니다.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(voiceFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[{name}] 올바른 폴더 에셋이 아닙니다.");
            return;
        }

        // 1. 기존 데이터 백업 (클립 이름 기준으로 기존 설정값 매핑)
        Dictionary<string, VoiceData> existingDataMap = new Dictionary<string, VoiceData>();
        foreach (var data in voiceDataList)
        {
            if (data != null && data.clip != null)
            {
                existingDataMap[data.clip.name] = data;
            }
        }

        // 2. 지정된 폴더 경로에서 AudioClip GUID 검색
        string[] searchFolders = new[] { folderPath };
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", searchFolders);

        List<VoiceData> updatedList = new List<VoiceData>();
        HashSet<string> scannedClipNames = new HashSet<string>();

        int addedCount = 0;
        int preservedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // 하위 폴더 제외 설정 시 검사
            if (!includeSubfolders)
            {
                string parentDir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (parentDir != folderPath) continue;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null) continue;

            scannedClipNames.Add(clip.name);

            // 3. 기존에 이미 존재하던 클립은 이전 설정 유지
            if (existingDataMap.TryGetValue(clip.name, out VoiceData existingData))
            {
                existingData.clip = clip; // 참조 갱신
                if (string.IsNullOrEmpty(existingData.voiceKey))
                {
                    existingData.voiceKey = clip.name;
                }
                updatedList.Add(existingData);
                preservedCount++;
            }
            else
            {
                // 4. 신규 클립 추가 (클립 이름을 voiceKey 기본값으로 사용)
                VoiceData newData = new VoiceData
                {
                    voiceKey = clip.name,
                    clip = clip,
                    speaker = "",
                    volume = 1f,
                    is3D = false,
                    enableDucking = true
                };
                updatedList.Add(newData);
                addedCount++;
            }
        }

        // 5. 누락된 파일 유지 여부 처리
        if (!removeMissingClips)
        {
            foreach (var kvp in existingDataMap)
            {
                if (!scannedClipNames.Contains(kvp.Key))
                {
                    updatedList.Add(kvp.Value);
                }
            }
        }

        // 6. 데이터 갱신 및 에셋 저장
        Undo.RecordObject(this, "Sync Voice Table From Folder");
        voiceDataList = updatedList;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[{name}] 동기화 완료! 추가된 클립: {addedCount}개 / 유지된 기존 클립: {preservedCount}개 (총 {voiceDataList.Count}개)");
    }
#endif
}
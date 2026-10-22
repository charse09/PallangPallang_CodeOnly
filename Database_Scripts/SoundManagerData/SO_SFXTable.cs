using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SFXTable", menuName = "Audio/SFX Table")]
public class SO_SFXTable : ScriptableObject
{
#if UNITY_EDITOR
    [Header("Folder Auto-Registration")]
    [Tooltip("자동 등록할 SFX 파일들이 모여있는 폴더를 드래그 앤 드롭하세요.")]
    [SerializeField] private DefaultAsset sfxFolder;
    [SerializeField] private bool includeSubfolders = true;
    [SerializeField] private bool removeMissingClips = true;
#endif

    [Header("SFX Data List")]
    [SerializeField] private List<SFXData> sfxDataList = new List<SFXData>();

    public List<SFXData> SFXDataList => sfxDataList;

    private Dictionary<string, SFXData> _sfxDict;

    public void Initialize()
    {
        _sfxDict = new Dictionary<string, SFXData>();
        foreach (var data in sfxDataList)
        {
            if (data != null && !string.IsNullOrEmpty(data.soundKey))
            {
                _sfxDict[data.soundKey] = data;
            }
        }
    }

    public SFXData GetSFXData(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_sfxDict == null) Initialize();
        return _sfxDict.TryGetValue(key, out var data) ? data : null;
    }

#if UNITY_EDITOR
    [ContextMenu("폴더 내 사운드 자동 동기화 (Sync Folder)")]
    public void SyncAudioClipsFromFolder()
    {
        if (sfxFolder == null)
        {
            Debug.LogWarning($"[{name}] 동기화할 폴더(sfxFolder)가 지정되지 않았습니다.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(sfxFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[{name}] 올바른 폴더 에셋이 아닙니다.");
            return;
        }

        Dictionary<string, SFXData> existingDataMap = new Dictionary<string, SFXData>();
        foreach (var data in sfxDataList)
        {
            if (data != null && data.clip != null)
            {
                existingDataMap[data.clip.name] = data;
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        List<SFXData> updatedList = new List<SFXData>();
        HashSet<string> scannedClipNames = new HashSet<string>();

        int addedCount = 0;
        int preservedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!includeSubfolders)
            {
                string parentDir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (parentDir != folderPath) continue;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null) continue;

            scannedClipNames.Add(clip.name);

            if (existingDataMap.TryGetValue(clip.name, out SFXData existingData))
            {
                existingData.clip = clip;
                if (string.IsNullOrEmpty(existingData.soundKey))
                    existingData.soundKey = clip.name;
                updatedList.Add(existingData);
                preservedCount++;
            }
            else
            {
                SFXData newData = new SFXData
                {
                    soundKey = clip.name,
                    clip = clip,
                    volume = 1f,
                    pitch = 1f,
                    randomPitchVariance = 0.05f,
                    is3D = true
                };
                updatedList.Add(newData);
                addedCount++;
            }
        }

        if (!removeMissingClips)
        {
            foreach (var kvp in existingDataMap)
            {
                if (!scannedClipNames.Contains(kvp.Key))
                    updatedList.Add(kvp.Value);
            }
        }

        Undo.RecordObject(this, "Sync SFX Table From Folder");
        sfxDataList = updatedList;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[{name}] SFX 동기화 완료! 추가: {addedCount}개 / 유지: {preservedCount}개 (총 {sfxDataList.Count}개)");
    }
#endif
}
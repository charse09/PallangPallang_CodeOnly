using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "EffectTable", menuName = "Effect/Effect Table")]
public class SO_EffectTable : ScriptableObject
{
    [System.Serializable]
    public class EffectData
    {
        [Tooltip("호출 키 (예: FootstepDust, HitImpact, 11, 22)")]
        public string effectKey;
        public EffectType type = EffectType.None;
        public GameObject prefab;
        public int initialPoolSize = 5;
    }

#if UNITY_EDITOR
    [Header("Folder Auto-Registration")]
    [Tooltip("이펙트 프리팹들이 모여있는 폴더를 드래그 앤 드롭하세요.")]
    [SerializeField] private DefaultAsset effectFolder;
    [SerializeField] private bool includeSubfolders = true;
#endif

    [Header("Effect Data List")]
    [SerializeField] private List<EffectData> effectDataList = new List<EffectData>();

    public List<EffectData> EffectDataList => effectDataList;

#if UNITY_EDITOR
    [ContextMenu("폴더 내 프리팹 자동 동기화 (Sync Folder)")]
    public void SyncPrefabsFromFolder()
    {
        if (effectFolder == null)
        {
            Debug.LogWarning($"[{name}] effectFolder가 지정되지 않았습니다.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(effectFolder);
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        Dictionary<string, EffectData> existingMap = new Dictionary<string, EffectData>();
        foreach (var data in effectDataList)
        {
            if (data != null && data.prefab != null)
                existingMap[data.prefab.name] = data;
        }

        List<EffectData> updatedList = new List<EffectData>();
        int addedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            if (existingMap.TryGetValue(prefab.name, out EffectData existingData))
            {
                existingData.prefab = prefab;
                if (string.IsNullOrEmpty(existingData.effectKey)) existingData.effectKey = prefab.name;
                updatedList.Add(existingData);
            }
            else
            {
                updatedList.Add(new EffectData
                {
                    effectKey = prefab.name,
                    prefab = prefab,
                    initialPoolSize = 3
                });
                addedCount++;
            }
        }

        Undo.RecordObject(this, "Sync Effect Table");
        effectDataList = updatedList;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[{name}] 이펙트 동기화 완료! 추가: {addedCount}개 / 총 {effectDataList.Count}개");
    }
#endif
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    [Serializable]
    public class CutInStringData
    {
        public string stringID;
        public string speaker;
        [TextArea(3, 10)]
        public string dialogue;
    }

    [CreateAssetMenu(fileName = "NewCutInStringTable", menuName = "CutIn/String Table")]
    public class CutInStringTable : ScriptableObject
    {
        public List<CutInStringData> entries = new List<CutInStringData>();

        public CutInStringData GetStringData(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var entry in entries)
            {
                if (entry.stringID == id)
                    return entry;
            }
            return null;
        }
    }
}

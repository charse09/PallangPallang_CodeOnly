using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BGMTable", menuName = "Audio/BGM Table")]
public class SO_BGMTable : ScriptableObject
{
    [SerializeField] private List<BGMData> bgmDataList = new List<BGMData>();

    public List<BGMData> BGMDataList => bgmDataList;
}
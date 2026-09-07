using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> _poolDict = new Dictionary<string, Queue<GameObject>>();

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;
        if (!_poolDict.ContainsKey(key))
            _poolDict.Add(key, new Queue<GameObject>());

        GameObject obj;
        if (_poolDict[key].Count > 0)
        {
            obj = _poolDict[key].Dequeue();
            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        string key = obj.name.Replace("(Clone)", "").Trim();
        if (_poolDict.ContainsKey(key))
            _poolDict[key].Enqueue(obj);
    }
}
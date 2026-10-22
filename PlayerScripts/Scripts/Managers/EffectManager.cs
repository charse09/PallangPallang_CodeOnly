using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    None,
    BumperAntennaSpark, // 범퍼 안테나 스파크 (Particle / VFX Graph)
    PipeSteam,          // 파이프 증기 (Particle / VFX Graph)
    HitImpact,          // 타격 이펙트
    Explosion,          // 폭발 이펙트
    FootstepDust,       // 발걸음 먼지
    JumpDust,
    LandingDust,        // 착지 먼지
    SlideSpark,         // 슬라이딩 이펙트
    PaperTransform,     // 종이 변신 이펙트
    WakeUp,
    LieDown,
    RollingSmoke,
    HandEffect,
    SlideSmoke,
}

public class EffectManager : ManagerBase<EffectManager>
{
    public static EffectManager Instance => instance;

    [Header("Effect Table Asset")]
    [SerializeField] private SO_EffectTable effectTable;

    private readonly Dictionary<EffectType, string> _typeToPoolKeyDict = new();
    private readonly Dictionary<string, string> _stringToPoolKeyDict = new(System.StringComparer.OrdinalIgnoreCase);

    private void Start()
    {
        InitializeEffectPools();
    }

    private void InitializeEffectPools()
    {
        if (PoolManager.instance == null || effectTable == null) return;

        foreach (var data in effectTable.EffectDataList)
        {
            if (data.prefab == null) continue;

            string resolvedKey = !string.IsNullOrEmpty(data.effectKey) ? data.effectKey : data.type.ToString();
            if (string.IsNullOrEmpty(resolvedKey) || resolvedKey == nameof(EffectType.None)) continue;

            string poolKey = $"Effect_{resolvedKey}";

            if (data.type != EffectType.None && !_typeToPoolKeyDict.ContainsKey(data.type))
            {
                _typeToPoolKeyDict.Add(data.type, poolKey);
                _stringToPoolKeyDict.TryAdd(data.type.ToString(), poolKey);
            }

            if (!string.IsNullOrEmpty(data.effectKey))
            {
                _stringToPoolKeyDict[data.effectKey] = poolKey;
            }

            PooledEffect pooledEffect = data.prefab.GetComponent<PooledEffect>();
            if (pooledEffect == null)
            {
                pooledEffect = data.prefab.AddComponent<PooledEffect>();
            }
            pooledEffect.poolKey = poolKey;

            PoolManager.instance.CreatePool(poolKey, data.prefab, Mathf.Max(1, data.initialPoolSize));
        }
    }

    /// <summary>
    /// [기존 호환] EffectType Enum으로 이펙트를 재생합니다.
    /// </summary>
    public GameObject PlayEffect(EffectType type, Vector3 position, Quaternion rotation, Transform parent = null, float duration = 0f)
    {
        if (type == EffectType.None || !_typeToPoolKeyDict.TryGetValue(type, out string poolKey))
        {
            Debug.LogWarning($"[EffectManager] 등록되지 않은 EffectType 입니다: {type}");
            return null;
        }

        return SpawnInternal(poolKey, position, rotation, parent, duration);
    }

    /// <summary>
    /// [신규 확장] 문자열 키(effectKey)로 이펙트를 재생합니다. (Animation Event 연동 최적화)
    /// </summary>
    public GameObject PlayEffect(string effectKey, Vector3 position, Quaternion rotation, Transform parent = null, float duration = 0f)
    {
        if (string.IsNullOrEmpty(effectKey) || !_stringToPoolKeyDict.TryGetValue(effectKey, out string poolKey))
        {
            Debug.LogWarning($"[EffectManager] 등록되지 않은 effectKey 입니다: '{effectKey}'");
            return null;
        }

        return SpawnInternal(poolKey, position, rotation, parent, duration);
    }

    private GameObject SpawnInternal(string poolKey, Vector3 position, Quaternion rotation, Transform parent, float duration)
    {
        if (PoolManager.instance == null) return null;

        GameObject obj = PoolManager.instance.SpawnFromPool(poolKey, position, rotation);
        if (obj != null)
        {
            if (parent != null)
            {
                obj.transform.SetParent(parent, true);
            }

            // duration 매개변수가 전달된 경우(0보다 큰 경우), PooledEffect의 타이머를 해당 값으로 재설정
            if (duration > 0f)
            {
                PooledEffect pooledEffect = obj.GetComponent<PooledEffect>();
                if (pooledEffect != null)
                {
                    pooledEffect.SetDurationAndStart(duration, false);
                }
            }
        }
        return obj;
    }

    private IEnumerator AutoStopRoutine(GameObject effectObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effectObj != null && effectObj.activeInHierarchy)
        {
            StopEffect(effectObj);
        }
    }

    /// <summary>
    /// 재생 중인 이펙트를 정지하고 풀로 반환합니다.
    /// </summary>
    public void StopEffect(GameObject effectObj)
    {
        if (effectObj == null) return;

        PooledEffect pooledEffect = effectObj.GetComponent<PooledEffect>();
        if (pooledEffect != null)
        {
            pooledEffect.ManualReturn();
        }
        else
        {
            effectObj.SetActive(false);
        }
    }
}
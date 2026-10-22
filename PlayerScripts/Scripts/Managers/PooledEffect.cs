using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class PooledEffect : MonoBehaviour, IPoolable
{
    [Header("Pool Key")]
    [Tooltip("EffectManager가 초기화 시 자동으로 주입하므로 비워두셔도 됩니다.")]
    public string poolKey;

    [Header("Duration Settings")]
    [Tooltip("체크 시 자동 반환 타이머가 꺼지며, 수동으로 멈출 때까지 유지됩니다.")]
    public bool isInfinite = false;

    [Tooltip("isInfinite가 꺼져있을 때 유지될 시간(초)")]
    public float customDuration = 2.0f;

    private ParticleSystem[] _particles;
    private VisualEffect[] _vfxs;
    private Coroutine _autoReturnCoroutine;
    private bool _isReturned = false;

    private void Awake()
    {
        // 하위 모든 파티클 및 VFX Graph 검색 (복합 이펙트 대응)
        _particles = GetComponentsInChildren<ParticleSystem>(true);
        _vfxs = GetComponentsInChildren<VisualEffect>(true);
    }

    /// <summary>
    /// IPoolable.New: PoolManager.SpawnFromPool() 시 자동 실행
    /// </summary>
    public void New()
    {
        _isReturned = false;

        // 1. ParticleSystem 초기화 및 재생
        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] != null)
                {
                    _particles[i].Clear(true);
                    _particles[i].Play(true);
                }
            }
        }

        // 2. VFX Graph 초기화 및 재생
        if (_vfxs != null)
        {
            for (int i = 0; i < _vfxs.Length; i++)
            {
                if (_vfxs[i] != null)
                {
                    _vfxs[i].Reinit();
                    _vfxs[i].Play();
                }
            }
        }

        // 3. 기존 코루틴 정리
        StopTimerCoroutine();

        // 4. 자동 반환 타이머 시작
        if (!isInfinite && customDuration > 0f)
        {
            _autoReturnCoroutine = StartCoroutine(AutoReturnRoutine(customDuration));
        }
    }

    /// <summary>
    /// IPoolable.Free: PoolManager.ReturnToPool() 시 자동 실행
    /// </summary>
    public void Free()
    {
        StopTimerCoroutine();

        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] != null)
                    _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (_vfxs != null)
        {
            for (int i = 0; i < _vfxs.Length; i++)
            {
                if (_vfxs[i] != null)
                    _vfxs[i].Stop();
            }
        }
    }

    /// <summary>
    /// 재생 지속 시간을 외부에서 일시적으로 재정의할 때 사용
    /// </summary>
    public void SetDurationAndStart(float duration, bool infinite)
    {
        isInfinite = infinite;
        customDuration = duration;

        StopTimerCoroutine();
        if (!isInfinite && customDuration > 0f)
        {
            _autoReturnCoroutine = StartCoroutine(AutoReturnRoutine(customDuration));
        }
    }

    /// <summary>
    /// 수동으로 풀에 반환할 때 호출
    /// </summary>
    public void ManualReturn()
    {
        if (_isReturned) return; // 중복 반환 방지
        _isReturned = true;

        StopTimerCoroutine();

        if (PoolManager.instance != null && !string.IsNullOrEmpty(poolKey))
        {
            PoolManager.instance.ReturnToPool(poolKey, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void StopTimerCoroutine()
    {
        if (_autoReturnCoroutine != null)
        {
            StopCoroutine(_autoReturnCoroutine);
            _autoReturnCoroutine = null;
        }
    }

    private IEnumerator AutoReturnRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ManualReturn();
    }
}
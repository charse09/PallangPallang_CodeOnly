using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    // [핵심 변경] 각 파티클별 고유 설정을 담을 데이터 구조체 정의
    [System.Serializable]
    public struct ParticleTimingData
    {
        [Tooltip("제어할 파티클 시스템입니다.")]
        public ParticleSystem particleSystem;

        [Tooltip("게임 시작 후 이 파티클이 최초로 작동하기 전까지 대기하는 지연 시간(초)입니다.")]
        public float initialDelay;

        [Tooltip("파티클이 활성화되어 유지되는 시간(초)입니다.")]
        public float activatedDuration;

        [Tooltip("파티클이 꺼져 있는 대기 시간(초)입니다.")]
        public float deactivatedDuration;
    }

    [Header("Particle Settings List")]
    [Tooltip("각 파티클마다 개별 타이밍 설정을 채워 넣어주세요.")]
    [SerializeField] private List<ParticleTimingData> particleSettings = new List<ParticleTimingData>();

    [Header("Global Options")]
    [Tooltip("체크 시 게임이 시작되자마자 모든 파티클의 타이머 루프를 가동합니다.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("파티클 정지 시, 이미 방출된 입자들까지 화면에서 즉시 지울지 여부입니다.\n체크 해제(False) 시 새로 방출만 멈추고 기존 입자는 자연스럽게 사라집니다.")]
    [SerializeField] private bool clearImmediatelyOnStop = false;

    // 각 파티클 루틴들을 개별적으로 멈추고 관리하기 위해 코루틴 리스트를 저장
    private List<Coroutine> _activeCoroutines = new List<Coroutine>();

    private void Start()
    {
        // 시작 시 모든 파티클을 초기 상태(정지)로 초기화
        InitAllParticles();

        if (playOnStart)
        {
            StartAllLoops();
        }
    }

    /// <summary>
    /// 등록된 모든 파티클의 개별 주기 루프를 동시에 기동합니다.
    /// </summary>
    public void StartAllLoops()
    {
        StopAllLoops(); // 실행 중인 루프가 있다면 안전하게 먼저 종료

        foreach (var setting in particleSettings)
        {
            if (setting.particleSystem != null)
            {
                // 각 파티클 데이터 세팅을 매개변수로 넘겨 독립된 코루틴 루프 생성
                Coroutine co = StartCoroutine(IndividualParticleLoopRoutine(setting));
                _activeCoroutines.Add(co);
            }
        }
    }

    /// <summary>
    /// 가동 중인 모든 파티클 루프를 정지하고 입자를 비활성화합니다.
    /// </summary>
    public void StopAllLoops()
    {
        foreach (var co in _activeCoroutines)
        {
            if (co != null) StopCoroutine(co);
        }
        _activeCoroutines.Clear();

        foreach (var setting in particleSettings)
        {
            SetSingleParticleActive(setting.particleSystem, false);
        }
    }

    private void InitAllParticles()
    {
        foreach (var setting in particleSettings)
        {
            SetSingleParticleActive(setting.particleSystem, false);
        }
    }

    /// <summary>
    /// [핵심 로직] 각 파티클이 자기만의 세팅값에 맞춰 독립적으로 동작하는 코루틴입니다.
    /// </summary>
    private IEnumerator IndividualParticleLoopRoutine(ParticleTimingData data)
    {
        // 1. 최초 플레이 시 해당 파티클의 고유 n초만큼 대기(지연)
        if (data.initialDelay > 0f)
        {
            SetSingleParticleActive(data.particleSystem, false);
            yield return new WaitForSeconds(data.initialDelay);
        }

        // 지연 시간이 끝난 후 이 파티클만의 무한 주기 루프 시작
        while (true)
        {
            // 2. 활성화 구간 (파티클 On)
            SetSingleParticleActive(data.particleSystem, true);
            yield return new WaitForSeconds(data.activatedDuration);

            // 3. 비활성화 구간 (파티클 Off)
            SetSingleParticleActive(data.particleSystem, false);
            yield return new WaitForSeconds(data.deactivatedDuration);
        }
    }

    /// <summary>
    /// 단일 파티클 시스템을 켜고 끄는 내부 기능 함수
    /// </summary>
    private void SetSingleParticleActive(ParticleSystem ps, bool isActive)
    {
        if (ps == null) return;

        if (isActive)
        {
            ps.Clear();
            ps.Play();
        }
        else
        {
            if (clearImmediatelyOnStop)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private void OnDisable()
    {
        StopAllLoops();
    }
}
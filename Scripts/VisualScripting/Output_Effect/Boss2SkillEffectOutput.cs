using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum BossSkillType
    {
        ChargeAttack,      // 기본 돌진 (ExecuteChargeAttackSkill)
        KoongSkill,        // 내려찍기 (ExecuteKoongSkill)
        FanSpin,           // 회전 밀치기 (ExecuteFanSpinSkill)
        WooferRush,        // 우퍼 돌진 (ExecuteWooferRush)
        Woofer1,           // 우퍼 충격파 (ExecuteWoofer1Skill)
        ThrowProp,         // 프롭 던지기 (ExecuteThrowPropSkill)
        ThrowHeavyProp,    // 중량 프롭 던지기 (ExecuteThrowHeavyPropSkill)
        BunshinCombo,      // 분신 머신건 (ExecuteBunshinAndStraightShotSkill)
        SpawnProps,        // 프롭 소환 (ExecuteSpawnPropsSkill)
        PullAndPush,       // 끌어당기고 밀치기 (ExecutePullAndPushSkill)
        Relocate,          // 위치 이동 (ExecuteRelocateRoutine)
        TwoPunch,          // [신규] 2연속 펀치 (ExecuteTwoPunchSkill)
        FlailSwing         // [신규] 철퇴 휘두르기 (ExecuteFlailSwingSkill)
    }

    [System.Serializable]
    public struct SkillEffectData
    {
        [Tooltip("연동할 보스 스킬 종류")]
        public BossSkillType skillType;

        [Header("Visual Effect (VFX)")]
        [Tooltip("생성할 이펙트 프리팹")]
        public GameObject vfxPrefab;

        [Tooltip("이펙트가 생성될 위치 Transform (비어있으면 보스 본인 위치)")]
        public Transform spawnTransform;

        [Tooltip("위치 미세조정 오프셋")]
        public Vector3 positionOffset;

        [Tooltip("회전 미세조정 오프셋 (X, Y, Z 각도)")]
        public Vector3 rotationOffset;

        [Tooltip("생성 시 보스/타겟의 회전 방향에 맞출지 여부")]
        public bool matchRotation;

        [Tooltip("생성된 이펙트가 타겟(spawnTransform 또는 보스)의 이동을 계속 따라다닐지 여부")]
        public bool followTarget;

        [Tooltip("이펙트 자동 파괴 시간 (0이면 파괴 안 함)")]
        public float vfxDestroyDelay;

        [Header("Framework Output Connections")]
        [Tooltip("스킬 발동 시 함께 실행할 Visual Scripting Output 노드 리스트")]
        public List<ProcessBase> targetOutputNodes;

        [Header("Sound Effect (SFX)")]
        [Tooltip("SoundDatabase에 등록된 사운드 ID (예: Boss_Punch, Boss_Koong)")]
        public string sfxSoundID;

        [Tooltip("3D 음향 적용 여부 (1 = 완전 3D 음향, 0 = 2D 음향)")]
        [Range(0f, 1f)]
        public float spatialBlend;
    }

    public class Boss2SkillEffectOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("연동할 Boss2Controller (비어있으면 자동으로 찾습니다)")]
        [SerializeField] private Boss2Controller bossController;

        [Header("Process Node Settings")]
        [Tooltip("ProcessBase 노드로 Execute() 호출 시 실행할 기본 스킬 이펙트")]
        public BossSkillType nodeDefaultSkillType = BossSkillType.KoongSkill;

        [Header("Skill Effect Mapping List")]
        [Tooltip("스킬별 이펙트 및 사운드 설정 데이터 리스트")]
        public List<SkillEffectData> skillEffects = new List<SkillEffectData>();

        private Dictionary<BossSkillType, SkillEffectData> _effectDict = new Dictionary<BossSkillType, SkillEffectData>();

        private void Awake()
        {
            if (bossController == null)
            {
                bossController = GetComponentInParent<Boss2Controller>() ?? GetComponent<Boss2Controller>();
            }

            RebuildDictionary();
        }

        public void RebuildDictionary()
        {
            _effectDict.Clear();
            foreach (var data in skillEffects)
            {
                if (!_effectDict.ContainsKey(data.skillType))
                {
                    _effectDict.Add(data.skillType, data);
                }
            }
        }

        public override void Execute()
        {
            IsOn = false;
            PlayEffect(nodeDefaultSkillType);
            IsOn = true;
        }

        /// <summary>
        /// 지정한 스킬 이펙트를 재생합니다. (customPosition 전달 시 해당 위치에 생성)
        /// </summary>
        public GameObject PlayEffect(BossSkillType skillType, Vector3? customPosition = null, bool triggerOutputs = true)
        {
            if (_effectDict.Count == 0 && skillEffects.Count > 0)
            {
                RebuildDictionary();
            }

            if (!_effectDict.TryGetValue(skillType, out SkillEffectData data))
            {
                return null;
            }

            Transform baseTransform = data.spawnTransform != null ? data.spawnTransform :
                                     (bossController != null ? bossController.transform : transform);

            Vector3 spawnPos = customPosition.HasValue
                ? customPosition.Value
                : baseTransform.position + (baseTransform.rotation * data.positionOffset);

            GameObject vfxInstance = null;

            // 1. Visual Effect (VFX) 생성
            if (data.vfxPrefab != null)
            {
                Quaternion prefabOriginalRot = data.vfxPrefab.transform.rotation;
                Quaternion customOffsetRot = Quaternion.Euler(data.rotationOffset);

                Quaternion spawnRot = data.matchRotation ? (baseTransform.rotation * prefabOriginalRot * customOffsetRot)
                                                         : (prefabOriginalRot * customOffsetRot);

                vfxInstance = Instantiate(data.vfxPrefab, spawnPos, spawnRot);

                if (data.followTarget && baseTransform != null)
                {
                    vfxInstance.transform.SetParent(baseTransform, true);
                }

                if (data.vfxDestroyDelay > 0f)
                {
                    Destroy(vfxInstance, data.vfxDestroyDelay);
                }
            }

            // 2. 연동된 프레임워크 Output 노드(ProcessBase) 실행
            if (triggerOutputs)
            {
                TriggerOutputNodes(skillType);
            }

            // 3. Sound Effect (SFX) 재생
            /*if (!string.IsNullOrEmpty(data.sfxSoundID) && SoundManager.instance != null)
            {
                SoundManager.instance.PlaySFXAtPosition(data.sfxSoundID, spawnPos, data.spatialBlend);
            }*/

            return vfxInstance;
        }

        /// <summary>
        /// 스킬 시작 시점 등 원하는 타이밍에 연동된 Output 노드들(RimLight 등)만 즉시 실행합니다.
        /// </summary>
        public void TriggerOutputNodes(BossSkillType skillType)
        {
            if (_effectDict.Count == 0 && skillEffects.Count > 0)
            {
                RebuildDictionary();
            }

            if (_effectDict.TryGetValue(skillType, out SkillEffectData data))
            {
                if (data.targetOutputNodes != null && data.targetOutputNodes.Count > 0)
                {
                    foreach (var outputNode in data.targetOutputNodes)
                    {
                        if (outputNode != null)
                        {
                            outputNode.Execute();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 스킬 종료 시 연동된 Output 노드(BossRimLightOutput 등)를 정지시킵니다.
        /// </summary>
        public void StopEffect(BossSkillType skillType)
        {
            if (_effectDict.Count == 0 && skillEffects.Count > 0)
            {
                RebuildDictionary();
            }

            if (_effectDict.TryGetValue(skillType, out SkillEffectData data))
            {
                if (data.targetOutputNodes != null && data.targetOutputNodes.Count > 0)
                {
                    foreach (var outputNode in data.targetOutputNodes)
                    {
                        if (outputNode is BossRimLightOutput rimLightOutput)
                        {
                            rimLightOutput.TurnOff();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 지정한 스킬의 설정된 발생 위치(spawnTransform + positionOffset)를 가져옵니다.
        /// </summary>
        public Vector3 GetSpawnPosition(BossSkillType skillType)
        {
            if (_effectDict.Count == 0 && skillEffects.Count > 0)
            {
                RebuildDictionary();
            }

            if (_effectDict.TryGetValue(skillType, out SkillEffectData data))
            {
                Transform baseTransform = data.spawnTransform != null ? data.spawnTransform :
                                         (bossController != null ? bossController.transform : transform);

                return baseTransform.position + (baseTransform.rotation * data.positionOffset);
            }

            return bossController != null ? bossController.transform.position : transform.position;
        }
    }
}
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class LightingControl : ProcessBase
    {
        // 빌드 시에는 메모리 절약을 위해 변수 자체를 숨깁니다.
#if UNITY_EDITOR
        [Header("Debug")]
        [Tooltip("체크하면 상세 로그를 화면과 콘솔에 띄웁니다.")]
        [SerializeField] private bool showDebugLog = true;
#endif

        [Header("Target Settings")]
        [SerializeField] private Light targetLight;

        [Header("Control Options")]
        [Tooltip("유지 시간\n-1: 영구 변경\n양수: 해당 시간 유지 후 원래대로 복구 (깜빡임 효과 등)")]
        [SerializeField] private float duration = -1f;

        [Space(10)]
        [SerializeField] private bool disable = true;
        [SerializeField] private bool isToggle = false;

        [Space(5)]
        [SerializeField] private bool controlIntensity = false;
        [SerializeField] private float setIntensity = 1.0f;

        [Space(5)]
        [SerializeField] private bool controlColor = false;
        [SerializeField] private Color setColor = Color.white;

        // 백업 변수
        private float _originIntensity;
        private Color _originColor;
        private Coroutine _activeCoroutine;

        private void Awake()
        {
            if (targetLight == null)
                targetLight = GetComponent<Light>();
        }

        public override void Execute()
        {
            // ★ [VisualLogger 적용] this.Log로 아주 깔끔하게 호출합니다.
            // showDebugLog 변수는 에디터에서만 존재하므로, #if로 감싸서 전달하거나 
            // 에디터가 아닐 땐 true/false 의미가 없으므로(함수가 삭제됨) 그냥 두어도 됩니다.

#if UNITY_EDITOR
            this.Log("LightingControl 실행 시작", showDebugLog);
#endif

            if (targetLight == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 오류: 타겟 Light가 없습니다!");
                return;
            }

            if (_activeCoroutine != null)
            {
#if UNITY_EDITOR
                this.Log("기존 코루틴 중단됨", showDebugLog);
#endif
                StopCoroutine(_activeCoroutine);
            }

            if (duration == -1f)
            {
#if UNITY_EDITOR
                this.Log("모드: 영구 변경", showDebugLog);
#endif
                ApplyLighting();
                IsOn = true;
            }
            else if (duration > 0f)
            {
#if UNITY_EDITOR
                this.Log($"모드: 임시 변경 ({duration}초)", showDebugLog);
#endif
                _activeCoroutine = StartCoroutine(Co_TemporaryLightChange());
                IsOn = true;
            }
        }

        private void ApplyLighting()
        {

#if UNITY_EDITOR
            this.Log($" > Enabled: {targetLight.enabled} -> Disable: {disable}");
#endif
            targetLight.enabled = !disable;


            if (controlIntensity)
            {
#if UNITY_EDITOR
                this.Log($" > Intensity: {targetLight.intensity} -> {setIntensity}", showDebugLog);
#endif
                targetLight.intensity = setIntensity;
            }

            if (controlColor)
            {
#if UNITY_EDITOR
                this.Log($" > Color: {targetLight.color} -> {setColor}", showDebugLog);
#endif
                targetLight.color = setColor;
            }
        }

        private IEnumerator Co_TemporaryLightChange()
        {
            _originIntensity = targetLight.intensity;
            _originColor = targetLight.color;

#if UNITY_EDITOR
            this.Log("(백업 완료)", showDebugLog);
#endif

            ApplyLighting();
            yield return new WaitForSeconds(duration);

#if UNITY_EDITOR
            this.Log("원본 복구 시작", showDebugLog);
#endif

            //if (controlEnabled) targetLight.enabled = _originEnabled;
            if (controlIntensity) targetLight.intensity = _originIntensity;
            if (controlColor) targetLight.color = _originColor;

#if UNITY_EDITOR
            this.Log("복구 완료", showDebugLog);
#endif
            _activeCoroutine = null;
        }
    }
}
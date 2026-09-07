using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetObjectScriptsEnabledOutput : ProcessBase
    {
        public enum SetMode
        {
            Enable,
            Disable,
            Toggle
        }

        [Serializable]
        public class ScriptToggleData
        {
            [Tooltip("켜고 끌 스크립트")]
            public MonoBehaviour targetScript;

            [Tooltip("이 스크립트에 적용할 동작")]
            public SetMode setMode = SetMode.Toggle;
        }

        [Header("Target Object")]
        [Tooltip("여러 스크립트가 붙어 있는 대상 오브젝트")]
        [SerializeField] private GameObject targetObject;

        [Header("Script Controls")]
        [Tooltip("대상 오브젝트에 붙은 스크립트 중 제어할 것들")]
        [SerializeField] private List<ScriptToggleData> scriptControls = new List<ScriptToggleData>();

        public override void Execute()
        {
            if (targetObject == null)
            {
                Debug.LogWarning($"{gameObject.name}: Target Object가 비어 있습니다.");
                return;
            }

            if (scriptControls == null || scriptControls.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name}: 제어할 Script Controls가 없습니다.");
                return;
            }

            foreach (ScriptToggleData data in scriptControls)
            {
                if (data == null || data.targetScript == null) continue;

                switch (data.setMode)
                {
                    case SetMode.Enable:
                        data.targetScript.enabled = true;
                        break;

                    case SetMode.Disable:
                        data.targetScript.enabled = false;
                        break;

                    case SetMode.Toggle:
                        data.targetScript.enabled = !data.targetScript.enabled;
                        break;
                }
            }

            IsOn = true;
        }
    }
}
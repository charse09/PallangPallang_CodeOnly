using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ToggleOutline : ProcessBase
    {
        [Header("아웃라인 대상 및 재질")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material outlineMaterial;

        [Header("아웃라인 설정")]
        [SerializeField] private bool turnOn = true;
        [SerializeField] private bool useCustomSettings = false;

        [Header("커스텀 아웃라인 옵션")]
        [ColorUsage(true, true)]
        [SerializeField] private Color outlineColor = Color.white;

        [Range(0f, 0.1f)]
        [SerializeField] private float outlineThickness = 0.001f;

        private readonly string colorProp = "_OutlineColor";
        private readonly string thicknessProp = "_OutlineThickness";

        // 생성한 아웃라인 머티리얼 재사용 및 메모리 관리를 위한 캐싱 변수
        private Material _instantiatedOutlineMat;

        public override void Execute()
        {
            if (targetRenderer == null || outlineMaterial == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 타겟 렌더러 또는 아웃라인 재질이 지정되지 않았습니다!");
                IsOn = true;
                return;
            }

            // sharedMaterials를 사용하여 기존 원본 머티리얼이 (Instance)로 변환되는 것을 방지
            Material[] sharedMats = targetRenderer.sharedMaterials;
            List<Material> materialList = new List<Material>(sharedMats);

            // 이미 생성한 캐시 머티리얼 또는 이름 기반 검사
            int outlineIndex = materialList.FindIndex(m => m != null && (m == _instantiatedOutlineMat || m.name.StartsWith(outlineMaterial.name)));

            if (turnOn)
            {
                if (outlineIndex == -1)
                {
                    // 아웃라인 머티리얼이 없을 때만 1회 인스턴스화하여 캐싱
                    if (_instantiatedOutlineMat == null)
                    {
                        _instantiatedOutlineMat = Instantiate(outlineMaterial);
                    }

                    materialList.Add(_instantiatedOutlineMat);
                    outlineIndex = materialList.Count - 1;
                }

                if (useCustomSettings && _instantiatedOutlineMat != null)
                {
                    _instantiatedOutlineMat.SetColor(colorProp, outlineColor);
                    _instantiatedOutlineMat.SetFloat(thicknessProp, outlineThickness);
                }

                targetRenderer.sharedMaterials = materialList.ToArray();
                Debug.Log($"<color=lime>[Output]</color> '{targetRenderer.gameObject.name}' HDR 아웃라인 켜짐");
            }
            else
            {
                if (outlineIndex != -1)
                {
                    materialList.RemoveAt(outlineIndex);
                    targetRenderer.sharedMaterials = materialList.ToArray();
                    Debug.Log($"<color=lime>[Output]</color> '{targetRenderer.gameObject.name}' 아웃라인 꺼짐");
                }
            }

            IsOn = true;
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지: 동적으로 생성된 머티리얼 명시적 파괴
            if (_instantiatedOutlineMat != null)
            {
                Destroy(_instantiatedOutlineMat);
            }
        }
    }
}
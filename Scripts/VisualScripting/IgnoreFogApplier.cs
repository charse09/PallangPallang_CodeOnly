using UnityEngine;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 부착된 오브젝트 및 자식 오브젝트의 모든 TextMeshPro와 Renderer가 씬의 안개(Fog) 영향을 받지 않도록 처리하는 유틸리티입니다.
    /// </summary>
    [ExecuteAlways]
    public class IgnoreFogApplier : MonoBehaviour
    {
        [Tooltip("체크 시 씬의 안개(Fog) 효과를 무시하고 항상 선명하게 표시합니다.")]
        [SerializeField] private bool ignoreFog = true;

        [Tooltip("자식 오브젝트에 있는 텍스트/렌더러까지 모두 포함하여 적용할지 여부")]
        [SerializeField] private bool includeChildren = true;

        private void OnEnable()
        {
            ApplyFogSettings();
        }

        private void Start()
        {
            ApplyFogSettings();
        }

        private void OnValidate()
        {
            ApplyFogSettings();
        }

        public void SetIgnoreFog(bool enable)
        {
            ignoreFog = enable;
            ApplyFogSettings();
        }

        [ContextMenu("Apply Fog Settings Now")]
        public void ApplyFogSettings()
        {
            // 1. TextMeshPro (3D 및 UI) 처리
            TMP_Text[] tmpTexts = includeChildren
                ? GetComponentsInChildren<TMP_Text>(true)
                : GetComponents<TMP_Text>();

            foreach (var text in tmpTexts)
            {
                if (text == null) continue;

                Material mat = Application.isPlaying ? text.fontMaterial : text.fontSharedMaterial;
                if (mat != null)
                {
                    ApplyFogToMaterial(mat);
                }
            }

            // 2. 일반 Renderer (SpriteRenderer, MeshRenderer 등) 처리
            Renderer[] renderers = includeChildren
                ? GetComponentsInChildren<Renderer>(true)
                : GetComponents<Renderer>();

            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                Material[] mats = Application.isPlaying ? rend.materials : rend.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat != null)
                    {
                        ApplyFogToMaterial(mat);
                    }
                }
            }
        }

        private void ApplyFogToMaterial(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_IgnoreFog"))
            {
                mat.SetFloat("_IgnoreFog", ignoreFog ? 1f : 0f);
            }

            if (ignoreFog)
            {
                // 안개 연산 비활성화
                mat.EnableKeyword("_IGNOREFOG_ON");
                mat.DisableKeyword("FOG_LINEAR");
                mat.DisableKeyword("FOG_EXP");
                mat.DisableKeyword("FOG_EXP2");
            }
            else
            {
                // 안개 연산 복구
                mat.DisableKeyword("_IGNOREFOG_ON");
                if (RenderSettings.fog)
                {
                    switch (RenderSettings.fogMode)
                    {
                        case FogMode.Linear: mat.EnableKeyword("FOG_LINEAR"); break;
                        case FogMode.Exponential: mat.EnableKeyword("FOG_EXP"); break;
                        case FogMode.ExponentialSquared: mat.EnableKeyword("FOG_EXP2"); break;
                    }
                }
            }
        }
    }
}
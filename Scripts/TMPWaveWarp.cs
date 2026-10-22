using UnityEngine;
using TMPro;

namespace _Project.Scripts.VisualScripting.UI
{
    /// <summary>
    /// 프리미어 프로의 '파도 비틀기(Wave Warp)' 효과를 TextMeshPro 텍스트에 실시간으로 적용합니다.
    /// 글자 단위 또는 연속적인 공간 좌표를 기준으로 부드럽게 꿀렁이는 파동을 생성합니다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TMPWaveWarp : MonoBehaviour
    {
        [Header("1. Wave Properties (프리미어 프로 파도 설정)")]
        [Tooltip("파도 높이 / 진폭 (Wave Height) - 글자가 위아래로 흔들리는 최대 폭")]
        [SerializeField] private float waveHeight = 10f;

        [Tooltip("파도 폭 / 파장 (Wave Width) - 값이 작을수록 널찍한 파도, 클수록 촘촘한 파도")]
        [SerializeField] private float waveWidth = 0.05f;

        [Tooltip("파도가 지나가는 속도 (Speed)")]
        [SerializeField] private float speed = 5f;

        [Header("2. Direction & Mode")]
        [Tooltip("체크 시 상하(Y축) 파도, 해제 시 좌우(X축) 파도")]
        [SerializeField] private bool verticalWave = true;

        [Tooltip("체크 시 글자 인덱스가 아닌 '글자의 실제 위치 좌표(X/Y)'를 기준으로 매끄럽게 휘어집니다.")]
        [SerializeField] private bool continuousSpatialWave = true;

        [Tooltip("게임 일시정지 중에도 파도가 칠지 여부")]
        [SerializeField] private bool useUnscaledTime = false;

        private TMP_Text _tmpText;

        private void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (_tmpText == null) return;

            // 텍스트 메쉬 데이터 강제 갱신
            _tmpText.ForceMeshUpdate();

            TMP_TextInfo textInfo = _tmpText.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount == 0) return;

            float time = (useUnscaledTime ? Time.unscaledTime : Time.time) * speed;

            // 각 글자의 4개 정점을 파동 공식에 맞춰 변형
            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // 보이지 않는 문자(공백 등) 제외
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                // 해당 글자의 사각형 꼭짓점 4개 변형
                for (int j = 0; j < 4; j++)
                {
                    Vector3 orig = sourceVertices[vertexIndex + j];

                    // 파도 위상 계산 (글자 위치 기반 or 인덱스 기반)
                    float pos = continuousSpatialWave ? (verticalWave ? orig.x : orig.y) : i * 20f;
                    float waveOffset = Mathf.Sin(pos * waveWidth + time) * waveHeight;

                    if (verticalWave)
                    {
                        orig.y += waveOffset;
                    }
                    else
                    {
                        orig.x += waveOffset;
                    }

                    sourceVertices[vertexIndex + j] = orig;
                }
            }

            // 수정된 정점 데이터를 메쉬에 적용
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].mesh == null) continue;
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                _tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}
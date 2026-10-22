using UnityEngine;

public enum BossFaceState { Idle, BeforeMad, Worried, Mad, Destroied, QuestionMark, Excited }

public class BossFaceController : MonoBehaviour
{
    [SerializeField] private Renderer faceRenderer;
    [SerializeField] private int targetMaterialIndex = 1; // 2번째 머티리얼이므로 인덱스는 1 (0부터 시작)

    [Header("Face Textures")]
    [SerializeField] private Texture2D idleTexture;
    [SerializeField] private Texture2D beforeMadTexture;
    [SerializeField] private Texture2D worriedTexture;
    [SerializeField] private Texture2D madTexture;
    [SerializeField] private Texture2D destroiedTexture;
    [SerializeField] private Texture2D questionMarkTexture;
    [SerializeField] private Texture2D excitedTexture;

    public void ChangeFace(BossFaceState state)
    {
        if (faceRenderer == null) return;

        Texture2D targetTexture = state switch
        {
            BossFaceState.Idle => idleTexture,
            BossFaceState.BeforeMad => beforeMadTexture,
            BossFaceState.Worried => worriedTexture,
            BossFaceState.Mad => madTexture,
            BossFaceState.Destroied => destroiedTexture,
            BossFaceState.QuestionMark => questionMarkTexture,
            BossFaceState.Excited => excitedTexture,
            _ => idleTexture
        };

        // 1. 현재 메쉬의 모든 머티리얼 배열을 가져옵니다.
        Material[] mats = faceRenderer.materials;

        // 2. 범위를 벗어나지 않는지 확인 후 특정 인덱스의 머티리얼을 수정합니다.
        if (targetMaterialIndex >= 0 && targetMaterialIndex < mats.Length)
        {
            // _BaseMap(또는 _MainTex) 프로퍼티를 변경합니다.
            mats[targetMaterialIndex].SetTexture("_BaseMap", targetTexture);

            // 3. 수정된 배열을 다시 Renderer에 할당합니다.
            faceRenderer.materials = mats;
        }
    }
}
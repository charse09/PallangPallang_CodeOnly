using UnityEngine;

public class PaperCharacterFlip : MonoBehaviour
{
    [Header("텍스처 설정")]
    public Texture2D frontTexture; // 앞면 텍스처
    public Texture2D backTexture;  // 뒷면 텍스처

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (frontTexture == null || backTexture == null) return;

        // 1. 카메라와 캐릭터의 방향 벡터 계산
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 charForward = transform.forward;

        // 2. 내적(Dot Product) 구하기
        // 값이 0보다 크면 캐릭터가 카메라와 같은 방향을 보고 있음 (뒷모습)
        float dot = Vector3.Dot(charForward, camForward);

        // 3. 상태에 따른 텍스처 선택
        Texture2D targetTexture = (dot > 0) ? backTexture : frontTexture;

        // 4. MaterialPropertyBlock으로 텍스처 교체 (URP/Lit의 기본 텍스처 변수명은 _BaseMap)
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(BaseMapID, targetTexture);
        _renderer.SetPropertyBlock(_propBlock);
    }
}
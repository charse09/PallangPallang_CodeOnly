using UnityEngine;

public class PlayerFaceMaterial : MonoBehaviour
{
    [SerializeField] private GameObject targetFace;
    [SerializeField] private SkinnedMeshRenderer smr;

    [Header("Face Textures (Texture2D)")]
    [SerializeField] private Texture2D normalFace;
    [SerializeField] private Texture2D surprizeFace;
    [SerializeField] private Texture2D kawaiiFace;
    [SerializeField] private Texture2D stareFace;
    [SerializeField] private Texture2D sadFace;
    [SerializeField] private Texture2D mouthSmileFace;
    [SerializeField] private Texture2D embarassedFace;
    [SerializeField] private Texture2D smileFace;

    public Texture2D NormalFace => normalFace;
    public Texture2D SurprizeFace => surprizeFace;
    public Texture2D KawaiiFace => kawaiiFace;
    public Texture2D StareFace => stareFace;
    public Texture2D SadFace => sadFace;
    public Texture2D MouthSmileFace => mouthSmileFace;
    public Texture2D EmbarassedFace => embarassedFace;
    public Texture2D SmileFace => smileFace;

    [Header("Shader Settings")]
    [Tooltip("셰이더의 텍스처 파라미터 이름입니다. (기본 3D: _MainTex / URP: _BaseMap)")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    // 성능을 위해 머티리얼을 캐싱해 둘 변수
    private Material faceMaterial;

    public Texture2D CurrentFaceTexture { get; private set; }

    private void Start()
    {
        if (smr == null && targetFace != null)
        {
            smr = targetFace.GetComponent<SkinnedMeshRenderer>();
        }

        // 시작할 때 SkinnedMeshRenderer에 할당된 머티리얼을 가져와 저장해 둡니다.
        if (smr != null)
        {
            faceMaterial = smr.material;
        }

        SetNormalFace();
    }

    public void SetNormalFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, normalFace);
        CurrentFaceTexture = normalFace;
    }

    public void SetSurprizeFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, surprizeFace);
        CurrentFaceTexture = surprizeFace;
    }

    public void SetKawaiiFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, kawaiiFace);
        CurrentFaceTexture = kawaiiFace;
    }

    public void SetSmileFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, smileFace);
        CurrentFaceTexture = smileFace;
    }

    public void SetStareFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, stareFace);
        CurrentFaceTexture = stareFace;
    }

    public void SetSadFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, sadFace);
        CurrentFaceTexture = sadFace;
    }

    public void SetMouthSmileFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, mouthSmileFace);
        CurrentFaceTexture = mouthSmileFace;
    }

    public void SetEmbarassedFace()
    {
        if (faceMaterial != null) faceMaterial.SetTexture(texturePropertyName, embarassedFace);
        CurrentFaceTexture = embarassedFace;
    }
}
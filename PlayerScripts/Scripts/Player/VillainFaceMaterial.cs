using UnityEngine;

public class VillainFaceMaterial : MonoBehaviour
{
    [SerializeField] private GameObject targetFace;
    [SerializeField] private SkinnedMeshRenderer renderer;

    [Header("Face Materials")]
    [SerializeField] private Material normalFace;
    [SerializeField] private Material hurtFace;
    [SerializeField] private Material jigglingFace;
    [SerializeField] private Material talkFace;
    [SerializeField] private Material sadFace;
    [SerializeField] private Material embarassedFace;
    [SerializeField] private Material smileFace;

    private void Awake()
    {
        if (renderer == null && targetFace != null)
        {
            renderer = targetFace.GetComponent<SkinnedMeshRenderer>();
        }

        SetNormalFace();
    }

    public void SetNormalFace()
    {
        renderer.material = normalFace;
    }

    public void SetHurtFace()
    {
        renderer.material = hurtFace;
    }

    public void SetJigglingFace()
    {
        renderer.material = jigglingFace;
    }
    public void SetTalkFace()
    {
        renderer.material = talkFace;
    }
    public void SetSadFace()
    {
        renderer.material = sadFace;
    }
    public void SetEmbarassedFace()
    {
        renderer.material = embarassedFace;
    }

    public void SetSmileFace()
    {
        renderer.material = smileFace;
    }

}

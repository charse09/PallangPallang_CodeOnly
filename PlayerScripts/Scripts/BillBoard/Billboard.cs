using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCamTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCamTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (mainCamTransform == null)
        {
            if (Camera.main != null) mainCamTransform = Camera.main.transform;
            else return;
        }

        // 카메라의 회전값을 그대로 추종하여 정면을 바라봄
        transform.rotation = mainCamTransform.rotation;
    }
}
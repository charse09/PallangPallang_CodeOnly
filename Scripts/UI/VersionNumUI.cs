using UnityEngine;
using TMPro; // TextMeshPro를 사용할 경우

public class VersionNumUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private string prefix = "v"; // 버전 앞에 붙일 문자열 (예: v1.0.0)

    private void Awake()
    {
        // <TextMeshProUGUI> 제네릭 타입을 명시해주어야 합니다.
        if (versionText == null)
            versionText = GetComponent<TextMeshProUGUI>();

        if (versionText != null)
        {
            versionText.text = $"{prefix}{Application.version}";
        }
    }
}
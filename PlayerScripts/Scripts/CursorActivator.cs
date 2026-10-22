using UnityEngine;

/// <summary>
/// 특정 UI 오브젝트나 팝업에 부착하여, 해당 오브젝트가 켜질(Active) 때
/// 마우스 커서를 자동으로 표시하고 꺼질(Inactive) 때 커서를 다시 잠그는 컴포넌트입니다.
/// 인스펙터나 코드 작업 없이 단순히 컴포넌트 추가만으로 커서 활성화 상황을 확장할 수 있습니다.
/// </summary>
[DisallowMultipleComponent]
public class CursorActivator : MonoBehaviour
{
    private void OnEnable()
    {
        UpdateCursorState.RequestCursor(this);
    }

    private void OnDisable()
    {
        UpdateCursorState.ReleaseCursor(this);
    }

    private void OnDestroy()
    {
        UpdateCursorState.ReleaseCursor(this);
    }
}

using UnityEngine;

// 상호작용 가능한 모든 스크립트가 가져야 할 공통 기능
public interface IInteractable
{
    // 이 함수를 무조건 만들어야 한다는 규칙입니다.
    void Interact();
}
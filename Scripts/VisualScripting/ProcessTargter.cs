using _Project.Scripts.VisualScripting;
using UnityEngine;

public abstract class ProcessTargeter : ProcessBase
{
    // 타겟을 가져오는 공통 메서드 (인터페이스 역할)
    protected GameObject objTarget;
    public GameObject GetTarget() { return objTarget; }
    public void ResetTarget() { objTarget = null; }
}

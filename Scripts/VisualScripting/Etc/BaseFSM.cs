using UnityEngine;

// 상태 정의: Hit -> Finish로 변경
public enum UnitState
{
    Idle,
    Move,
    Patrol,
    Finish // [변경] 피격 후 소멸(처형) 상태
}

public abstract class BaseFSM : MonoBehaviour
{
    [SerializeField]
    protected UnitState currentState = UnitState.Idle;

    protected virtual void Start()
    {
        OnEnterState(currentState);
    }

    protected virtual void Update()
    {
        OnUpdateState(currentState);
    }

    public void ChangeState(UnitState newState)
    {
        if (currentState == newState) return;

        OnExitState(currentState);

        UnitState previousState = currentState;
        currentState = newState;

        OnEnterState(currentState);

        Debug.Log($"[FSM] State Changed: {previousState} -> {currentState}");
    }

    // ---------------------------------------------------------
    // 상태 분기 처리 (Hit -> Finish로 교체)
    // ---------------------------------------------------------

    protected virtual void OnEnterState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: Enter_Idle(); break;
            case UnitState.Move: Enter_Move(); break;
            case UnitState.Patrol: Enter_Patrol(); break;
            case UnitState.Finish: Enter_Finish(); break; // [변경]
        }
    }

    protected virtual void OnExitState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: Exit_Idle(); break;
            case UnitState.Move: Exit_Move(); break;
            case UnitState.Patrol: Exit_Patrol(); break;
            case UnitState.Finish: Exit_Finish(); break; // [변경]
        }
    }

    protected virtual void OnUpdateState(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: Update_Idle(); break;
            case UnitState.Move: Update_Move(); break;
            case UnitState.Patrol: Update_Patrol(); break;
            case UnitState.Finish: Update_Finish(); break; // [변경]
        }
    }

    // 가상 함수 정의
    protected virtual void Enter_Idle() { }
    protected virtual void Update_Idle() { }
    protected virtual void Exit_Idle() { }

    protected virtual void Enter_Move() { }
    protected virtual void Update_Move() { }
    protected virtual void Exit_Move() { }

    protected virtual void Enter_Patrol() { }
    protected virtual void Update_Patrol() { }
    protected virtual void Exit_Patrol() { }

    // [변경] Hit -> Finish
    protected virtual void Enter_Finish() { }
    protected virtual void Update_Finish() { }
    protected virtual void Exit_Finish() { }
}
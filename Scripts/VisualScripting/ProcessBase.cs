using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public abstract class ProcessBase : MonoBehaviour
    {
        [SerializeField] private bool isOn;

        public bool IsOn
        {
            get => isOn;
            protected set => isOn = value;
        }

        public virtual void Reset()
        {
            isOn = false;
        }

        public void SetIsOn(bool value)
        {
            IsOn = value;
        }
        // 컴포넌트가 동작할 기능 함수
        public abstract void Execute();
    
        // 입력된 프로세스의 동작 여부를 Not 연산을 적용해 반환
        protected virtual bool CheckInputProcessStatus(ProcessData processData)
        {
            return (processData.isNot) ? !processData.process.IsOn : processData.process.IsOn;
        }

#if UNITY_EDITOR
        [ContextMenu("강제 실행 (테스트)")]
        public void ForceExecute()
        {
            Debug.Log($"[{gameObject.name}] 강제 실행 명령을 받았습니다.");
            Execute();
        }
#endif
    }
}

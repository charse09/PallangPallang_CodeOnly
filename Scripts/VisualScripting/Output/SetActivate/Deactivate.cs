//using UnityEngine;



//namespace _Project.Scripts.VisualScripting

//{

//    [AddComponentMenu("Visual Scripting/Actions/Deactivate (Self)")]



//    public class Deactivate : ProcessBase

//    {

//        /*

//         * 컨디셔널의 업데이트에서 진입시 오브젝트 비활성화되어 정상작동되지않음.

//        */

//        //이 트리거는 객체가 비활성화 시점에 작동해야하므로, 객체 내부에서 Execute()되도록 처리하는 것이 인터페이스 변경.

//        [Header("자기자신을 가르킴(반드시 오브젝트에 등록)")]

//        [SerializeField] private GameObject obj;



//        private void Awake()

//        {



//            //if (!obj) 

//            obj = this.gameObject;

//            Debug.Log($"{this.GetType()}.Start({obj.name})");

//            obj.SetActive(true);

//        }



//        public override void Execute()

//        {

//            IsOn = true;

//            Debug.Log($"{this.GetType()}.Execute({obj.name})");

//            obj.SetActive(false);

//            

//        }



//        public void OnDisable()

//        {

//            if (obj.GetInstanceID() == this.gameObject.GetInstanceID()) Execute();

//        }

//    }

//}

using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [AddComponentMenu("Visual Scripting/Actions/Deactivate (Self)")]
    public class Deactivate : ProcessBase
    {
        [Header("비활성화할 대상 (비워둘 시 자기 자신)")]
        [SerializeField] private GameObject obj;

        private bool _isProcessing = false;

        private void Awake()
        {
            // 인스펙터에 할당된 것이 없을 때만 자기 자신으로 설정
            if (obj == null)
            {
                obj = this.gameObject;
            }
        }

        public override void Execute()
        {
            // 이미 처리 중이거나 오브젝트가 이미 꺼져있다면 중복 실행 방지
            if (_isProcessing || obj == null || !obj.activeSelf)
            {
                IsOn = true;
                return;
            }

            try
            {
                _isProcessing = true;
                IsOn = false;

#if UNITY_EDITOR
                Debug.Log($"[{GetType().Name}] {obj.name} 비활성화");
#endif

                obj.SetActive(false);
            }
            finally
            {
                _isProcessing = false;
                IsOn = true; // 다음 시퀀스로 넘어갈 수 있도록 플래그 해제
            }
        }

        private void OnDisable()
        {
            // 외부에서 오브젝트를 껐을 때 시퀀스 상태(IsOn) 동기화가 필요하다면 플래그만 세팅
            if (!_isProcessing)
            {
                IsOn = true;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _isProcessing = false;
            IsOn = false;
        }
    }
}
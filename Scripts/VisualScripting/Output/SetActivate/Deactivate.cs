using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [AddComponentMenu("Visual Scripting/Actions/Deactivate (Self)")]
   
    public class Deactivate : ProcessBase
    {
        /*
         * 컨디셔널의 업데이트에서 진입시 오브젝트 비활성화되어 정상작동되지않음.
        */
        //이 트리거는 객체가 비활성화 시점에 작동해야하므로, 객체 내부에서 Execute()되도록 처리하는 것이 인터페이스 변경.
        [Header("자기자신을 가르킴(반드시 오브젝트에 등록)")]
        [SerializeField] private GameObject obj;

        private void Awake()
        {
            //if (!obj) 
                obj = this.gameObject;
            Debug.Log($"{this.GetType()}.Start({obj.name})");
            obj.SetActive(true);
        }

        public override void Execute()
        {
            IsOn = true;
            Debug.Log($"{this.GetType()}.Execute({obj.name})");
            obj.SetActive(false);
        }

        public void OnDisable()
        {
            if (obj.GetInstanceID() == this.gameObject.GetInstanceID()) Execute();
        }
    }
}
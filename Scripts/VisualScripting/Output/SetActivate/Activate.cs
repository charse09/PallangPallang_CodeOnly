//using UnityEngine;

//namespace _Project.Scripts.VisualScripting
//{
//    public class Activate : ProcessBase
//    {
//        [SerializeField] private GameObject obj;

//        public void Awake()
//        {
//            if(!obj) obj = this.gameObject;
//            Debug.Log($"{this.GetType()}.Start({obj.name})");
//        }
//        public override void Execute()
//        {
//            IsOn = true;
//            obj.SetActive(true);
//            Debug.Log($"{this.GetType()}.Execute({obj.name})");
//        }
//    }
//}

using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Activate : ProcessBase
    {
        [SerializeField] private GameObject obj;

        private void Awake()
        {
            if (!obj) obj = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (obj != null)
            {
                obj.SetActive(true);
#if UNITY_EDITOR
                Debug.Log($"[{GetType().Name}] {obj.name} È°¼ºÈ­");
#endif
            }

            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}

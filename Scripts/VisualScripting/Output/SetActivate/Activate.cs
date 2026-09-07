using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Activate : ProcessBase
    {
        [SerializeField] private GameObject obj;

        public void Awake()
        {
            if(!obj) obj = this.gameObject;
            Debug.Log($"{this.GetType()}.Start({obj.name})");
        }
        public override void Execute()
        {
            IsOn = true;
            obj.SetActive(true);
            Debug.Log($"{this.GetType()}.Execute({obj.name})");
        }
    }
}

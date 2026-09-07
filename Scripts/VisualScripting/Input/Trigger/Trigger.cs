using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Trigger : ProcessBase
    {
        [SerializeField] private string selectedTag;
        [SerializeField] GameObject objTarget;

        public GameObject GetTarget() { return objTarget; }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                IsOn = true;
                objTarget = other.gameObject;
#if UNITY_EDITOR
                this.Log($"진입 감지: {other.name}");
#endif
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                IsOn = true;
#if UNITY_EDITOR
                this.Log($"이탈 감지: {other.name}");
#endif
            }
            if (!objTarget && other.gameObject.GetInstanceID() == objTarget.GetInstanceID())
                objTarget = null;
        }

        public override void Execute()
        {
            IsOn = !IsOn;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
	public class TriggerTimmer : ProcessBase
	{
        [SerializeField] public Color gizmoColor = Color.blue;
        [SerializeField] private string selectedTag;
        [SerializeField] GameObject objTarget;

        public GameObject GetTarget() { return objTarget; }

        [SerializeField] float TimeDelay = 0.1f;
        //bool isWaiting = false;

        private Coroutine _waitCoroutine;
        IEnumerator WaitForSeconds(float waitTime)
        {
#if UNITY_EDITOR
            this.Log($"대기 시작");
#endif
            //isWaiting = true;
            // 딜레이 시간만큼 무조건 대기
            if (TimeDelay > 0)
                yield return new WaitForSeconds(waitTime);
            IsOn = true;
            _waitCoroutine = null;
            //isWaiting = false;
#if UNITY_EDITOR
            this.Log($"대기 종료");
#endif
        }

        bool StartWaitForSeconds()
        {
            if (_waitCoroutine == null)
            {
#if UNITY_EDITOR
                this.Log($"대기 코루틴 시작");
#endif
                _waitCoroutine = StartCoroutine(WaitForSeconds(TimeDelay));
                return true;
            }
            return false;
        }

        bool StopWatitForSeconds()
        {
            if (_waitCoroutine != null)
            {
#if UNITY_EDITOR
                this.Log($"대기 코루틴 강제종료");
#endif
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
                return true;
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
    	{
        	if (other.CompareTag(selectedTag))
            {
                //IsOn = true;
                objTarget = other.gameObject;
                if (StartWaitForSeconds())
                {
#if UNITY_EDITOR
                    this.Log($"진입 감지: {other.name}");
#endif
                }
            }
    	}

    	private void OnTriggerExit(Collider other)
    	{
            if (other.CompareTag(selectedTag))
            {
                IsOn = false;
                if (_waitCoroutine != null)
                {
#if UNITY_EDITOR
                    this.Log($"이탈 감지: {other.name}");
#endif
                   StopWatitForSeconds();
                }
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
		    Gizmos.color = gizmoColor;
		    Gizmos.matrix = transform.localToWorldMatrix;
		    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
	    }
	}    
}
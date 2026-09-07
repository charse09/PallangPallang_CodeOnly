using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SlideZoneTrigger : MonoBehaviour
    {
        [SerializeField] private SlideZoneProcess targetProcess;
        [SerializeField] private bool isExitTrigger = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (targetProcess != null)
                {
                    if (!isExitTrigger)
                    {
                        targetProcess.Execute(); // 备开 矫累
                    }
                    else
                    {
                        targetProcess.ExitZone(); // 备开 场
                    }
                }
            }
        }
    }
}
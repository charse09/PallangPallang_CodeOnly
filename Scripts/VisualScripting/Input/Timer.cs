using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Timer : ProcessBase
    {
        [SerializeField] private float seconds;
        public override void Execute()
        {
            StartCoroutine(WaitForSeconds());
        }

        private IEnumerator WaitForSeconds()
        {
            Debug.Log($"{gameObject.name}.WaitForSeconds({seconds}) Start");
            yield return new WaitForSeconds(seconds);
            Debug.Log($"{gameObject.name}.WaitForSeconds({seconds}) End");
            IsOn = true;
        }
    }
}
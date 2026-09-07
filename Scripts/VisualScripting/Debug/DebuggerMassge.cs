using UnityEngine;

public class DebuggerMassge : MonoBehaviour
{
    [SerializeField] bool updateLogger = false;
    bool isLoggedUpdate = false;
    bool isLoggedFixedUpdate = false;
    bool isLoggedLateUpdate = false;
    // Update is called once per frame
    void Update()
    {
        if (!isLoggedUpdate)
        {
            Debug.Log($"{gameObject.name}.Update()");
            isLoggedUpdate = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isLoggedFixedUpdate)
        {
            Debug.Log($"{gameObject.name}.FixedUpdate()");
            isLoggedFixedUpdate = true;
        }
    }

    private void LateUpdate()
    {
        if (!isLoggedLateUpdate)
        {
            Debug.Log($"{gameObject.name}.LateUpdate()");
            isLoggedLateUpdate = true;
        }
    }

    private void Awake()
    {
        Debug.Log($"{gameObject.name}.Awake()");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       Debug.Log($"{gameObject.name}.Start()");
    }

    private void OnEnable()
    {
        Debug.Log($"{gameObject.name}.OnEnable()");
    }

    private void OnDisable()
    {
        Debug.Log($"{gameObject.name}.OnDisable()");
    }
}

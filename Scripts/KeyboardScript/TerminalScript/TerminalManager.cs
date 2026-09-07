
using UnityEngine;

public class TerminalManager : MonoBehaviour
{
    [Header("Global Key Settings")]
    public float globalPressDepth = 0.1f;
    public float globalPressSpeed = 15f;    // ������ �� �ӵ�
    public float globalReleaseSpeed = 5f;   // �ö�� �� �ӵ� (�� õõ��)
    public float globalStayDelay = 0.1f;    // ���� ���¿��� ������ �ּ� �ð�


    // �ν����� â���� ��ũ��Ʈ �̸�(��Ϲ���)�� ��Ŭ���Ͽ� ������ �� �ִ� �޴� ����
    [ContextMenu("Setup All Keys Automatically")]
    public void SetupKeys()
    {
        // 1. Ű���� �ٵ��� ���� �ڽĵ��� ��ȸ�մϴ�.
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // 2. �ݶ��̴��� ���ٸ� �ڵ����� �߰��մϴ�. (���Ѵٸ� MeshCollider ������ ���� ����)
            if (child.GetComponent<Collider>() == null)
            {
                child.gameObject.AddComponent<MeshCollider>();
            }

            // 3. KeyboardKey ��ũ��Ʈ�� ���ٸ� �ڵ����� �߰��մϴ�.
            TerminalKey keyScript = child.GetComponent<TerminalKey>();
            if (keyScript == null)
            {
                keyScript = child.gameObject.AddComponent<TerminalKey>();
            }

            // 4. �Ŵ����� ������ ���� ��� Űĸ�� �ϰ� �����ϴ�.
            keyScript.pressDepth = this.globalPressDepth;
            keyScript.pressSpeed = this.globalPressSpeed;
            keyScript.releaseSpeed = this.globalReleaseSpeed;
            keyScript.stayDelay = this.globalStayDelay;
        }

        Debug.Log($"[TerminalManager] �� {transform.childCount}���� Űĸ�� ��ũ��Ʈ�� �ݶ��̴� ������ �Ϸ�Ǿ����ϴ�!");
    }

    void Awake()
    {
        // ���� �����Ϳ��� �̸� ���� �ʰ�, ������ ���� ��(��Ÿ��) 
        // �������� ��ũ��Ʈ�� ���̰� �ʹٸ� �Ʒ� �ּ��� �����ϼ���.
        // SetupKeys();
    }
}

/*
using UnityEngine;

public class KeyboardManager : MonoBehaviour
{
    [Header("Global Key Settings")]
    public float globalPressDepth = 0.1f;
    public float globalPressSpeed = 15f;    // [����] �������� �ӵ� �ϰ� ����
    public float globalReleaseSpeed = 5f;   // [����] �ö���� �ӵ� �ϰ� ����
    public float globalStayDelay = 0.1f;    // [�߰�] ������ �ð� �ϰ� ����

    [ContextMenu("Setup All Keys (Dual Collider Version)")]
    public void SetupKeys()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // ������ �پ��ִ� �ݶ��̴� �� ����� ���� ����
            Collider[] existingColliders = child.GetComponents<Collider>();
            foreach (var c in existingColliders)
            {
                DestroyImmediate(c);
            }

            // 1. �÷��̾ ��� ���� �ϴ� �ܴ��� �ݶ��̴� (Is Trigger = False)
            MeshCollider solidCollider = child.gameObject.AddComponent<MeshCollider>();
            solidCollider.isTrigger = false;

            // 2. ������ ���� ������ ���� �ݶ��̴� (Is Trigger = True)
            MeshCollider triggerCollider = child.gameObject.AddComponent<MeshCollider>();
            triggerCollider.isTrigger = true;
            //triggerCollider.size = new Vector3(1.01f, 1.01f, 1.01f);

            // 3. ��ũ��Ʈ �߰� �� ����
            KeyboardKey keyScript = child.GetComponent<KeyboardKey>();
            if (keyScript == null)
            {
                keyScript = child.gameObject.AddComponent<KeyboardKey>();
            }

            // [����] KeyboardKey�� ���� �������� ��ġ�ϵ��� ����
            keyScript.pressDepth = this.globalPressDepth;
            keyScript.pressSpeed = this.globalPressSpeed;
            keyScript.releaseSpeed = this.globalReleaseSpeed;
            keyScript.stayDelay = this.globalStayDelay;
        }

        Debug.Log($"[KeyboardManager] �� {transform.childCount}���� Űĸ�� ������ �Ϸ�Ǿ����ϴ�!");
    }
}
*/
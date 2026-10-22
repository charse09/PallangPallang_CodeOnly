using System.Collections;
using UnityEngine;

public class TicketBender : MonoBehaviour
{
    [Header("���� ����")]
    [Tooltip("���⿡ Bone001 ���� Bone009 ���� ���ʴ�� �巡���ؼ� ��������.")]
    public Transform[] bones;

    [Header("���θ��� ����")]
    [Tooltip("�� ����(��)�� ���η��� ��ǥ ���� (�ʹ� ũ�� ���׶��� ���������ϴ�)")]
    public float bendAnglePerBone = 6.0f;

    [Tooltip("���η����� �ӵ�")]
    public float bendSpeed = 2.0f;

    [Tooltip("��� ������ ���θ� ���ΰ�? (���� X �Ǵ� Z ���Դϴ�)")]
    public Vector3 bendAxis = new Vector3(1, 0, 0); // X�� ���� (1, 0, 0)

    // �ܺο��� "Ƽ�� �� ���Դ�! ���η���!" �� �� ȣ���ϴ� �Լ�
    public void StartBending()
    {
        StartCoroutine(BendTicketRoutine());
    }

    // �����Ϳ��� �÷������ڸ��� �׽�Ʈ�� ������ �Ʒ� Start �ּ��� Ǫ����.


    private void OnEnable()
    {
        StartBending();
    }


    private IEnumerator BendTicketRoutine()
    {
        float t = 0f;

        // �� ������� �� ó��(������ ��) ������ ����� �Ӵϴ�.
        Quaternion[] startRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            startRotations[i] = bones[i].localRotation;
        }

        // t�� 0���� 1�� �� ������ �ε巴�� ���θ��ϴ�.
        while (t < 1f)
        {
            t += Time.deltaTime * bendSpeed;

            // �迭�� 1�� �ε���(Bone002)���� ���θ��ϴ�. 
            // (Bone001�� ��迡 �پ��ִ� �Ѹ� �����̹Ƿ� �����ϴ� �� �ڿ��������ϴ�)
            for (int i = 1; i < bones.Length; i++)
            {
                // ������ ��(bendAxis) �������� bendAnglePerBone ��ŭ ��ǥ ȸ���� ���
                Vector3 eulerAngle = bendAxis * bendAnglePerBone;
                Quaternion targetRot = startRotations[i] * Quaternion.Euler(eulerAngle);

                // Slerp�� �̿��� ���� �������� ��ǥ ������ �ε巴��(�������ϰ�) ����
                bones[i].localRotation = Quaternion.Slerp(startRotations[i], targetRot, t);
            }

            yield return null; // ���� �����ӱ��� ��� (�ε巯�� �ִϸ��̼��� ����)
        }
    }
}
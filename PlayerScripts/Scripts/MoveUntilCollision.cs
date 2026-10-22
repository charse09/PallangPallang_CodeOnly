using UnityEngine;

public class MoveUntilCollision : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f; // 이동 속도

    private bool isMoving = true; // 이동 상태를 체크하는 변수

    void Update()
    {
        // isMoving이 true일 때만 앞으로 전진
        if (isMoving)
        {
            // 로컬 좌표계 기준으로 앞쪽(Z축)으로 이동
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    // 다른 오브젝트와 충돌했을 때 자동으로 호출되는 함수
    void OnCollisionEnter(Collision collision)
    {
        if (isMoving)
        {
            // 충돌이 발생하면 이동 상태를 false로 변경하여 멈춤
            isMoving = false;

            // 디버그 창에 어떤 오브젝트와 부딪혔는지 출력
            Debug.Log(collision.gameObject.name + "와(과) 충돌하여 정지했습니다!");
        }
    }
}
using UnityEngine;

public class PaperFlutter : MonoBehaviour
{
    private Rigidbody rb;

    [Header("설정")]
    public float fallSpeed = 0.5f;    // 떨어지는 속도 조절 (Drag 대용)
    public float swingSpeed = 2.0f;   // 좌우로 흔들리는 속도
    public float swingAmount = 3.0f;  // 좌우로 흔들리는 폭
    public float rotationAmount = 20.0f; // 흔들릴 때 회전하는 각도

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 종이는 가벼우므로 기본 Drag를 높여주는 게 좋아.
        rb.linearDamping = 2.0f;  // 이동 저항 (기존 drag)
        rb.angularDamping = 1.0f; // 회전 저항 (기존 angularDrag)
    }

    void FixedUpdate()
    {
        // 1. 사인파를 이용해 좌우 흔들림 계산
        float sine = Mathf.Sin(Time.time * swingSpeed);

        // 2. 수평으로 밀어주는 힘 (좌우 왔다갔다)
        Vector3 horizontalForce = transform.right * sine * swingAmount;

        // 3. 살짝 위로 들어올리는 힘 (천천히 떨어지게)
        Vector3 liftForce = Vector3.up * Mathf.Abs(sine) * fallSpeed;

        // 4. 힘 적용
        rb.AddForce(horizontalForce + liftForce, ForceMode.Acceleration);

        // 5. 시각적으로 좌우로 기우뚱하게 회전 (Z축 회전)
        float tiltAngle = sine * rotationAmount;
        Quaternion targetRotation = Quaternion.Euler(rb.rotation.eulerAngles.x, rb.rotation.eulerAngles.y, tiltAngle);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, Time.fixedDeltaTime * swingSpeed));
    }
}
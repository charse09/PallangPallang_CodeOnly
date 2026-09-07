using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.VisualScripting
{
    public class ForceMoveHazard : ProcessBase
    {
        [Header("Movement Target")]
        [SerializeField] private Rigidbody objectToMove;

        [Header("Kill Target Collider")]
        [Tooltip("Player와 닿았을 때 죽이는 판정을 가진 오브젝트")]
        [SerializeField] private GameObject killObject;

        [Header("Force Settings")]
        [SerializeField] private Vector3 forceDirection = new Vector3(1f, 0f, 0f);
        [SerializeField] private float forcePower = 10f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Header("Hazard Time")]
        [SerializeField] private float hazardDuration = 3f;

        [Header("Game Over")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float restartDelay = 1f;

        private bool isHazardActive = false;
        private bool isGameOver = false;

        private void Start()
        {
            if (objectToMove == null)
                objectToMove = GetComponent<Rigidbody>();

            if (killObject == null && objectToMove != null)
                killObject = objectToMove.gameObject;
        }

        public override void Execute()
        {
            if (IsOn) return;
            if (objectToMove == null) return;

            StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            IsOn = true;
            isHazardActive = true;

            objectToMove.isKinematic = false;

            Vector3 dir = forceDirection.normalized;
            objectToMove.AddForce(dir * forcePower, forceMode);

            float timer = 0f;

            while (timer < hazardDuration)
            {
                timer += Time.deltaTime;

                if (objectToMove.linearVelocity.magnitude > maxSpeed)
                {
                    objectToMove.linearVelocity =
                        objectToMove.linearVelocity.normalized * maxSpeed;
                }

                yield return null;
            }

            isHazardActive = false;
            IsOn = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isHazardActive) return;
            if (isGameOver) return;

            // 이 스크립트가 붙은 오브젝트가 아니라,
            // killObject가 충돌했을 때만 처리
            if (collision.gameObject.CompareTag(playerTag))
            {
                GameOver(collision.gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isHazardActive) return;
            if (isGameOver) return;

            if (other.CompareTag(playerTag))
            {
                GameOver(other.gameObject);
            }
        }

        public void TryKillPlayer(GameObject player)
        {
            if (!isHazardActive) return;
            if (isGameOver) return;
            if (player == null) return;
            if (!player.CompareTag(playerTag)) return;

            GameOver(player);
        }

        private void GameOver(GameObject player)
        {
            isGameOver = true;
            isHazardActive = false;

            Destroy(player);
            StartCoroutine(RestartSceneAfterDelay());
        }

        private IEnumerator RestartSceneAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            isHazardActive = false;
            StopAllCoroutines();
        }
    }
}
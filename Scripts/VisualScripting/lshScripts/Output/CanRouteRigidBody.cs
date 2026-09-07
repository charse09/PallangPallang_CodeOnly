using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanRouteHazard : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private List<Transform> routePoints = new List<Transform>();
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Tags")]
    [SerializeField] private string pigeonTag = "Pigeon";
    [SerializeField] private string playerTag = "Player";

    [Header("Game Over")]
    [SerializeField] private float restartDelay = 1f;

    private bool isMoving = false;
    private bool isGameOver = false;
    private int currentIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (isGameOver) return;

        if (other.CompareTag(pigeonTag))
        {
            StartMove();
        }

        if (other.CompareTag(playerTag))
        {
            GameOver(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isGameOver) return;

        if (collision.gameObject.CompareTag(pigeonTag))
        {
            StartMove();
        }

        if (collision.gameObject.CompareTag(playerTag))
        {
            GameOver(collision.gameObject);
        }
    }

    private void StartMove()
    {
        if (isMoving) return;
        if (routePoints == null || routePoints.Count == 0) return;

        isMoving = true;
        currentIndex = 0;
        StartCoroutine(C_MoveRoute());
    }

    private IEnumerator C_MoveRoute()
    {
        while (currentIndex < routePoints.Count)
        {
            Transform target = routePoints[currentIndex];

            if (target == null)
            {
                currentIndex++;
                continue;
            }

            while (Vector3.Distance(transform.position, target.position) > arriveDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = target.position;
            currentIndex++;

            yield return null;
        }

        isMoving = false;
    }

    private void GameOver(GameObject player)
    {
        isGameOver = true;
        isMoving = false;

        Destroy(player);
        StartCoroutine(C_RestartScene());
    }

    private IEnumerator C_RestartScene()
    {
        yield return new WaitForSeconds(restartDelay);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
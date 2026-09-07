using UnityEngine;
using System.Collections;

public class YutnoriSystem : MonoBehaviour
{
    public enum YutValueStatus
    {
        Doe,
        Gae,
        Girl,
        Yut,
    }

    [Header("윷 판 및 스탬프 연결")]
    [Tooltip("이동할 미니 장승(스탬프 본체) 스크립트를 연결하세요.")]
    [SerializeField] private Stamp miniJangseung;
    [Tooltip("모든 윷 칸을 순서대로 연결하기 (0번은 시작점)")]
    [SerializeField] private Transform[] NextLinePos;

    [Header("윷놀이에 대한 설정")]
    [SerializeField] private int DoeValue = 1;
    [SerializeField] private int GaeValue = 2;
    [SerializeField] private int GirlValue = 3;

    [Tooltip("장승이 공중에 복귀한 뒤, 다음 윷을 던지기 전까지의 대기 시간")]
    [SerializeField] private float waitBeforeNextRoll = 3.0f;

    [Header("페이즈(꼭짓점) 설정")]
    [SerializeField] private int currentPhaseLength = 6;

    [Header("윷 결과 시각화 (4개의 장승)")]
    [Tooltip("윷 역할을 할 4개의 장승 렌더러를 연결하세요.")]
    [SerializeField] private Renderer[] yutSticks = new Renderer[4];
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color redColor = Color.red;

    // [추가된 부분] 장승이 회전하는 속도를 조절합니다. (낮을수록 천천히 돕니다)
    [SerializeField] private float yutRotationSpeed = 3.0f;

    private Quaternion[] inwardRotations = new Quaternion[4]; // 처음 바라보던 '안쪽' 방향 기억
    private Quaternion[] targetRotations = new Quaternion[4]; // 장승들이 돌아가야 할 '목표' 방향

    private int currentIndex = 0;

    void Start()
    {
        // 시작할 때 4개 장승의 원래(안쪽) 회전값을 저장해 둡니다.
        for (int i = 0; i < yutSticks.Length; i++)
        {
            if (yutSticks[i] != null)
            {
                inwardRotations[i] = yutSticks[i].transform.rotation;
                targetRotations[i] = inwardRotations[i]; // 초기 목표 방향도 안쪽으로 설정

                // 초기 상태를 파란색으로 칠해줍니다.
                yutSticks[i].material.color = blueColor;
            }
        }

        if (miniJangseung != null && NextLinePos.Length > 0)
        {
            Vector3 startPos = NextLinePos[0].position;
            miniJangseung.transform.position = new Vector3(startPos.x, miniJangseung.transform.position.y, startPos.z);

            StartCoroutine(AutoYutnoriRoutine());
        }
    }

    // [추가된 부분] 매 프레임마다 장승들을 목표 방향으로 천천히 회전시킵니다.
    void Update()
    {
        for (int i = 0; i < yutSticks.Length; i++)
        {
            if (yutSticks[i] != null)
            {
                // 현재 회전값에서 목표 회전값(targetRotations)으로 스무스하게 보간(Slerp)합니다.
                yutSticks[i].transform.rotation = Quaternion.Slerp(
                    yutSticks[i].transform.rotation,
                    targetRotations[i],
                    Time.deltaTime * yutRotationSpeed
                );
            }
        }
    }

    private IEnumerator AutoYutnoriRoutine()
    {
        while (true)
        {
            YutValueStatus rolledValue = (YutValueStatus)Random.Range(0, 4);
            int stepsToMove = GetSteps(rolledValue);

            // 윷이 던져지면 4개의 장승(윷가락)의 색상과 방향을 바꿉니다!
            UpdateYutVisuals(rolledValue);

            if (rolledValue == YutValueStatus.Yut)
            {
                Debug.Log($"[윷놀이] 윷! 4개가 빨갛게 변합니다. {stepsToMove}칸 데굴데굴 구릅니다.");
                yield return StartCoroutine(RollToTargetRoutine(stepsToMove));
            }
            else
            {
                Debug.Log($"[윷놀이] 굴린 결과: {rolledValue}. 쿵쿵 이동 시작!");
                yield return StartCoroutine(StompPathRoutine(stepsToMove));
            }

            yield return new WaitForSeconds(waitBeforeNextRoll);
        }
    }

    /// <summary>
    /// 윷 결과에 맞춰 4개 장승의 색상과 목표 방향을 업데이트하는 함수
    /// </summary>
    private void UpdateYutVisuals(YutValueStatus status)
    {
        int redCount = 0;

        switch (status)
        {
            case YutValueStatus.Doe: redCount = 1; break;
            case YutValueStatus.Gae: redCount = 2; break;
            case YutValueStatus.Girl: redCount = 3; break;
            case YutValueStatus.Yut: redCount = 4; break;
        }

        // 4개의 장승을 순회하며 상태를 변경합니다.
        for (int i = 0; i < yutSticks.Length; i++)
        {
            if (yutSticks[i] == null) continue;

            if (i < redCount)
            {
                // 빨간색 & 바깥쪽 (즉시 회전시키지 않고 '목표 회전값'만 바꿔줍니다)
                yutSticks[i].material.color = redColor;
                targetRotations[i] = inwardRotations[i] * Quaternion.Euler(0, 180f, 0);
            }
            else
            {
                // 파란색 & 안쪽
                yutSticks[i].material.color = blueColor;
                targetRotations[i] = inwardRotations[i];
            }
        }
    }

    private int GetSteps(YutValueStatus status)
    {
        int nextVertex = ((currentIndex / currentPhaseLength) + 1) * currentPhaseLength;
        int steps = nextVertex - currentIndex;

        switch (status)
        {
            case YutValueStatus.Doe: return DoeValue;
            case YutValueStatus.Gae: return GaeValue;
            case YutValueStatus.Girl: return GirlValue;
            case YutValueStatus.Yut:
                return steps > 0 ? steps : currentPhaseLength;
            default: return 0;
        }
    }

    private IEnumerator StompPathRoutine(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            currentIndex = (currentIndex + 1) % NextLinePos.Length;
            miniJangseung.SetTarget(NextLinePos[currentIndex]);

            yield return new WaitUntil(() => miniJangseung.IsAttacking);
            yield return new WaitUntil(() => !miniJangseung.IsAttacking);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator RollToTargetRoutine(int steps)
    {
        Transform[] pathToRoll = new Transform[steps];
        for (int i = 0; i < steps; i++)
        {
            currentIndex = (currentIndex + 1) % NextLinePos.Length;
            pathToRoll[i] = NextLinePos[currentIndex];
        }

        yield return StartCoroutine(miniJangseung.RollSequence(pathToRoll));
    }
}
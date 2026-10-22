using UnityEngine;
using UnityEngine.Events;

public class BossBridge : MonoBehaviour
{
    [Header("Renderer Setup")]
    [Tooltip("셰이더 그래프 메테리얼이 적용된 유리 오브젝트의 Renderer")]
    [SerializeField] private Renderer glassRenderer;

    [Header("Final Event")]
    [Tooltip("마지막 페이즈에서 등을 밟아 유리가 완전히 깨졌을 때 실행할 이벤트")]
    public UnityEvent onFinalGlassBroken;

    private bool isStepped = false;
    private int currentPhase;
    private int maxPhase;

    // 셰이더 그래프에서 생성한 프로퍼티(Reference) 이름과 정확히 일치해야 합니다.
    private readonly int crackProgressProperty = Shader.PropertyToID("_CrackProgress");

    public void Initialize(int phaseIndex, int totalPhases)
    {
        currentPhase = phaseIndex;
        maxPhase = totalPhases;

        // Renderer가 할당되지 않았다면 자기 자신에서 찾습니다.
        if (glassRenderer == null)
        {
            glassRenderer = GetComponent<Renderer>();
        }

        // 스폰될 때 현재 페이즈에 맞는 균열 상태로 초기화
        UpdateCrackShader(currentPhase);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isStepped) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            isStepped = true;
            StepOnGlass();
        }
    }

    private void StepOnGlass()
    {
        // 밟았을 때 균열 단계를 다음 페이즈 수준으로 올립니다.
        int nextState = currentPhase + 1;
        UpdateCrackShader(nextState);

        // 현재 페이즈가 마지막 페이즈(배열의 끝)였다면 완전 파괴 이벤트 실행
        if (currentPhase >= maxPhase - 1)
        {
            Debug.Log("유리가 완전히 깨졌습니다! 다음 씬 또는 컷신 연출 시작!");

            // 보스 등에서 플레이어가 밑으로 쑥 빠지게 만들고 싶다면 콜라이더를 끄면 됩니다.
            // GetComponent<Collider>().enabled = false;

            onFinalGlassBroken?.Invoke();
        }
    }

    private void UpdateCrackShader(int stateIndex)
    {
        if (glassRenderer == null) return;

        // 총 페이즈 개수를 기준으로 0.0 ~ 1.0 사이의 비율을 구합니다.
        // 예) maxPhase가 4일 경우: 0페이즈=0.0 / 1페이즈=0.33 / 2페이즈=0.66 / 3페이즈=1.0
        float maxState = Mathf.Max(1, maxPhase);
        float crackRatio = Mathf.Clamp01((float)stateIndex / maxState);

        // 메테리얼의 _CrackProgress(진행도)에 계산한 비율을 넘겨줍니다.
        glassRenderer.material.SetFloat(crackProgressProperty, crackRatio);
    }
}
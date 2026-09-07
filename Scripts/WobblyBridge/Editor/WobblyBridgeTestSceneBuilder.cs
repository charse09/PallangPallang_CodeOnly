using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 흔들리는 밧줄 다리 테스트 씬을 자동 생성하는 에디터 유틸리티.
/// 메뉴: Tools > Wobbly Bridge > Create Test Scene
/// 
/// [밧줄 다리 컨셉]
/// - 좁고 빈틈 없는 발판들이 일렬로 이어진 위태로운 밧줄 다리
/// - 양쪽에 밧줄(로프) 난간
/// - 다리 중앙부가 살짝 처지는 현수선(카테나리) 형태
/// - 발판 사이에 틈이 없어서 종이 캐릭터도 빠지지 않음
/// </summary>
public class WobblyBridgeTestSceneBuilder : EditorWindow
{
    [MenuItem("Tools/Wobbly Bridge/Create Test Scene")]
    public static void CreateTestScene()
    {
        // 새 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ═══════════════════════════════════════════
        // 파라미터 정의
        // ═══════════════════════════════════════════
        int plankCount = 30;              // 발판 수
        float plankWidth = 1.2f;          // 발판 폭 (좁게! 아슬아슬하게)
        float plankDepth = 0.55f;         // 발판 깊이 (전진 방향)
        float plankHeight = 0.08f;        // 발판 두께 (얇게)
        float plankSpacing = 0.5f;        // 발판 간격 (= plankDepth보다 작게 → 빈틈 없음)
        float bridgeHeight = 12f;         // 다리가 놓인 높이 (아래가 낭떠러지)
        float sagAmount = 2.5f;           // 현수선 처짐 정도 (중앙이 낮음)
        float ropeHeight = 0.7f;          // 밧줄 난간 높이
        float ropeInset = 0.08f;          // 밧줄이 발판 가장자리에서 안쪽으로 들어오는 정도

        float totalBridgeLength = plankCount * plankSpacing;
        float bridgeStartZ = 5f;

        // ═══════════════════════════════════════════
        // 1. 시작 절벽 플랫폼
        // ═══════════════════════════════════════════
        GameObject startCliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startCliff.name = "StartCliff";
        startCliff.transform.position = new Vector3(0f, bridgeHeight - 1f, 0f);
        startCliff.transform.localScale = new Vector3(6f, bridgeHeight + 1f, 10f);
        SetColor(startCliff, new Color(0.45f, 0.42f, 0.38f)); // 돌 느낌

        // 시작 절벽 위 걸어가는 면
        GameObject startTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startTop.name = "StartTop";
        startTop.transform.position = new Vector3(0f, bridgeHeight + 0.05f, 1.5f);
        startTop.transform.localScale = new Vector3(5f, 0.1f, 7f);
        SetColor(startTop, new Color(0.5f, 0.48f, 0.42f));

        // ═══════════════════════════════════════════
        // 2. 도착 절벽 플랫폼
        // ═══════════════════════════════════════════
        float endZ = bridgeStartZ + totalBridgeLength + 5f;
        GameObject endCliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endCliff.name = "EndCliff";
        endCliff.transform.position = new Vector3(0f, bridgeHeight - 1f, endZ);
        endCliff.transform.localScale = new Vector3(6f, bridgeHeight + 1f, 10f);
        SetColor(endCliff, new Color(0.45f, 0.42f, 0.38f));

        GameObject endTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endTop.name = "EndTop";
        endTop.transform.position = new Vector3(0f, bridgeHeight + 0.05f, endZ - 1.5f);
        endTop.transform.localScale = new Vector3(5f, 0.1f, 7f);
        SetColor(endTop, new Color(0.5f, 0.48f, 0.42f));

        // ═══════════════════════════════════════════
        // 3. 다리 루트 오브젝트
        // ═══════════════════════════════════════════
        GameObject bridgeRoot = new GameObject("WobblyBridge");
        bridgeRoot.transform.position = new Vector3(0f, bridgeHeight, bridgeStartZ);
        WobblyBridge bridgeScript = bridgeRoot.AddComponent<WobblyBridge>();

        // ═══════════════════════════════════════════
        // 4. 다리 트리거 존
        // ═══════════════════════════════════════════
        GameObject triggerZone = new GameObject("BridgeTriggerZone");
        triggerZone.transform.SetParent(bridgeRoot.transform);
        triggerZone.transform.localPosition = new Vector3(0f, 1.5f, totalBridgeLength * 0.5f);
        BoxCollider triggerCollider = triggerZone.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(plankWidth + 2f, 5f, totalBridgeLength + 2f);

        // ═══════════════════════════════════════════
        // 5. 발판 생성 (빈틈 없이 밀착!)
        // ═══════════════════════════════════════════
        GameObject planksParent = new GameObject("Planks");
        planksParent.transform.SetParent(bridgeRoot.transform);
        planksParent.transform.localPosition = Vector3.zero;

        for (int i = 0; i < plankCount; i++)
        {
            float z = i * plankSpacing;

            // 현수선 처짐: 중앙이 가장 낮음 (포물선 근사)
            float normalizedPos = (float)i / (plankCount - 1); // 0 ~ 1
            float sagOffset = -sagAmount * (1f - Mathf.Pow(2f * normalizedPos - 1f, 2f));

            // --- 발판 ---
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = $"Plank_{i:D2}";
            plank.transform.SetParent(bridgeRoot.transform);
            plank.transform.localPosition = new Vector3(0f, sagOffset, z);
            plank.transform.localScale = new Vector3(plankWidth, plankHeight, plankDepth);
            plank.AddComponent<WobblyBridgePlank>();

            // 발판 색상: 낡은 나무 느낌, 약간씩 색 변화
            float colorVar = Random.Range(-0.04f, 0.04f);
            SetColor(plank, new Color(0.55f + colorVar, 0.38f + colorVar, 0.2f + colorVar));
        }

        // ═══════════════════════════════════════════
        // 6. 밧줄(로프) 난간 — 좌우 양쪽
        // ═══════════════════════════════════════════
        GameObject ropesParent = new GameObject("Ropes");
        ropesParent.transform.SetParent(bridgeRoot.transform);
        ropesParent.transform.localPosition = Vector3.zero;

        // 밧줄 세그먼트: 발판과 같은 수, 같은 처짐 커브를 따름
        for (int i = 0; i < plankCount - 1; i++)
        {
            float z0 = i * plankSpacing;
            float z1 = (i + 1) * plankSpacing;
            float zMid = (z0 + z1) * 0.5f;

            float norm0 = (float)i / (plankCount - 1);
            float norm1 = (float)(i + 1) / (plankCount - 1);
            float sag0 = -sagAmount * (1f - Mathf.Pow(2f * norm0 - 1f, 2f));
            float sag1 = -sagAmount * (1f - Mathf.Pow(2f * norm1 - 1f, 2f));
            float sagMid = (sag0 + sag1) * 0.5f;

            float segmentLen = plankSpacing;

            // --- 왼쪽 밧줄 ---
            float leftX = -(plankWidth * 0.5f - ropeInset);
            GameObject leftRope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftRope.name = $"LeftRope_{i:D2}";
            leftRope.transform.SetParent(ropesParent.transform);
            leftRope.transform.localPosition = new Vector3(leftX, sagMid + ropeHeight, zMid);
            leftRope.transform.localScale = new Vector3(0.03f, segmentLen * 0.55f, 0.03f);
            leftRope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetColor(leftRope, new Color(0.4f, 0.3f, 0.18f));

            // --- 오른쪽 밧줄 ---
            float rightX = plankWidth * 0.5f - ropeInset;
            GameObject rightRope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightRope.name = $"RightRope_{i:D2}";
            rightRope.transform.SetParent(ropesParent.transform);
            rightRope.transform.localPosition = new Vector3(rightX, sagMid + ropeHeight, zMid);
            rightRope.transform.localScale = new Vector3(0.03f, segmentLen * 0.55f, 0.03f);
            rightRope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetColor(rightRope, new Color(0.4f, 0.3f, 0.18f));
        }

        // ═══════════════════════════════════════════
        // 7. 밧줄 수직 연결줄 (발판에서 밧줄로 올라가는 끈)
        // ═══════════════════════════════════════════
        GameObject verticalRopes = new GameObject("VerticalRopes");
        verticalRopes.transform.SetParent(bridgeRoot.transform);
        verticalRopes.transform.localPosition = Vector3.zero;

        for (int i = 0; i < plankCount; i += 2) // 2칸마다
        {
            float z = i * plankSpacing;
            float normalizedPos = (float)i / (plankCount - 1);
            float sagOffset = -sagAmount * (1f - Mathf.Pow(2f * normalizedPos - 1f, 2f));

            float leftX = -(plankWidth * 0.5f - ropeInset);
            float rightX = plankWidth * 0.5f - ropeInset;

            // 왼쪽 수직 줄
            GameObject leftVert = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftVert.name = $"LeftVertRope_{i:D2}";
            leftVert.transform.SetParent(verticalRopes.transform);
            leftVert.transform.localPosition = new Vector3(leftX, sagOffset + ropeHeight * 0.5f, z);
            leftVert.transform.localScale = new Vector3(0.025f, ropeHeight * 0.5f, 0.025f);
            SetColor(leftVert, new Color(0.38f, 0.28f, 0.15f));

            // 오른쪽 수직 줄
            GameObject rightVert = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightVert.name = $"RightVertRope_{i:D2}";
            rightVert.transform.SetParent(verticalRopes.transform);
            rightVert.transform.localPosition = new Vector3(rightX, sagOffset + ropeHeight * 0.5f, z);
            rightVert.transform.localScale = new Vector3(0.025f, ropeHeight * 0.5f, 0.025f);
            SetColor(rightVert, new Color(0.38f, 0.28f, 0.15f));
        }

        // ═══════════════════════════════════════════
        // 8. 양 끝 기둥 (밧줄 고정점)
        // ═══════════════════════════════════════════
        float postHeight = 2.5f;
        float postY = bridgeHeight + postHeight * 0.5f - 0.5f;

        // 시작 쪽 기둥
        CreatePost("StartLeftPost", new Vector3(-plankWidth * 0.5f - 0.15f, postY, bridgeStartZ - 0.3f), postHeight);
        CreatePost("StartRightPost", new Vector3(plankWidth * 0.5f + 0.15f, postY, bridgeStartZ - 0.3f), postHeight);

        // 끝 쪽 기둥
        float lastPlankZ = bridgeStartZ + (plankCount - 1) * plankSpacing + 0.3f;
        CreatePost("EndLeftPost", new Vector3(-plankWidth * 0.5f - 0.15f, postY, lastPlankZ), postHeight);
        CreatePost("EndRightPost", new Vector3(plankWidth * 0.5f + 0.15f, postY, lastPlankZ), postHeight);

        // ═══════════════════════════════════════════
        // 9. 낙하 감지용 킬 존 (참조용)
        // ═══════════════════════════════════════════
        GameObject killZone = new GameObject("KillZone_Reference");
        killZone.transform.position = new Vector3(0f, -5f, bridgeStartZ + totalBridgeLength * 0.5f);
        BoxCollider killCollider = killZone.AddComponent<BoxCollider>();
        killCollider.isTrigger = true;
        killCollider.size = new Vector3(30f, 1f, totalBridgeLength + 20f);

        // ═══════════════════════════════════════════
        // 10. 조명
        // ═══════════════════════════════════════════
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                light.intensity = 1.3f;
                light.color = new Color(1f, 0.95f, 0.88f);
            }
        }

        // ═══════════════════════════════════════════
        // 11. 카메라 위치
        // ═══════════════════════════════════════════
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(6f, bridgeHeight + 4f, bridgeStartZ - 5f);
            mainCam.transform.LookAt(new Vector3(0f, bridgeHeight, bridgeStartZ + totalBridgeLength * 0.3f));
        }

        // ═══════════════════════════════════════════
        // 12. 스폰 포인트
        // ═══════════════════════════════════════════
        GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, bridgeHeight + 0.5f, 2f);

        // ═══════════════════════════════════════════
        // 씬 저장
        // ═══════════════════════════════════════════
        string scenePath = "Assets/Scenes/_TestScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // 포커스
        Selection.activeGameObject = bridgeRoot;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"<color=green>[WobblyBridge]</color> 밧줄 다리 테스트 씬이 '{scenePath}'에 생성되었습니다!\n" +
                  $"  - 발판 수: {plankCount}개 (폭: {plankWidth}m, 빈틈 없음)\n" +
                  $"  - 다리 길이: {totalBridgeLength:F1}m, 높이: {bridgeHeight}m\n" +
                  $"  - 처짐: {sagAmount}m (현수선 형태)\n" +
                  $"  - 플레이어를 'PlayerSpawnPoint'에 배치하세요!");

        EditorUtility.DisplayDialog("밧줄 다리 테스트 씬 생성 완료",
            $"흔들리는 밧줄 다리 테스트 씬이 생성되었습니다!\n\n" +
            $"경로: {scenePath}\n" +
            $"발판: {plankCount}개 (폭 {plankWidth}m, 빈틈 없음)\n" +
            $"다리 길이: {totalBridgeLength:F1}m\n" +
            $"높이: {bridgeHeight}m (낭떠러지)\n\n" +
            $"플레이어 프리팹을 PlayerSpawnPoint에 배치하고\n" +
            $"Player 태그가 설정되어 있는지 확인하세요.",
            "확인");
    }

    /// <summary>
    /// 밧줄 고정 기둥을 생성합니다.
    /// </summary>
    private static void CreatePost(string name, Vector3 position, float height)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = name;
        post.transform.position = position;
        post.transform.localScale = new Vector3(0.2f, height * 0.5f, 0.2f);
        SetColor(post, new Color(0.35f, 0.28f, 0.18f));
    }

    /// <summary>
    /// 오브젝트에 색상을 설정합니다.
    /// </summary>
    private static void SetColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // URP/Lit 사용 시도, 없으면 Standard
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
            {
                mat = new Material(Shader.Find("Standard"));
            }
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting; // 기존 IPoolable(New, Free) 네임스페이스 사용

/// <summary>
/// 프로젝트 전체의 오브젝트 풀링(Object Pooling)을 통괄 관리하는 싱글톤 매니저 클래스입니다.
/// 게임 오브젝트의 빈번한 생성(Instantiate)과 파괴(Destroy)로 인한 메모리 파편화 및 프레임 드랍을 방지합니다.
/// </summary>

public class PoolManager : ManagerBase<PoolManager>
{
    // ========================================================================
    // [내부 딕셔너리 관리 변수]
    // ========================================================================
    /// <summary> poolKey를 기준으로 비활성화 상태의 휴식 중인 오브젝트들을 저장하는 큐(Queue) 딕셔너리 </summary>
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    /// <summary> poolKey를 기준으로 원본 프리팹(Prefab) 참조를 보관하는 딕셔너리 (풀 자동 확장 시 사용) </summary>
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    /// <summary> 하이라키(Hierarchy) 창 정리를 위해 poolKey별 그룹 빈 오브젝트 Transform을 보관하는 딕셔너리 </summary>
    private Dictionary<string, Transform> groupDictionary = new Dictionary<string, Transform>();

    // ========================================================================
    // [메서드 / 기능 함수]
    // ========================================================================
    /// <summary>
    /// 새로운 오브젝트 풀을 등록하고 지정한 개수만큼 객체를 미리 생성(프리폼)해 둡니다.
    /// </summary>
    /// <param name="poolKey">풀을 식별할 고유 키 문자열 (예: "Effect_BumperAntennaSpark")</param>
    /// <param name="prefab">풀링할 대상 게임 오브젝트 프리팹</param>
    /// <param name="poolSize">최초에 생성하여 준비해 둘 객체의 수</param>
    public void CreatePool(string poolKey, GameObject prefab, int poolSize)
    {
        // 이미 생성된 키가 아닌 경우에만 신규 등록 수행
        if (!poolDictionary.ContainsKey(poolKey))
        {
            // 1. 데이터 관리를 위한 딕셔너리 공간 할당
            poolDictionary.Add(poolKey, new Queue<GameObject>());
            prefabDictionary.Add(poolKey, prefab);

            // 2. 하이라키 창이 더러워지지 않도록 매니저 하위에 전용 그룹 오브젝트 생성 (${poolKey}_Group)
            GameObject poolGroup = new GameObject($"{poolKey}_Group");
            poolGroup.transform.SetParent(this.transform);
            groupDictionary.Add(poolKey, poolGroup.transform);

            // 3. 요청한 개수(poolSize)만큼 인스턴스를 미리 생성하여 큐에 대기시킴
            for (int i = 0; i < poolSize; i++)
            {
                CreateNewInstance(poolKey, prefab, poolGroup.transform);
            }
        }
    }

    /// <summary>
    /// [내부 전용 함수] 새로운 오브젝트 인스턴스를 생성하여 비활성화 상태로 큐에 보관합니다.
    /// </summary>
    /// <param name="poolKey">등록할 풀 키 문자열</param>
    /// <param name="prefab">생성할 프리팹</param>
    /// <param name="parent">배치될 부모 Transform (그룹 오브젝트)</param>
    /// <returns>생성된 게임 오브젝트</returns>
    private GameObject CreateNewInstance(string poolKey, GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false); // 생성 직후 즉시 비활성화
        poolDictionary[poolKey].Enqueue(obj); // 대기열(Queue)에 등록
        return obj;
    }

    /// <summary>
    /// 풀에서 쉬고 있는 오브젝트를 꺼내어 지정된 위치와 회전값으로 씬에 스폰(배치)합니다.
    /// 여유 객체가 없을 경우 자동으로 1개를 더 생성(자동 확장)하여 정상 동작을 보장합니다.
    /// </summary>
    /// <param name="poolKey">스폰할 풀의 식별 키</param>
    /// <param name="position">배치할 월드 좌표 위치</param>
    /// <param name="rotation">배치할 월드 회전값</param>
    /// <returns>스폰된 게임 오브젝트 (풀을 찾지 못하면 null)</returns>
    public GameObject SpawnFromPool(string poolKey, Vector3 position, Quaternion rotation)
    {
        // 예외 처리: 존재하지 않는 키를 호출한 경우 경고 출력 후 종료
        if (!poolDictionary.ContainsKey(poolKey))
        {
            UnityEngine.Debug.LogWarning($"[PoolManager] {poolKey} 이름의 풀이 존재하지 않습니다.");
            return null;
        }

        // [안전장치] 풀에 여유 객체(Count == 0)가 없으면 무작정 에러를 내지 않고 1개 즉시 추가 생성
        if (poolDictionary[poolKey].Count == 0)
        {
            CreateNewInstance(poolKey, prefabDictionary[poolKey], groupDictionary[poolKey]);
        }

        // 1. 큐의 맨 앞에서 쉬고 있는 오브젝트 꺼내기
        GameObject obj = poolDictionary[poolKey].Dequeue();

        // 2. 위치 및 회전값 재설정 후 오브젝트 활성화
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // 3. [인터페이스 연동] 스폰된 객체에 IPoolable이 부착되어 있다면 New() 자동 실행 (파티클/VFX 재생 등)
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.New();
        }

        return obj;
    }

    /// <summary>
    /// 사용이 완전히 끝난 오브젝트를 정돈하여 풀(Queue)로 반환(회수)합니다.
    /// </summary>
    /// <param name="poolKey">반환할 풀의 식별 키</param>
    /// <param name="obj">반환할 게임 오브젝트</param>
    public void ReturnToPool(string poolKey, GameObject obj)
    {
        if (obj == null) return;

        // 1. [인터페이스 연동] 반환하기 전 IPoolable.Free()를 호출하여 정지/잔상 정리 수행
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.Free();
        }

        // 2. 오브젝트 비활성화
        obj.SetActive(false);

        // 3. 씬 진행 중 부모 관계(Parent)가 변경되었을 수 있으므로 원래의 풀 그룹 하위로 부모 복구
        if (groupDictionary.ContainsKey(poolKey))
        {
            obj.transform.SetParent(groupDictionary[poolKey]);
        }

        // 4. 다시 언제든 재활용할 수 있도록 대기열 큐(Queue)에 밀어넣기
        if (poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary[poolKey].Enqueue(obj);
        }
    }

    /// <summary>
    /// 씬이 전환되거나 메모리를 대대적으로 정리해야 할 때, 풀링된 모든 인스턴스 파괴 및 데이터 초기화를 수행합니다.
    /// </summary>
    public void ClearAllPools()
    {
        // 1. 모든 큐 내부의 게임 오브젝트 실체 파괴 (Destroy)
        foreach (var pool in poolDictionary.Values)
        {
            foreach (var obj in pool)
            {
                if (obj != null) Destroy(obj);
            }
        }

        // 2. 관리용 딕셔너리 데이터 전체 초기화
        poolDictionary.Clear();
        prefabDictionary.Clear();
        groupDictionary.Clear();
        UnityEngine.Debug.Log("[PoolManager] 모든 풀이 성공적으로 초기화되었습니다.");
    }
}
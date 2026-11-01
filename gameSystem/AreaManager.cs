using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// [3단계] area(= 씬 안의 "area" 루트) 전환 관리.
// Enter(씬, 스폰): 그 씬이 없으면 additive 로드 → 그 area만 켜고 나머지 area는 끔 → 플레이어를 스폰으로 이동.
// 포탈/플레이어 상주화는 아직 안 건드림(현재 플레이에 영향 없음). ContextMenu로 전환만 테스트.
public class AreaManager : MonoBehaviour
{
    public static AreaManager Instance;

    [Header("현재 활성 area 씬 이름 (읽기용)")]
    public string currentArea;

    [Header("테스트 — ContextMenu로 호출")]
    public string testScene = "level1";
    public string testSpawn = "LV1_PlayerSpawn";
    [Space]
    public string returnScene = "tutorial";
    public string returnSpawn = "afterLV1_PlayerSpawn";

    // 각 씬에서 켰다 껐다 할 루트 오브젝트 이름 (area 밖의 player/RoomManager는 안 건드림)
    private const string AreaRootName = "area";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ContextMenu("▶ Enter (testScene)")]
    private void EnterTest() => Enter(testScene, testSpawn);

    [ContextMenu("◀ Enter (returnScene)")]
    private void ReturnTest() => Enter(returnScene, returnSpawn);

    // area 씬으로 입장
    public void Enter(string sceneName, string spawnPointName)
    {
        StartCoroutine(EnterRoutine(sceneName, spawnPointName));
    }

    private IEnumerator EnterRoutine(string sceneName, string spawnPointName)
    {
        // 1. 안 떠 있으면 additive 로드
        Scene sc = SceneManager.GetSceneByName(sceneName);
        if (!sc.IsValid() || !sc.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[AreaManager] '{sceneName}' 로드 실패 — Build Settings 확인.");
                yield break;
            }
            yield return op;
            yield return null;   // 로드된 씬의 Room.Start(등록)·RoomManager 재바인딩 대기
            sc = SceneManager.GetSceneByName(sceneName);
        }

        // 2. 로드된 모든 area 중 target만 켜고 나머지 끔
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            GameObject areaRoot = FindAreaRoot(s);
            if (areaRoot != null) areaRoot.SetActive(s.name == sceneName);
        }

        // 3. 플레이어를 스폰으로 이동
        Transform spawn = FindInScene(sc, spawnPointName);
        if (spawn == null)
            Debug.LogWarning($"[AreaManager] '{sceneName}'에서 스폰 '{spawnPointName}'을 못 찾음.");
        else
            MovePlayer(spawn);

        currentArea = sceneName;
        Debug.Log($"[AreaManager] Enter '{sceneName}' 완료 (spawn={spawnPointName}).");
    }

    // 씬의 루트들 중 "area" 이름을 가진 것 (비활성이어도 GetRootGameObjects에 잡힘)
    private GameObject FindAreaRoot(Scene s)
    {
        foreach (var root in s.GetRootGameObjects())
            if (root.name == AreaRootName) return root;
        return null;
    }

    // 씬 전체(자식·비활성 포함)에서 이름으로 Transform 찾기
    private Transform FindInScene(Scene s, string objectName)
    {
        foreach (var root in s.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t;
        return null;
    }

    private void MovePlayer(Transform spawn)
    {
        var p = Player.Instance;
        if (p == null) { Debug.LogWarning("[AreaManager] Player.Instance 없음."); return; }

        var rb = p.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = spawn.position;
            rb.rotation = spawn.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        p.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        // 새 위치의 방을 현재 방으로 → roomChanged 발행 → RoomManager가 활성 방 갱신
        Room room = FindRoomAt(spawn.position);
        if (room != null) p.SetRoom(room);
    }

    private Room FindRoomAt(Vector3 pos)
    {
        if (RoomManager.Instance == null) return null;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null) continue;
            Bounds? b = r.homeBound != null ? r.homeBound.bounds : (Bounds?)null;
            if (b.HasValue && b.Value.Contains(pos)) return r;
        }
        return null;
    }
}

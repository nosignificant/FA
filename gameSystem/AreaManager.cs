using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// [3단계] area(= 씬 안의 "area" 루트) 전환 관리.
// Enter(씬, 스폰): 그 씬이 없으면 additive 로드 → 그 area만 켜고 나머지 area는 끔 → 플레이어를 스폰으로 이동.
public class AreaManager : MonoBehaviour
{
    public static AreaManager Instance;

    [Header("현재 활성 area 씬 이름 (읽기용)")]
    public string currentArea;

    [Header("전환 로딩 커버")]
    [Tooltip("area 전환 시 로딩 패널(SceneLoader)을 최소 이 시간(초) 띄움 — 즉시 토글이어도 상태 패널을 읽을 수 있게. 0이면 끔")]
    public float transitionCoverTime = 4f;

    // 각 씬에서 켰다 껐다 할 루트 오브젝트 이름 (area 밖의 player/RoomManager는 안 건드림)
    private const string AreaRootName = "area";

    // 씬별 '도착 시 바라볼 방향'. 씬에 배치된 Player(중복)가 파괴되기 직전 자기 회전을 등록해둠.
    private readonly Dictionary<string, Quaternion> _sceneFacing = new();
    public void RegisterSceneFacing(string sceneName, Quaternion rot) => _sceneFacing[sceneName] = rot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // area 씬으로 입장
    public void Enter(string sceneName, string spawnPointName)
    {
        StartCoroutine(EnterRoutine(sceneName, spawnPointName));
    }

    private IEnumerator EnterRoutine(string sceneName, string spawnPointName)
    {
        // 전환 동안 로딩 커버를 띄워 area 토글·플레이어 이동이 화면에 그대로 보이지 않게 함
        var loader = transitionCoverTime > 0f ? SceneLoader.Instance : null;
        if (loader != null) loader.ShowCover();
        float coverStart = Time.unscaledTime;

        // 1. 안 떠 있으면 additive 로드
        Scene sc = SceneManager.GetSceneByName(sceneName);
        if (!sc.IsValid() || !sc.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[AreaManager] '{sceneName}' 로드 실패 — Build Settings 확인.");
                if (loader != null) loader.HideCover();
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

        // 2.5 씬마다 딸려온 중복 카메라/오디오리스너를 끈다 — 깊이 같은 카메라가 겹쳐 렌더되면
        //  "같은 뷰인데 빛만 깜빡"거리고, AudioListener 중복 경고도 난다. (미니맵 등 RT 카메라는 유지)
        DeduplicateCameras();

        // 3. 플레이어를 스폰으로 이동
        Transform spawn = FindInScene(sc, spawnPointName);
        if (spawn == null)
            Debug.LogWarning($"[AreaManager] '{sceneName}'에서 스폰 '{spawnPointName}'을 못 찾음.");
        else
            MovePlayer(spawn);

        currentArea = sceneName;
        Debug.Log($"[AreaManager] Enter '{sceneName}' 완료 (spawn={spawnPointName}).");

        // 커버 최소 표시시간 보장 후 내림 (즉시 토글이어도 패널이 잠깐 보이게)
        if (loader != null)
        {
            float elapsed = Time.unscaledTime - coverStart;
            if (elapsed < transitionCoverTime)
                yield return new WaitForSecondsRealtime(transitionCoverTime - elapsed);
            loader.HideCover();
        }
    }

    // 플레이어(persistent) 카메라 하나만 화면 렌더로 남기고, 씬마다 딸려온 중복 Main Camera를 끈다.
    //  RenderTexture로 그리는 카메라(미니맵 등)는 화면과 무관하므로 건드리지 않는다.
    private void DeduplicateCameras()
    {
        Camera keep = (Player.Instance != null && Player.Instance.pl != null)
            ? Player.Instance.pl.cam : Camera.main;

        foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (cam == null || cam == keep) continue;
            if (cam.targetTexture != null) continue;   // 미니맵 등 RT 전용 카메라는 유지
            cam.enabled = false;
            var al = cam.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
        }
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

        // PlayerControl이 매 프레임 내부 yaw로 transform 회전을 덮어쓰므로, 도착 방향을 그 yaw에 반영.
        // 씬에 배치한 Player의 방향이 등록돼 있으면 그걸 우선, 없으면 스폰 포인트 회전 사용.
        var pc = p.GetComponent<PlayerControl>();
        if (pc != null)
        {
            float yaw = _sceneFacing.TryGetValue(spawn.gameObject.scene.name, out var rot)
                ? rot.eulerAngles.y
                : spawn.eulerAngles.y;
            pc.SetLookYaw(yaw);
        }

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

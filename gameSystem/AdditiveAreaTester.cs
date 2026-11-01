using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// [임시 검증용 — 2단계] additive 로드 시 RoomManager.allRooms가 누적되는지,
// 그리고 로드한 씬을 비활성화(SetActive false)해도 방이 계속 카운트되는지 확인한다.
// 사용법: 씬의 아무 오브젝트에 붙이고 sceneToLoad에 레벨 씬 이름 입력 →
//         인스펙터 컴포넌트 우클릭 메뉴에서 "1) Load Additive" / "2) Toggle Loaded Active" 실행.
// 게임 입력은 안 건드림(규칙 준수). 확인 끝나면 이 파일 삭제.
public class AdditiveAreaTester : MonoBehaviour
{
    [Tooltip("additive로 겹쳐 로드할 씬 이름 (예: level1)")]
    public string sceneToLoad = "level1";

    private Scene _loaded;

    [ContextMenu("1) Load Additive")]
    public void LoadAdditive()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        LogCounts("로드 전");

        var op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[AreaTest] '{sceneToLoad}' 로드 실패 — Build Settings에 있는지/이름 확인.");
            yield break;
        }
        yield return op;
        yield return null;   // 로드된 씬의 Room.Start(등록)·RoomManager 재바인딩이 끝나길 한 프레임 대기
        yield return null;

        _loaded = SceneManager.GetSceneByName(sceneToLoad);
        LogCounts($"'{sceneToLoad}' additive 로드 후");
    }

    [ContextMenu("2) Toggle Loaded Active")]
    public void ToggleLoadedActive()
    {
        if (!_loaded.IsValid() || !_loaded.isLoaded)
        {
            Debug.LogWarning("[AreaTest] 먼저 'Load Additive'로 씬을 로드하세요.");
            return;
        }

        bool anyActive = false;
        foreach (var root in _loaded.GetRootGameObjects())
            if (root.activeSelf) { anyActive = true; break; }

        foreach (var root in _loaded.GetRootGameObjects())
            root.SetActive(!anyActive);   // 켜져 있으면 전부 끄고, 꺼져 있으면 전부 켬

        LogCounts(anyActive ? $"'{sceneToLoad}' 비활성화(꺼짐) 후" : $"'{sceneToLoad}' 활성화(켜짐) 후");
    }

    private void LogCounts(string tag)
    {
        var rm = RoomManager.Instance;
        if (rm == null) { Debug.Log($"[AreaTest] {tag}: RoomManager 없음"); return; }

        int l = 0, a = 0, none = 0;
        foreach (var r in rm.allRooms)
        {
            if (r == null) continue;
            switch (r.Activation)
            {
                case Room.RoomActivation.L: l++; break;
                case Room.RoomActivation.A: a++; break;
                default: none++; break;
            }
        }
        Debug.Log($"[AreaTest] {tag}: allRooms={rm.allRooms.Count} (L={l} A={a} None={none})");
    }
}

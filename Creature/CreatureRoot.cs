using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬별 _Creatures 컨테이너. 스포너와 같은 씬의 것을 반환(없으면 그 씬에 생성).
// additive-area 모델: 전역 하나면 레벨 생성물이 area와 안 묶여 orphan이 누적됨 → 씬(=area)별로 분리한다.
// 씬 안에서 _Creatures를 area 밑에 미리 두면 그걸 찾아 쓰고, area와 함께 켜고/꺼진다.
public class CreatureRoot : MonoBehaviour
{
    private static readonly Dictionary<Scene, Transform> _byScene = new();

    // 스포너(context)와 같은 씬의 컨테이너. context 없으면 활성 씬.
    public static Transform ContainerFor(Component context)
    {
        Scene scene = context != null ? context.gameObject.scene : SceneManager.GetActiveScene();
        return ContainerForScene(scene);
    }

    public static Transform ContainerForScene(Scene scene)
    {
        if (_byScene.TryGetValue(scene, out var cached) && cached != null) return cached;

        // 그 씬에 이미 있는 _Creatures(예: area 밑에 배치)를 찾음 — 비활성(꺼진 area)도 포함
        foreach (var r in FindObjectsByType<CreatureRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r != null && r.gameObject.scene == scene)
            {
                _byScene[scene] = r.transform;
                return r.transform;
            }

        // 없으면 그 씬에 생성 (가능하면 해당 씬으로 이동 — 안 그러면 활성 씬에 생김)
        var go = new GameObject("_Creatures");
        if (scene.IsValid() && scene.isLoaded) SceneManager.MoveGameObjectToScene(go, scene);

        // 그 씬의 area 루트 밑에 두면 area와 함께 켜고/꺼지고 정리됨. 없으면 씬 루트.
        if (scene.IsValid() && scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "area") { go.transform.SetParent(root.transform, true); break; }

        go.AddComponent<CreatureRoot>();
        _byScene[scene] = go.transform;
        return go.transform;
    }
}

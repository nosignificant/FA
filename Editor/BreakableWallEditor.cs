using UnityEngine;
using UnityEditor;

// isWindow 켜고 버튼 누르면 에디터에서도 창문 프리팹을 생성/미리보기.
// 생성된 창문은 window 슬롯에 들어가 런타임 중복 생성을 막음.
[CustomEditor(typeof(BreakableWall))]
public class BreakableWallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var bw = (BreakableWall)target;

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!bw.isWindow))
        {
            if (GUILayout.Button("창문 생성 / 갱신"))
                CreateOrRefreshWindow(bw);
        }
        if (GUILayout.Button("창문 제거"))
            RemoveWindow(bw);
    }

    private void CreateOrRefreshWindow(BreakableWall bw)
    {
        if (bw.windowPrefabs == null || bw.windowPrefabs.Length == 0)
        {
            EditorUtility.DisplayDialog("창문 없음", "Window Prefabs가 비어 있습니다.", "확인");
            return;
        }
        int idx = Mathf.Clamp(bw.windowIndex, 0, bw.windowPrefabs.Length - 1);
        var prefab = bw.windowPrefabs[idx];
        if (prefab == null) return;

        RemoveWindow(bw);   // 기존 것 있으면 지우고 새로

        Transform parent = bw.windowParent != null ? bw.windowParent : bw.transform;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = bw.transform.position;
        go.transform.rotation = bw.transform.rotation;
        go.transform.localScale = Vector3.one * 0.05f;   // 창문 스케일
        Undo.RegisterCreatedObjectUndo(go, "Create Window");

        Undo.RecordObject(bw, "Assign Window");
        bw.window = go;
        go.SetActive(bw.isWindow);

        // 미리보기: 창문이면 일반 벽 메쉬 끄기
        if (bw.normalWall != null)
        {
            var mr = bw.normalWall.GetComponent<MeshRenderer>();
            if (mr != null) { Undo.RecordObject(mr, "Toggle Wall Mesh"); mr.enabled = !bw.isWindow; }
        }

        EditorUtility.SetDirty(bw);
    }

    private void RemoveWindow(BreakableWall bw)
    {
        if (bw.window == null) return;
        Undo.RecordObject(bw, "Remove Window");
        var go = bw.window;
        bw.window = null;
        Undo.DestroyObjectImmediate(go);

        if (bw.normalWall != null)   // 벽 메쉬 다시 켜기
        {
            var mr = bw.normalWall.GetComponent<MeshRenderer>();
            if (mr != null) { Undo.RecordObject(mr, "Toggle Wall Mesh"); mr.enabled = !bw.breakable; }
        }
        EditorUtility.SetDirty(bw);
    }
}

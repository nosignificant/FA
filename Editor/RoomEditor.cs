using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    private const string ConfigPath = "Assets/script/Editor/RoomEditorConfig.asset";
    private const float OverlapEpsilon = 0.5f;

    private RoomEditorConfig GetConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<RoomEditorConfig>(ConfigPath);
        if (config == null)
            Debug.LogWarning($"[RoomEditor] RoomEditorConfig가 없습니다. 우클릭 → Create/Room/Editor Config 로 만들고 {ConfigPath} 에 두세요.");
        return config;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room Editor", EditorStyles.boldLabel);

        RoomEditorConfig config = GetConfig();
        if (config != null)
        {
            EditorGUI.BeginChangeCheck();
            config.roomPrefab = (GameObject)EditorGUILayout.ObjectField("Room Prefab", config.roomPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(config);
        }
        else
        {
            EditorGUILayout.HelpBox($"RoomEditorConfig.asset을 {ConfigPath} 에 생성해주세요.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        Room room = (Room)target;
        foreach (var dir in DirectionExt.All)
        {
            if (GUILayout.Button($"Add {dir} room"))
                AddRoom(room, dir);
        }

        EditorGUILayout.Space();
        DrawDoorControls(room);

        EditorGUILayout.Space();
        if (GUILayout.Button("Spawn Creatures (spawnEntries대로 즉시 스폰)"))
            SpawnCreatures(room);
        if (GUILayout.Button("Clear Creatures (방 안 생물 전부 제거)"))
            ClearCreatures(room);

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Reset Walls (벽 전부 초기 상태로)"))
            ResetWalls(room);
        GUI.backgroundColor = Color.white;
    }

    void DrawDoorControls(Room room)
    {
        EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);

        foreach (var slot in room.wallSlots)
        {
            Door d = slot.wall != null ? slot.wall.door : null;
            if (d == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{slot.direction}", GUILayout.Width(30));

            // 조건 옵저빙 생물
            EditorGUI.BeginChangeCheck();
            CreatureData newObs = (CreatureData)EditorGUILayout.ObjectField(
                d.roomCondition.observingC, typeof(CreatureData), false, GUILayout.MinWidth(80));
            int newCount = EditorGUILayout.IntField(d.roomCondition.howManyMore, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(d, "Edit Door Condition");
                var cond = d.roomCondition;
                cond.observingC = newObs;
                cond.howManyMore = newCount;
                d.roomCondition = cond;
                EditorUtility.SetDirty(d);
            }

            // 강제 열기/닫기 버튼
            GUI.backgroundColor = d.isOpen ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button(d.isOpen ? "Open ▶ Close" : "Closed ▶ Open", GUILayout.Width(110)))
            {
                Undo.RecordObject(d, "Toggle Door");
                if (Application.isPlaying)
                    d.DoorCloseAndOpen(!d.isOpen);
                else
                    d.isOpen = !d.isOpen;   // 에디터 모드에선 상태만 토글
                EditorUtility.SetDirty(d);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }

    void ClearCreatures(Room room)
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clear Creatures");

        // 자식 중 Creature 컴포넌트 가진 것만 제거 (Door, Wall 등은 보존)
        var toRemove = new System.Collections.Generic.List<GameObject>();
        foreach (var c in room.GetComponentsInChildren<Creature>(true))
        {
            // Door의 self Creature는 보존
            if (c.GetComponent<Door>() != null) continue;
            toRemove.Add(c.gameObject);
        }

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(room);
    }

    void SpawnCreatures(Room room)
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Spawn Creatures");

        // 스폰 전 자식 수 기억
        int before = room.transform.childCount;
        room.SpawnInitialCreatures();
        int after = room.transform.childCount;

        // 새로 생긴 자식들 Undo에 등록
        for (int i = before; i < after; i++)
            Undo.RegisterCreatedObjectUndo(room.transform.GetChild(i).gameObject, "Spawn Creatures");

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(room);
    }

    void ResetWalls(Room room)
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Reset Room Walls");

        Undo.RecordObject(room, "Reset Room Walls");

        // 모든 wall의 mesh + 연결된 door 기록
        foreach (var slot in room.wallSlots)
        {
            Wall w = slot.wall;
            if (w == null) continue;
            if (w.noDoorMesh  != null) Undo.RecordObject(w.noDoorMesh,  "Reset Room Walls");
            if (w.yesDoorMesh != null) Undo.RecordObject(w.yesDoorMesh, "Reset Room Walls");
        }
        foreach (var d in room.doors)
            if (d != null) Undo.RecordObject(d, "Reset Room Walls");

        room.ResetWalls();

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(room);
    }

    void OnSceneGUI()
    {
        Room room = (Room)target;
        Vector3 c = room.transform.position;
        Vector3 half = room.roomSize * 0.5f;

        foreach (var dir in DirectionExt.All)
        {
            Vector3 pos = c + Vector3.Scale(dir.ToVec(), half + Vector3.one * 1.5f);
            if (FindRoomNear(pos) != null) continue;

            float h = HandleUtility.GetHandleSize(pos) * 0.3f;
            Handles.color = Color.cyan;
            if (Handles.Button(pos, Quaternion.identity, h, h, Handles.SphereHandleCap))
            {
                AddRoom(room, dir);
            }
        }
    }

    Room FindRoomNear(Vector3 pos)
    {
        var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
        foreach (var r in rooms)
        {
            Bounds b = new Bounds(r.transform.position, r.roomSize);
            if (b.Contains(pos)) return r;
        }
        return null;
    }

    void AddRoom(Room from, Direction dir)
    {
        RoomEditorConfig config = GetConfig();
        if (config == null || config.roomPrefab == null) { Debug.LogError("[RoomEditor] RoomEditorConfig에 Room Prefab을 등록해주세요."); return; }
        GameObject roomPrefab = config.roomPrefab;

        // Undo 그룹 묶기: 한 번의 Ctrl+Z로 전부 되돌림
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Add Adjacent Room");

        // 1. 새 방 생성
        GameObject newGO = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
        Undo.RegisterCreatedObjectUndo(newGO, "Add Room");

        Room newRoom = newGO.GetComponent<Room>();

        Vector3 offset = Vector3.Scale(dir.ToVec(), (from.roomSize + newRoom.roomSize) * 0.5f);
        newGO.transform.position = from.transform.position + offset;
        newGO.transform.rotation = Quaternion.identity;
        newGO.name = $"Room_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
        newRoom.roomID = newGO.name;

        // 2. from쪽에만 문 달고, newRoom쪽 벽은 통째로 숨김
        Wall wallA = from.GetWall(dir);
        Wall wallB = newRoom.GetWall(dir.Opposite());

        // from 방과 양쪽 Wall의 mesh 변경을 Undo에 기록
        Undo.RecordObject(from, "Add Adjacent Room");
        if (wallA != null)
        {
            if (wallA.noDoorMesh  != null) Undo.RecordObject(wallA.noDoorMesh,  "Add Adjacent Room");
            if (wallA.yesDoorMesh != null) Undo.RecordObject(wallA.yesDoorMesh, "Add Adjacent Room");
        }
        if (wallB != null)
        {
            if (wallB.noDoorMesh  != null) Undo.RecordObject(wallB.noDoorMesh,  "Add Adjacent Room");
            if (wallB.yesDoorMesh != null) Undo.RecordObject(wallB.yesDoorMesh, "Add Adjacent Room");
        }

        from.SetDoorWall(dir, true);
        wallB?.Hide();

        // 3. 그 하나의 Door를 양쪽 방이 공유
        Door door = wallA?.door;
        if (door != null)
        {
            Undo.RecordObject(door, "Add Adjacent Room");
            door.roomA = from;
            door.roomB = newRoom;
            if (!from.doors.Contains(door))    from.doors.Add(door);
            if (!newRoom.doors.Contains(door)) newRoom.doors.Add(door);
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(from);
        EditorUtility.SetDirty(newRoom);

        Selection.activeGameObject = newGO;
    }
}

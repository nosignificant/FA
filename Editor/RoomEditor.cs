using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    private const string RoomPrefabPath = "Assets/Prefabs/Room.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Door.prefab";
    private const float OverlapEpsilon = 0.5f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Room room = (Room)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Adjacent Room", EditorStyles.boldLabel);

        foreach (var dir in DirectionExt.All)
        {
            if (GUILayout.Button($"Add {dir} room"))
            {
                AddRoom(room, dir);
            }
        }
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
        var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
        if (roomPrefab == null)
        {
            Debug.LogError($"[RoomEditor] Room prefab not found at {RoomPrefabPath}");
            return;
        }
        var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        if (doorPrefab == null)
        {
            Debug.LogError($"[RoomEditor] Door prefab not found at {DoorPrefabPath}");
            return;
        }

        // 1. 새 방 생성
        GameObject newGO = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
        Undo.RegisterCreatedObjectUndo(newGO, "Add Room");

        Room newRoom = newGO.GetComponent<Room>();

        // 가변 크기 처리: 두 방의 절반 크기 합만큼 띄움 → 경계가 맞닿음
        Vector3 offset = Vector3.Scale(
            dir.ToVec(),
            (from.roomSize + newRoom.roomSize) * 0.5f
        );
        newGO.transform.position = from.transform.position + offset;
        newGO.transform.rotation = Quaternion.identity;
        newGO.name = $"Room_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
        newRoom.roomID = newGO.name;

        // 2. 양쪽 Door 생성/연결
        Door doorA = SpawnDoor(doorPrefab, from, dir, newRoom);
        Door doorB = SpawnDoor(doorPrefab, newRoom, dir.Opposite(), from);

        if (doorA != null && !from.doors.Contains(doorA)) from.doors.Add(doorA);
        if (doorB != null && !newRoom.doors.Contains(doorB)) newRoom.doors.Add(doorB);

        EditorUtility.SetDirty(from);
        EditorUtility.SetDirty(newRoom);

        Selection.activeGameObject = newGO;
    }

    Door SpawnDoor(GameObject doorPrefab, Room owner, Direction dir, Room target)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab, owner.transform);
        Undo.RegisterCreatedObjectUndo(go, "Add Door");

        Transform slot = owner.GetSlot(dir);
        if (slot != null)
        {
            go.transform.position = slot.position;
            go.transform.rotation = slot.rotation;
        }
        else
        {
            // 슬롯 없으면 방 가장자리 중앙
            Vector3 edge = owner.transform.position +
                           Vector3.Scale(dir.ToVec(), owner.roomSize * 0.5f);
            go.transform.position = edge;
        }

        Door d = go.GetComponent<Door>();
        if (d != null)
        {
            d.room = owner;
            d.nextRoom = target;
        }
        return d;
    }
}

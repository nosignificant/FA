using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CreatureTypes;

// 미니맵에 방 하나를 나타내는 UI. MiniMap이 방마다 하나씩 Instantiate.
public class MiniMapRoom : MonoBehaviour
{
    public Room connectRoom;   // 이 UI가 가리키는 실제 방
    public string roomID;

    [Header("UI")]
    public Image box;              // 방 박스 (색으로 상태 표시)
    public TMP_Text roomTMP;       // 방 이름
    public TMP_Text roomStateTMP;  // 우세종

    [Header("색")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.25f);   // 비활성
    public Color activeColor = new Color(0.6f, 1f, 0.4f, 0.6f); // 활성(플레이어와 연결)
    public Color playerColor = new Color(1f, 0.85f, 0.2f, 0.9f); // 플레이어 현재 방

    [Header("문 (4방향) — 존재하는 방향만 켜짐")]
    public GameObject doorN;
    public GameObject doorS;
    public GameObject doorE;
    public GameObject doorW;
    [Tooltip("문 오브젝트에 Image가 있으면 열림/닫힘 색 적용")]
    public Color doorClosedColor = new Color(1f, 1f, 1f, 0.5f);
    public Color doorOpenColor = new Color(0.4f, 1f, 0.4f, 1f);

    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (box == null) box = GetComponent<Image>();
    }

    public void Bind(Room room)
    {
        connectRoom = room;
        roomID = room != null ? room.roomID : "";
        if (roomTMP != null) roomTMP.text = roomID;
        SetupDoors();
    }

    // 방에 실제로 있는 방향의 문만 켬 (N/S/E/W)
    private void SetupDoors()
    {
        SetDoorActive(doorN, Direction.N);
        SetDoorActive(doorS, Direction.S);
        SetDoorActive(doorE, Direction.E);
        SetDoorActive(doorW, Direction.W);
    }

    private void SetDoorActive(GameObject go, Direction dir)
    {
        if (go == null) return;
        go.SetActive(connectRoom != null && connectRoom.GetDoor(dir) != null);
    }

    // MiniMap이 계산한 위치·크기로 배치
    public void SetLayout(Vector2 anchoredPos, Vector2 size)
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    // 매 주기 갱신 — 우세종 텍스트 + 상태 색
    public void Refresh(Room playerRoom)
    {
        if (connectRoom == null) return;

        if (roomStateTMP != null)
        {
            CreatureData best = connectRoom.MostNumerousSpecies();
            roomStateTMP.text = best != null ? best.creatureName : "-";
        }

        if (box != null)
        {
            if (connectRoom == playerRoom) box.color = playerColor;
            else if (connectRoom.isActive) box.color = activeColor;
            else box.color = idleColor;
        }

        // 문 열림/닫힘 색
        RefreshDoor(doorN, Direction.N);
        RefreshDoor(doorS, Direction.S);
        RefreshDoor(doorE, Direction.E);
        RefreshDoor(doorW, Direction.W);
    }

    private void RefreshDoor(GameObject go, Direction dir)
    {
        if (go == null || !go.activeSelf || connectRoom == null) return;
        Door d = connectRoom.GetDoor(dir);
        if (d == null) return;

        // 색: 열림/닫힘
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = d.isOpen ? doorOpenColor : doorClosedColor;

        // 조건 텍스트 (프리팹의 condition TMP)
        TMP_Text cond = go.GetComponentInChildren<TMP_Text>();
        if (cond != null) cond.text = d.ConditionLabel();
    }
}

using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Direction")]
    public Direction direction;

    [Header("Meshes")]
    public GameObject noDoorMesh;
    public GameObject yesDoorMesh;

    [Header("Door (비워두면 yesDoorMesh에서 자동 탐색)")]
    [SerializeField] private Door _door;

    public Door door
    {
        get
        {
            if (_door == null && yesDoorMesh != null)
                _door = yesDoorMesh.GetComponentInChildren<Door>(true); // 비활성 포함
            return _door;
        }
    }


    public void SetDoor(bool hasDoor)
    {
        if (noDoorMesh != null) noDoorMesh.SetActive(!hasDoor);
        if (yesDoorMesh != null) yesDoorMesh.SetActive(hasDoor);
    }

    /// <summary>연결된 쪽: 문 슬롯의 noDoorMesh·yesDoorMesh 둘 다 off → 그 칸만 빈 구멍(통로). 나머지 벽은 그대로.</summary>
    public void Hide()
    {
        if (noDoorMesh != null) noDoorMesh.SetActive(false);
        if (yesDoorMesh != null) yesDoorMesh.SetActive(false);
        // SetActive(false)면 콜라이더도 같이 꺼져 통과 가능.
    }

    public bool HasDoor =>
        yesDoorMesh != null && yesDoorMesh.activeSelf;
}

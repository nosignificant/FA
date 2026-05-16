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

    private void Awake()
    {
        SetDoor(false);
    }

    public void SetDoor(bool hasDoor)
    {
        if (noDoorMesh != null) noDoorMesh.SetActive(!hasDoor);
        if (yesDoorMesh != null) yesDoorMesh.SetActive(hasDoor);
    }

    /// <summary>옆 방과 붙어있어서 벽 자체가 필요 없을 때 통째로 숨김</summary>
    public void Hide()
    {
        if (noDoorMesh != null) noDoorMesh.SetActive(false);
        if (yesDoorMesh != null) yesDoorMesh.SetActive(false);
    }

    public bool HasDoor =>
        yesDoorMesh != null && yesDoorMesh.activeSelf;
}

using System.Collections;
using UnityEngine;

// 떠다니는 장식 particle 등: 자기 '위치'가 속한 방의 활성화 상태(L/A/None) 색을 따라감.
// Creature가 아니라 방 생물 수에 안 잡히고, 방 자식으로 등록할 필요도 없음.
public class RoomTintByPosition : MonoBehaviour
{
    [Tooltip("색 바꿀 렌더러들 (비우면 이 오브젝트+자식의 모든 Renderer 자동)")]
    public Renderer[] renderers;
    [Tooltip("셰이더 색 프로퍼티 override. 비우면 자동(_BaseColor/_Color, SpriteRenderer는 .color)")]
    public string colorProperty = "";

    public Color noneColor = Color.white;
    public Color lColor = new Color(0.6f, 1f, 0.3f);
    public Color aColor = new Color(0.9f, 0.2f, 0.2f);

    [Tooltip("방 판정·색 갱신 주기(초). 움직이는 particle이면 낮게.")]
    public float interval = 0.3f;

    private Renderer[] _resolved;
    private Room.RoomActivation _lastState = (Room.RoomActivation)(-1);
    private Room _lastRoom;

    private void Start() => StartCoroutine(Loop());

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            Room room = FindRoom();
            var state = room != null ? room.Activation : Room.RoomActivation.None;

            if (room != _lastRoom || state != _lastState)   // 방이나 상태 바뀔 때만 적용
            {
                Apply(state);
                _lastRoom = room;
                _lastState = state;
            }
            yield return wait;
        }
    }

    private void Apply(Room.RoomActivation state)
    {
        var rs = Resolve();
        Color col = state == Room.RoomActivation.A ? aColor
                  : state == Room.RoomActivation.L ? lColor : noneColor;
        ActivationColor.Apply(rs, col, colorProperty);
    }

    // 위치가 속한 방 (bounds 기준). 없으면 null.
    private Room FindRoom()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return null;
        Vector3 pos = transform.position;
        foreach (var kv in RoomManager.Instance.rooms)
        {
            var r = kv.Value;
            if (r == null || r.homeBound == null) continue;
            if (r.homeBound.bounds.Contains(pos)) return r;
        }
        return null;
    }

    private Renderer[] Resolve()
    {
        if (_resolved != null) return _resolved;
        _resolved = (renderers != null && renderers.Length > 0)
            ? renderers
            : GetComponentsInChildren<Renderer>(true);
        return _resolved;
    }
}

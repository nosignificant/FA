using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using CreatureTypes;

public class PlayerLockOn : MonoBehaviour
{
    [Header("ref")]
    public Camera cam;
    public Creature targetCreature;
    private Collider tc;
    private Collider c;

    [Header("설정")]
    public float rotationSpeed = 5f;
    public Vector3 aimOffset = new Vector3(0, 1.0f, 0);

    public event Action<Creature> targetChanged;

    // 사이클링용
    public List<Creature> candidates = new();
    private int currentIndex = -1;
    // 이미 cycle에서 본 생물들 (한 사이클 내에서 중복 방지)
    private readonly HashSet<Creature> visited = new();

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }
    public bool TryLock()
    {
        //플레이어 있는 방에서만 락온
        Room r = Player.Instance.currentRoom;
        if (r == null) { Debug.Log("[LockOn] currentRoom null"); return false; }

        BuildCandidateList(r);
        if (candidates.Count == 0) return false;

        // 새 사이클 시작 → 방문기록 리셋
        visited.Clear();
        Creature first = candidates[0];
        visited.Add(first);
        currentIndex = 0;
        SetTarget(first);
        return true;
    }

    // 다음 후보로 순환 (Tab 재입력) — 안 본 생물 우선
    public bool CycleNext()
    {
        Room r = Player.Instance.currentRoom;
        if (r != null) BuildCandidateList(r);

        if (candidates.Count == 0) { Unlock(); return false; }

        // 아직 visited에 없는 첫 후보 찾기
        Creature pick = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!visited.Contains(candidates[i])) { pick = candidates[i]; break; }
        }

        // 다 봤으면 visited 리셋하고 다시 첫 번째부터
        if (pick == null)
        {
            visited.Clear();
            pick = candidates[0];
        }

        visited.Add(pick);
        currentIndex = candidates.IndexOf(pick);
        SetTarget(pick);
        return true;
    }

    public void Unlock()
    {
        targetCreature = null;
        candidates.Clear();
        currentIndex = -1;
        visited.Clear();
        targetChanged?.Invoke(null);
    }

    private void SetTarget(Creature target)
    {
        targetCreature = target;
        targetChanged?.Invoke(targetCreature);
        LookAtTarget();
    }

    // 거리 기준으로 정렬된 후보 리스트 빌드
    private void BuildCandidateList(Room room)
    {
        candidates.Clear();
        if (cam == null || room == null) return;

        Vector3 playerPos = transform.position;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        List<Creature> lockables = new();
        if (room.creatureList != null)
            lockables.AddRange(room.creatureList);

        if (room.doors != null)
        {
            foreach (var door in room.doors)
            {
                if (door != null && door.self != null && !lockables.Contains(door.self))
                    lockables.Add(door.self);
            }
        }

        candidates = lockables
            .Where(c => c != null && c.data != null && c.data.creatureID != CreatureID.Player)
            .Where(c =>
            {
                Vector3 sp = cam.WorldToScreenPoint(GetLockPosition(c));
                return sp.z > 0f; // 카메라 앞만
            })
            .OrderBy(c =>
            {
                Vector3 lockPos = GetLockPosition(c);
                Vector3 sp = cam.WorldToScreenPoint(lockPos);
                float worldDist = (lockPos - playerPos).magnitude;
                float screenDist = ((Vector2)sp - screenCenter).magnitude;
                return worldDist + screenDist * 0.01f;
            })
            .ToList();
    }

    private void LateUpdate()
    {
        if (Player.Instance != null && Player.Instance.isTracking && targetCreature != null)
            LookAtTarget();

        if (targetCreature == null) Player.Instance.isTracking = false;
    }

    private void LookAtTarget()
    {
        if (targetCreature == null || cam == null) return;

        Vector3 targetPos = GetLockPosition(targetCreature) + aimOffset;
        Vector3 dir = targetPos - cam.transform.position;

        if (dir.sqrMagnitude <= 0.000001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        cam.transform.rotation = Quaternion.Slerp(
            cam.transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private Vector3 GetLockPosition(Creature creature)
    {
        if (creature == null) return Vector3.zero;

        Collider lockCollider = creature.mainCollider != null
            ? creature.mainCollider
            : creature.GetComponentInChildren<Collider>();

        if (lockCollider != null)
            return lockCollider.bounds.center;

        return creature.transform.position;
    }
}

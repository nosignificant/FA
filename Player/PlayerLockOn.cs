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
    [Tooltip("이 반경 안의 생물은 방이 달라도 락온 후보에 포함")]
    public float lockRadius = 30f;

    [Tooltip("락온 대상 떨림 보정 시간(클수록 부드럽지만 반응 느림). 0이면 보정 없음")]
    public float aimSmoothTime = 0.12f;
    [Tooltip("카메라 회전 보정 시간")]
    public float rotationSmoothTime = 0.1f;
    private Vector3 smoothedAimPos;
    private Vector3 aimVelocity;
    private Vector3 rotationVelocity;
    private bool aimInitialized = false;

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
        aimInitialized = false;   // 새 대상으로 바꾸면 보정값 스냅
        targetChanged?.Invoke(targetCreature);
        LookAtTarget();
    }

    /// <summary>특정 생물로 강제 락온 (조종 중 고정용)</summary>
    public void ForceLock(Creature target)
    {
        if (target == null) return;
        SetTarget(target);
        if (Player.Instance != null) Player.Instance.isTracking = true;
    }

    // 거리 기준으로 정렬된 후보 리스트 빌드
    private void BuildCandidateList(Room room)
    {
        candidates.Clear();
        if (cam == null || room == null) return;

        Vector3 playerPos = transform.position;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        List<Creature> lockables = new();

        // 현재 방 + 문 생물
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

        // 반경 안의 생물은 방이 달라도 후보에 추가 (CreatureScanner와 동일 방식)
        Collider[] hits = Physics.OverlapSphere(playerPos, lockRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Creature c = hits[i].GetComponentInParent<Creature>();
            if (c == null || lockables.Contains(c)) continue;
            lockables.Add(c);
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

        Vector3 rawPos = GetLockPosition(targetCreature) + aimOffset;

        // 대상 바디 진동 흡수: 조준점을 SmoothDamp로 부드럽게
        if (!aimInitialized) { smoothedAimPos = rawPos; aimInitialized = true; }
        smoothedAimPos = aimSmoothTime > 0f
            ? Vector3.SmoothDamp(smoothedAimPos, rawPos, ref aimVelocity, aimSmoothTime)
            : rawPos;

        Vector3 dir = smoothedAimPos - cam.transform.position;

        if (dir.sqrMagnitude <= 0.000001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Vector3 currentEuler = cam.transform.rotation.eulerAngles;
        Vector3 targetEuler = targetRot.eulerAngles;
        Vector3 smoothed = new Vector3(
            Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref rotationVelocity.x, rotationSmoothTime),
            Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref rotationVelocity.y, rotationSmoothTime),
            Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref rotationVelocity.z, rotationSmoothTime)
        );
        cam.transform.rotation = Quaternion.Euler(smoothed);
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

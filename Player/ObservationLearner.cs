using System.Collections.Generic;
using UnityEngine;
using CreatureTypes;

// 락온한 생물이 "대표 행동(signatureIntent)"에 진입하는 걸 N번 목격하면
// 그 폼을 learnedForms에 추가 (변신 가능해짐).
public class ObservationLearner : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerLockOn lockOn;

    // CreatureID별 목격 누적
    private readonly Dictionary<CreatureID, int> progress = new();

    private Creature observed;
    private CreatureIntent lastIntent;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (lockOn == null) lockOn = player != null ? player.pl : GetComponent<PlayerLockOn>();
    }

    private void Update()
    {
        Creature target = lockOn != null ? lockOn.targetCreature : null;

        // 관찰 대상이 바뀌면 intent 추적 초기화
        if (target != observed)
        {
            observed = target;
            if (observed != null) lastIntent = observed.intent;
            return;
        }

        if (observed == null || observed.data == null) return;

        CreatureData data = observed.data;
        CreatureIntent now = observed.intent;

        // 대표 행동으로 "진입"하는 순간만 1회 카운트 (연속 유지는 중복 X)
        if (now != lastIntent && now == data.signatureIntent)
            CountObservation(data);

        lastIntent = now;
    }

    private void CountObservation(CreatureData data)
    {
        if (player == null) return;
        if (player.learnedForms.Contains(data)) return;   // 이미 학습함

        CreatureID id = data.creatureID;
        progress.TryGetValue(id, out int cur);
        cur++;
        progress[id] = cur;

        Debug.Log($"[Learn] {data.creatureName} 관찰 {cur}/{data.observationsToLearn}");

        if (cur >= data.observationsToLearn)
        {
            player.learnedForms.Add(data);
            progress.Remove(id);
            Debug.Log($"[Learn] {data.creatureName} 폼 학습 완료!");
            OnFormLearned?.Invoke(data);
        }
    }

    public System.Action<CreatureData> OnFormLearned;

    public int GetProgress(CreatureID id) => progress.TryGetValue(id, out int v) ? v : 0;
}

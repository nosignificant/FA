using UnityEngine;
using CreatureTypes;

// 락온한 생물 "개체"가 대표 행동(signatureIntent)에 진입하는 걸 N번 목격하면
// 그 개체를 possessable로 표시 (그 인스턴스만 빙의 가능해짐).
public class ObservationLearner : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerLockOn lockOn;

    private Creature observed;
    private CreatureIntent lastIntent;

    public System.Action<Creature> OnCreatureObserved;

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

        CreatureIntent now = observed.intent;

        // 대표 행동으로 "진입"하는 순간만 1회 카운트 (연속 유지는 중복 X)
        if (now != lastIntent && now == observed.data.signatureIntent)
            CountObservation(observed);

        lastIntent = now;
    }

    private void CountObservation(Creature c)
    {
        if (c.possessable) return;   // 이미 관찰 완료

        c.observeCount++;
        int need = Mathf.Max(1, c.data.observationsToLearn);
        Debug.Log($"[Observe] {c.data.creatureName} 관찰 {c.observeCount}/{need}");

        if (c.observeCount >= need)
        {
            c.possessable = true;
            Debug.Log($"[Observe] {c.data.creatureName} 빙의 가능!");
            OnCreatureObserved?.Invoke(c);
        }
    }
}

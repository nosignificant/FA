using UnityEngine;
using UnityEngine.Events;

// 퍼즐 조건: "특정 종을 watching하는 열린 문 개수 >= 임계값"이 되면 발동.
// DoorManager의 변경 이벤트를 구독해 실시간 판정하고, 상태가 바뀔 때만 UnityEvent를 쏜다.
public class DoorSpeciesCondition : MonoBehaviour
{
    [Header("Condition")]
    public CreatureData species;          // 어떤 종을 watching하는 문을 셀지
    [Min(1)] public int requiredOpenDoors = 1;

    [Header("Events")]
    public UnityEvent onConditionMet;     // 조건 충족되는 순간
    public UnityEvent onConditionLost;    // 조건 깨지는 순간

    public bool IsMet { get; private set; }

    private void OnEnable()
    {
        DoorManager.Instance.OnOpenDoorsChanged += Reevaluate;
        Reevaluate();
    }

    private void OnDisable()
    {
        if (DoorManager.Instance != null) DoorManager.Instance.OnOpenDoorsChanged -= Reevaluate;
    }

    private void Reevaluate()
    {
        bool now = DoorManager.Instance.OpenDoorCount(species) >= requiredOpenDoors;
        if (now == IsMet) return;   // 상태 변화 없으면 이벤트 안 쏨

        IsMet = now;
        if (IsMet) onConditionMet?.Invoke();
        else onConditionLost?.Invoke();
    }
}

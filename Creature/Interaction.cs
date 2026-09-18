using UnityEngine;
using CreatureTypes;
using static CreatureTypes.CreatureID;

[DisallowMultipleComponent]
public class Interaction : MonoBehaviour
{
    public bool HasAction(CreatureID selfID, CreatureID targetID, InteractionAction action)
    {
        return GetActionPriority(selfID, targetID, action) > int.MinValue;
    }

    // self가 target에게 아무 상호작용(chase/flee/grab/decompose/synthesize 등)이라도 있는지
    public bool HasAnyAction(CreatureID selfID, CreatureID targetID)
    {
        foreach (InteractionAction a in System.Enum.GetValues(typeof(InteractionAction)))
            if (HasAction(selfID, targetID, a)) return true;
        return false;
    }


    //priority는 같은 행동내에서 우선순위를 구분하고싶을떄 쓰는거고
    //hasAction은 그냥 행동이 거기 있는지
    public int GetActionPriority(CreatureID selfID, CreatureID targetID, InteractionAction action)
    {
        switch (selfID)
        {
            case S: return GetActionForS(targetID, action);
            case AA: return GetActionForAA(targetID, action);
            case LL: return GetActionForLL(targetID, action);
            case D: return GetActionForD(targetID, action);
            case Timid: return GetActionForTimid(targetID, action);
            default: return int.MinValue;
        }
    }

    // H: 자율 상호작용 없음. 플레이어가 조종해 AA에 F로 bind (HBinder). 그 외엔 배회.

    // S: 벽 파괴 전용 — 생물 상호작용 없음 (벽 타겟은 Sthink가 처리).
    private static int GetActionForS(CreatureID targetID, InteractionAction action)
    {
        return int.MinValue;
    }

    // a: 수동 입자 — 아무 상호작용 없음. L처럼 열린 문으로 흐르기만 함(RoomMigration 처리).

    // aa: H로부터 도망. L을 잡아 A로 변환 (grab→synthesize).
    private static int GetActionForAA(CreatureID targetID, InteractionAction action)
    {
        if (targetID == H && action == InteractionAction.Flee) return 110;

        if (targetID == L)
        {
            if (action == InteractionAction.Chase) return 70;
            if (action == InteractionAction.Grab) return 80;
            if (action == InteractionAction.Synthesize) return 90;
        }
        return int.MinValue;
    }

    // L: 수동 입자 — 아무 상호작용 없음(AA가 잡아 A로 바꿀 수 있게 도망도 안 침). 문으로 흐르기만.

    // LL: 생산기 — L 입자를 뱉음(촉수 생산). 잡는 것 없음. AA로부터 도망.
    private static int GetActionForLL(CreatureID targetID, InteractionAction action)
    {
        if ((targetID == AA || targetID == LL) && action == InteractionAction.Flee) return 100;
        return int.MinValue;
    }

    // Timid: 겁쟁이 — 문·동족을 뺀 모든 생물에게서 도망.
    private static int GetActionForTimid(CreatureID targetID, InteractionAction action)
    {
        if (action != InteractionAction.Flee) return int.MinValue;
        if (targetID == Timid || targetID == CreatureID.Door) return int.MinValue;   // 동족·문은 안 피함
        return 40;   // 그 외 모든 종에게서 도망
    }

    // D: 과밀한 방의 입자 L/A만 분해 (개체수 조절).
    private static int GetActionForD(CreatureID targetID, InteractionAction action)
    {
        bool removable = targetID == L || targetID == A;
        if (!removable) return int.MinValue;

        if (action == InteractionAction.Chase) return 60;
        if (action == InteractionAction.Decompose) return 80;
        return int.MinValue;
    }
}

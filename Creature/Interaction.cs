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


    //priority는 같은 행동내에서 우선순위를 구분하고싶을떄 쓰는거고 
    //hasAction은 그냥 행동이 거기 있는지 
    public int GetActionPriority(CreatureID selfID, CreatureID targetID, InteractionAction action)
    {
        switch (selfID)
        {
            case H: return GetActionForHS(targetID, action);
            case S: return GetActionForHS(targetID, action);
            case A: return GetActionForA(targetID, action);
            case AH: return GetActionForAssembledA(targetID, action);
            case AS: return GetActionForAssembledA(targetID, action);
            case AA: return GetActionForAA(targetID, action);
            case SS: return GetActionForSS(targetID, action);
            case HH: return GetActionForHH(targetID, action);
            case L: return GetActionForL(targetID, action);
            case D: return GetActionForD(targetID, action);
            default: return int.MinValue;
        }
    }

    private static int GetActionForHS(CreatureID targetID, InteractionAction action)
    {
        if (targetID == CreatureID.Player && action == InteractionAction.Chase) return 100;
        if ((targetID == L || targetID == AA) && action == InteractionAction.Flee) return 100;
        return int.MinValue;
    }
    // a: hh/ss로부터 도망, h/s에 붙어서 ah/as 합성
    private static int GetActionForA(CreatureID targetID, InteractionAction action)
    {
        if ((targetID == HH || targetID == SS) && action == InteractionAction.Flee) return 100;

        if (targetID == H || targetID == S)
        {
            if (action == InteractionAction.Chase) return 50;
            if (action == InteractionAction.Grab) return 60;
            if (action == InteractionAction.Synthesize) return 70;
        }
        return int.MinValue;
    }

    // ah, as: L에게서 도망, aa에게 다가감
    // L, aa둘 다 있으면 aa에게 다가감이 우선 - 이거 만들어야해
    private static int GetActionForAssembledA(CreatureID targetID, InteractionAction action)
    {
        if (targetID == L && action == InteractionAction.Flee) return 100;
        if (targetID == AA && action == InteractionAction.Chase) return 80;
        return int.MinValue;
    }

    // aa: ss는 무조건 도망, hh는 맞공격 (서로 때리다 HP 먼저 0되는 쪽이 죽음)
    //     h/s/ah/as 잡아먹어 합성
    private static int GetActionForAA(CreatureID targetID, InteractionAction action)
    {
        if (targetID == SS && action == InteractionAction.Flee) return 110;

        if (targetID == HH)
        {
            if (action == InteractionAction.Chase) return 90;
            if (action == InteractionAction.Attack) return 100;
        }

        if (targetID == H || targetID == S || targetID == A || targetID == AH || targetID == AS)
        {
            if (action == InteractionAction.Chase) return 70;
            if (action == InteractionAction.Grab) return 80;
            if (action == InteractionAction.Synthesize) return 90;
        }
        return int.MinValue;
    }

    // ss: a를 h/s로 되돌림, aa를 a 2개로 분해
    private static int GetActionForSS(CreatureID targetID, InteractionAction action)
    {
        if (targetID == A || targetID == AA)
        {
            if (action == InteractionAction.Chase) return 80;
            if (action == InteractionAction.Decompose) return 90;
        }
        return int.MinValue;
    }

    // hh: a/aa를 공격해 죽임
    private static int GetActionForHH(CreatureID targetID, InteractionAction action)
    {
        if (targetID == A || targetID == AA)
        {
            if (action == InteractionAction.Chase) return 90;
            if (action == InteractionAction.Attack) return 100;
        }
        return int.MinValue;
    }

    // L: 적대 없음. h/s/a 잡아 합성 (h+h=hh, s+s=ss, a 포함=aa)
    private static int GetActionForL(CreatureID targetID, InteractionAction action)
    {
        if (targetID == H || targetID == S || targetID == A)
        {
            if (action == InteractionAction.Chase) return 50;
            if (action == InteractionAction.Grab) return 60;
            if (action == InteractionAction.Synthesize) return 70;
        }
        return int.MinValue;
    }

    // D: l, aa 제외 모든 생물 잡아와서 분해 (개체수 조절 + 문 조건 카운터)
    private static int GetActionForD(CreatureID targetID, InteractionAction action)
    {
        bool removable = targetID == H || targetID == HH
                      || targetID == S || targetID == SS
                      || targetID == A;
        if (!removable) return int.MinValue;

        if (action == InteractionAction.Chase) return 60;
        if (action == InteractionAction.Decompose) return 80;
        return int.MinValue;
    }

    private static int GetActionForAHAS(CreatureID targetID, InteractionAction action)
    {
        if (targetID == L)
        {
            if (action == InteractionAction.Flee) return 100;
        }
        if (targetID == AA)
        {
            if (action == InteractionAction.Chase) return 100;
        }
        return int.MinValue;

    }
}

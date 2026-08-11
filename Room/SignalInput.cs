using System;
using UnityEngine;
using CreatureTypes;

// 문 논리게이트 종류
public enum GateType
{
    AND,     // 두 입력 다 ON
    OR,      // 하나라도 ON
    NOT,     // 입력 A 반전 (단일 입력)
    EQUALS,  // 입력 A·B의 우세종이 같은 종족이면 ON
}

// 게이트 입력 하나.
// - Room 소스: 어떤 방의 우세종을 보고 "지정종과 같나(EqualsSpecies)" 또는 "기준 방과 같나(EqualsRoom)"로 ON/OFF.
//   (런타임에 발신기가 room을 바꿔 꽂음)
// - Door 소스: 다른 문이 열려 있으면 ON. (게이트끼리 잇는 배선용)
[Serializable]
public class SignalInput
{
    public enum Source { Room, Door }
    public enum Compare { EqualsSpecies, EqualsRoom }

    public Source source = Source.Room;

    [Header("Room 소스")]
    [Tooltip("이 방의 우세종을 봄. 런타임에 발신기가 바꿔 꽂을 수 있음")]
    public Room room;
    public Compare compare = Compare.EqualsSpecies;
    [Tooltip("EqualsSpecies: 우세종이 이 종과 같으면 ON")]
    public CreatureData target;
    [Tooltip("EqualsRoom: 이 기준 방(보통 수신기 방)의 우세종과 같으면 ON")]
    public Room compareRoom;

    [Header("Door 소스")]
    [Tooltip("이 문이 열려 있으면 ON (게이트 → 게이트 배선)")]
    public Door door;

    // ON/OFF (AND/OR/NOT용)
    public bool IsOn()
    {
        if (source == Source.Door)
            return door != null && door.isOpen;

        var best = room != null ? room.MostNumerousSpecies() : null;
        if (best == null) return false;

        if (compare == Compare.EqualsRoom)
        {
            var r = compareRoom != null ? compareRoom.MostNumerousSpecies() : null;
            return r != null && CreatureFamily.Same(best.creatureID, r.creatureID);
        }

        return target != null && CreatureFamily.Same(best.creatureID, target.creatureID);
    }

    // 우세종 값 (EQUALS 게이트용). Door 소스는 값 개념이 없어 null.
    public CreatureData Species()
        => source == Source.Room && room != null ? room.MostNumerousSpecies() : null;

    public bool HasBinding => source == Source.Door ? door != null : room != null;
}

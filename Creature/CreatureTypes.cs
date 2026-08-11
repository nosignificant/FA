namespace CreatureTypes
{
    public enum CreatureIntent
    {
        Wander = 0,
        Chase = 1,
        Flee = 2,
        Grabbed = 3,
        Decomposed = 8,
        Synthesized = 9,
        Decomposing = 4,
        Attack = 5,
        Synthesizing = 6,
        Controlled = 7,
    }

    public enum InteractionAction
    {
        Ignore = 0,
        Chase = 1,
        Flee = 2,
        Grab = 3,
        Decompose = 4,
        Attack = 5,
        Synthesize = 6,
    }
    public enum CreatureID
    {
        Player = 0,
        H = 1,
        HH = 2,
        S = 3,
        SS = 4,
        A = 5,
        AA = 6,
        AH = 7,
        AS = 8,
        L = 9,
        D = 10,
        M = 11,
        T = 12,
        R = 13,
        LL = 14,   // 합성 전용 (생산 안 함). L은 생산기로 분리

        Door = 97,
        WalkingWeed = 98, // 상호작용 없음

        Weed = 99,        // 상호작용 없음 — 다른 생물이 타겟 삼지 않음

    }

    // 종족(family) 묶음 — 문 우세 판정에서 같은 계열은 한 종으로 취급.
    // 예: H·HH는 같은 종족. 나머지는 자기 자신이 종족.
    public static class CreatureFamily
    {
        public static CreatureID Of(CreatureID id)
        {
            switch (id)
            {
                case CreatureID.H:
                case CreatureID.HH:
                    return CreatureID.H;
                case CreatureID.S:
                case CreatureID.SS:
                    return CreatureID.S;
                default:
                    return id;
            }
        }

        public static bool Same(CreatureID a, CreatureID b) => Of(a) == Of(b);
    }

}

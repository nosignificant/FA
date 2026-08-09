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
        Weed = 11,        // 상호작용 없음 — 다른 생물이 타겟 삼지 않음
        WalkingWeed = 12, // 상호작용 없음
        Door = 97,
        M = 98,
    }

}

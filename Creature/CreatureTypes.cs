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
        Synthesize = 6,
    }
    // int 값은 기존 직렬화(에셋/프리팹의 creatureID) 유지를 위해 그대로 둠.
    public enum CreatureID
    {
        Player = 0,
        H = 1,
        S = 3,
        A = 5,
        AA = 6,
        L = 9,     // 똥(입자): 문으로 흐름, 방을 L로 활성화
        D = 10,    // 분해자: 과밀(발열)한 방의 입자/base를 정리 (개체수 조절)
        T = 12,    // 발신
        R = 13,    // 수신
        LL = 14,   // L을 생산하며 돌아다니는 생산기
        Timid = 15, // 겁쟁이: 모든 생물을 피해 도망

        Door = 97,
        WalkingWeed = 98, // 상호작용 없음

        Weed = 99,        // 상호작용 없음 — 다른 생물이 타겟 삼지 않음

    }

    // 종족(family) 묶음 — 문 우세 판정용. 현재는 각 종이 곧 자기 종족.
    public static class CreatureFamily
    {
        public static CreatureID Of(CreatureID id) => id;
        public static bool Same(CreatureID a, CreatureID b) => a == b;
    }

}

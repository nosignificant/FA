using UnityEngine;

public class AACreature : TentacleCreature
{
    [Header("AABehavior")]
    [Tooltip("1마리만 잡은 채 이 시간 지나면 그거 소비해서 A 1마리 합성")]
    public float aaSingleGrabTimeout = 5f;
}

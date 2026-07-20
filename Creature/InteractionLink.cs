using UnityEngine;
using CreatureTypes;


[RequireComponent(typeof(LineRenderer))]
public class InteractionLink : MonoBehaviour
{
    public Think2 think;
    public Creature self;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (think == null) think = GetComponentInParent<Think2>();
        if (self == null) self = GetComponentInParent<Creature>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
    }

    void LateUpdate()
    {
        // 내가 락온된 대상이고 chase/flee 중일 때만 상호작용 선 표시
        bool isSelfLocked = Player.Instance != null && Player.Instance.pl != null
                            && Player.Instance.pl.targetCreature == self;
        bool chaseOrFlee = self.intent == CreatureIntent.Chase || self.intent == CreatureIntent.Flee || self.intent == CreatureIntent.Decomposing;

        Creature target = think != null ? think.currentTarget.creature : null;
        bool active = isSelfLocked && chaseOrFlee && target != null && target != self && !target.IsDead;

        lr.enabled = active;
        if (!active) return;

        lr.SetPosition(0, self.rootTransform.position);
        lr.SetPosition(1, target.rootTransform.position);
    }
}
using UnityEngine;
using CreatureTypes;

public class AttachCreature : Creature
{
    [Tooltip("타겟에 붙일 오브젝트 (보통 비주얼/몸체). 비워두면 자기 transform 사용)")]
    public GameObject AttachObject;

    // AttachedTo 진입 시 AttachObject의 원래 부모 저장 (Release에서 복구)
    private Transform _attachOriginalParent;
    private bool _hierarchySaved = false;

    private Transform AttachT =>
        AttachObject != null ? AttachObject.transform : transform;

    public override void AttachedTo(Transform attachPoint)
    {
        intent = CreatureIntent.Decomposing;

        // 원래 부모 저장 (최초 1회만 — 중첩 호출 시 덮어쓰기 방지)
        if (!_hierarchySaved)
        {
            _attachOriginalParent = AttachT.parent;
            _hierarchySaved = true;
        }

        Rigidbody rb = AttachT.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = false;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (data.creatureID == CreatureID.S || data.creatureID == CreatureID.A)
            kabschIn.localPosition = Vector3.zero;

        // AttachObject를 타겟에 붙이고 위치 초기화
        AttachT.SetParent(attachPoint);
        AttachT.localPosition = Vector3.zero;
    }

    public override void Release()
    {
        intent = CreatureIntent.Wander;

        Rigidbody rb = AttachT.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        // AttachObject를 원래 부모로 복구
        if (_hierarchySaved)
        {
            AttachT.SetParent(_attachOriginalParent);
            _hierarchySaved = false;
        }
        else
        {
            AttachT.SetParent(null);
        }

        foreach (var mono in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Creature) continue;
            if (mono is Think2) continue;
            mono.enabled = true;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }
}

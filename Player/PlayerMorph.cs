using UnityEngine;
using CreatureTypes;

// 플레이어를 다른 생물로 "변신": pc.data 교체 + 비주얼 교체.
// 다른 생물들은 scanner로 pc를 보고 Interaction(pc.data.creatureID)으로 반응하므로
// 데이터만 바꿔도 즉시 그 생물 취급됨.
public class PlayerMorph : MonoBehaviour
{
    [Header("References")]
    public Player player;
    [Tooltip("기본(인간 폼) 비주얼 — 변신 해제 시 복원")]
    public GameObject baseVisual;
    [Tooltip("변신 비주얼이 붙을 부모 (보통 플레이어 모델 루트)")]
    public Transform visualParent;

    [Header("Database")]
    public CreatureDatabase creatureDB;

    private CreatureData originalData;
    private GameObject currentFormVisual;
    private bool morphed = false;

    public bool IsMorphed => morphed;
    public CreatureData CurrentForm => player != null && player.pc != null ? player.pc.data : null;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
    }

    /// <summary>지정한 CreatureID 형태로 변신</summary>
    public void Morph(CreatureID id)
    {
        if (player == null || player.pc == null || creatureDB == null) return;

        CreatureData form = creatureDB.GetByID(id);
        if (form == null)
        {
            Debug.LogWarning($"[PlayerMorph] {id} 데이터가 creatureDB에 없습니다.");
            return;
        }

        if (!morphed) originalData = player.pc.data;

        // 1. 데이터 교체 (다른 생물 반응이 여기서 바뀜)
        player.pc.data = form;
        player.pc.currentHP = form.maxHP;

        // 2. 비주얼 교체
        SwapVisual(form);

        morphed = true;
    }

    /// <summary>인간 폼으로 복귀</summary>
    public void Revert()
    {
        if (!morphed || player == null || player.pc == null) return;

        player.pc.data = originalData;
        player.pc.currentHP = originalData != null ? originalData.maxHP : player.pc.currentHP;

        if (currentFormVisual != null) Destroy(currentFormVisual);
        currentFormVisual = null;
        if (baseVisual != null) baseVisual.SetActive(true);

        morphed = false;
    }

    private void SwapVisual(CreatureData form)
    {
        // 이전 변신 비주얼 제거
        if (currentFormVisual != null) Destroy(currentFormVisual);
        currentFormVisual = null;

        if (baseVisual != null) baseVisual.SetActive(false);

        if (form.prefab == null) return;

        Transform parent = visualParent != null ? visualParent : transform;
        currentFormVisual = Instantiate(form.prefab, parent);
        currentFormVisual.transform.localPosition = Vector3.zero;
        currentFormVisual.transform.localRotation = Quaternion.identity;

        // 비주얼 전용 — AI/물리/충돌 전부 끔 (플레이어가 직접 조종하므로)
        foreach (var mono in currentFormVisual.GetComponentsInChildren<MonoBehaviour>())
            mono.enabled = false;
        foreach (var col in currentFormVisual.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var rb in currentFormVisual.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }
}

using System.Collections;
using UnityEngine;
using CreatureTypes;

// [3.2] 플레이어가 들어오면 AreaManager.Enter(targetScene, targetSpawn)로 area 전환.
// 씬을 언로드하지 않고(additive + 켜고/끄기) 상태를 유지한다.
//  - 홈→레벨 포탈: recordChoice 끔, targetScene=레벨, targetSpawn=그 레벨 입장 스폰
//  - 레벨→홈 포탈: recordChoice 켬 (이 레벨의 L/A 판정 기록), targetScene=홈, targetSpawn=afterLVn
[RequireComponent(typeof(Collider))]
public class AreaPortal : MonoBehaviour
{
    [Header("이동 대상")]
    [Tooltip("들어갈 씬 이름 (Build Settings에 있어야 함)")]
    public string targetScene;
    [Tooltip("그 씬에서 플레이어가 도착할 스폰포인트 오브젝트 이름")]
    public string targetSpawn;

    [Header("레벨 판정 기록 (레벨→홈 복귀 포탈만 켬)")]
    [Tooltip("켜면 전환 전에 '이 포탈이 속한 씬'의 방 L/A 과반으로 ChoiceProgress 기록")]
    public bool recordChoice = false;

    [Header("옵션")]
    [Tooltip("들어온 뒤 이 시간(초) 대기 후 전환 (0이면 즉시)")]
    public float delay = 0f;

    private bool triggered;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // area가 다시 켜질 때마다 한 번 더 쓸 수 있게 초기화
    private void OnEnable() => triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.GetComponentInParent<Player>() == null) return;   // 플레이어만
        triggered = true;

        if (delay > 0f) StartCoroutine(FireAfterDelay());
        else Fire();
    }

    private IEnumerator FireAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);
        Fire();
    }

    private void Fire()
    {
        if (recordChoice) RecordChoiceForThisScene();

        if (AreaManager.Instance != null)
            AreaManager.Instance.Enter(targetScene, targetSpawn);
        else
            Debug.LogError("[AreaPortal] AreaManager.Instance 없음 — Systems에 AreaManager를 두세요.", this);
    }

    // 이 포탈이 속한 씬의 방들만 세서 과반으로 판정 (L>A면 L, 그 외 A). additive라 전체가 아닌 '이 레벨'만.
    private void RecordChoiceForThisScene()
    {
        if (RoomManager.Instance == null) return;
        var myScene = gameObject.scene;

        int l = 0, a = 0;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null || r.gameObject.scene != myScene) continue;
            if (r.Activation == Room.RoomActivation.L) l++;
            else if (r.Activation == Room.RoomActivation.A) a++;
        }

        CreatureID choice = l > a ? CreatureID.L : CreatureID.A;
        ChoiceProgress.Record(choice);
        Debug.Log($"[AreaPortal] '{myScene.name}' 판정 기록: {choice} (L={l} A={a})", this);
    }
}

using System.Collections;
using UnityEngine;
using CreatureTypes;

// H: 방이 L활성이거나 AA가 있으면 깨어나 AA를 쫓아감(Think가 이동). 닿으면 bind:
//   - AA를 슬롯에 잡지 않고, H의 촉수 끝을 AA에 향하게만 둔다(= bind 표현).
//   - 묶인 AA는 그 Think를 정지(dormant)시켜 이동·L→A 변환 중단.
//   - 1 H = 1 AA. AA가 죽거나 방을 벗어나거나 H가 죽으면 풀림.
//   - 도구 생물 → 방 밖으로 이주하지 않음.
[RequireComponent(typeof(Creature))]
public class HBinder : MonoBehaviour
{
    public float checkInterval = 0.2f;
    [Tooltip("AA를 가리킬 촉수들 (비우면 자식의 모든 Tentacle 자동 사용)")]
    public Tentacle[] bindTentacles;

    private Creature self;
    private Think2 think;
    private Creature boundAA;
    private Transform[] tentacleOldTargets;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
        self.canMigrate = false;   // 도구 — 방에 묶임
        if (bindTentacles == null || bindTentacles.Length == 0)
            bindTentacles = GetComponentsInChildren<Tentacle>();
    }

    private void OnEnable()  { if (self != null) self.Died += OnDied; }
    private void OnDisable() { if (self != null) self.Died -= OnDied; }
    private void OnDied(Creature c, CreatureID who) => Release();

    private void Start() => StartCoroutine(Loop());

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (self != null && !self.IsDead)
        {
            // 묶은 AA가 죽거나 방을 벗어나면 풀기
            if (boundAA != null && (boundAA.IsDead || boundAA.currentRoom != self.currentRoom))
                Release();

            UpdateActivated();

            // 묶는 중엔 isBound(움직임만 멈춤, 활성색 유지), 각성 안 하면 dormant(휴면).
            self.isBound = (boundAA != null);
            if (think != null) think.dormant = !activated;

            yield return wait;
        }
        Release();
    }

    // 각성 래치: 방이 한번 L이 되면 켜짐. 이후 AA가 있으면 계속 켜짐. L도 아니고 AA도 없으면 꺼짐.
    private bool activated;
    private void UpdateActivated()
    {
        var room = self.currentRoom;
        if (room == null) { activated = false; return; }
        if (room.Activation == Room.RoomActivation.L) { activated = true; return; }   // L → 켜짐(래치)
        // 여기부턴 방이 A 또는 None
        if (!room.HasSpecies(CreatureID.AA)) activated = false;   // L도 아니고 AA도 없으면 꺼짐
        // (L 아니지만 AA 있으면 → 현재 상태 유지 = 계속 켜짐)
    }
    private bool IsAwake() => activated;

    // 플레이어가 H 조종 중 AA에 F(락온 후 해제) → 수동 bind
    public void BindManual(Creature aa)
    {
        if (aa == null || aa.IsDead || boundAA != null) return;
        if (aa.data == null || aa.data.creatureID != CreatureID.AA) return;
        Bind(aa);
        self.isBound = true;   // 묶는 순간부터 H 정지(활성 유지)
    }

    private void Bind(Creature aa)
    {
        boundAA = aa;
        aa.isBound = true;   // AA 정지(활성 유지) → 이동·변환 중단, HUD "bound"

        // 모든 촉수 끝을 AA로 향하게
        Transform aaT = aa.rootTransform != null ? aa.rootTransform : aa.transform;
        if (bindTentacles != null)
        {
            tentacleOldTargets = new Transform[bindTentacles.Length];
            for (int i = 0; i < bindTentacles.Length; i++)
            {
                if (bindTentacles[i] == null) continue;
                tentacleOldTargets[i] = bindTentacles[i].target;
                bindTentacles[i].target = aaT;
            }
        }

        Popup.Instance?.BurstMessage(self, "bound", Color.cyan);
    }

    private void Release()
    {
        if (boundAA != null) boundAA.isBound = false;   // AA 정지 해제
        self.isBound = false;                            // H 정지 해제
        if (bindTentacles != null && tentacleOldTargets != null)
            for (int i = 0; i < bindTentacles.Length && i < tentacleOldTargets.Length; i++)
                if (bindTentacles[i] != null) bindTentacles[i].target = tentacleOldTargets[i];
        boundAA = null;
    }

    // ── HUD ──
    public string HudStatus()
    {
        if (boundAA != null) return "bind";
        if (!IsAwake()) return "dormant";
        return "wander";
    }

    public Creature HeldAA() => boundAA;
}

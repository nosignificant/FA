using System.Collections;
using UnityEngine;
using CreatureTypes;

// H: 한번 활성화되면(startActivated 또는 방이 L이 된 순간) 계속 활성 유지.
//   - 활성 상태면 Think이 AA를 자동 추적(Interaction Chase) → bindRange 안에 들면 자동 bind.
//   - bind: 촉수 대신 H중심↔AA중심을 잇는 선(LineRender)만 그림. 묶인 AA는 isBound로 정지.
//   - 해제: AA가 죽거나 방을 벗어나거나, releaseRange보다 멀어지면(플레이어가 H를 끌고 가면) 풀림.
//   - 1 H = 1 AA. 도구 생물 → 방 밖으로 이주하지 않음.
[RequireComponent(typeof(Creature))]
public class HBinder : MonoBehaviour
{
    public float checkInterval = 0.2f;
    [Tooltip("H중심↔AA중심을 잇는 bind 선 (비우면 자식에서 자동 탐색)")]
    public LineRender bindLine;
    [Tooltip("레벨 시작부터 각성. 방이 아직 L이 아니어도 처음부터 H를 쓰게 할 때.")]
    public bool startActivated = false;
    [Tooltip("이 거리 안에 AA가 들어오면 자동으로 붙잡음(bind)")]
    public float bindRange = 3f;
    [Tooltip("붙잡은 AA와 이 거리 넘게 멀어지면 자동 해제 (플레이어가 H를 끌고 가면)")]
    public float releaseRange = 8f;

    private Creature self;
    private Think2 think;
    private CreatureScanner scanner;
    private Creature boundAA;

    private void Awake()
    {
        self = GetComponent<Creature>();
        think = GetComponent<Think2>();
        scanner = GetComponent<CreatureScanner>();
        self.canMigrate = false;   // 도구 — 방에 묶임
        if (bindLine == null) bindLine = GetComponentInChildren<LineRender>();
    }

    private void OnEnable()  { if (self != null) self.Died += OnDied; }
    private void OnDisable() { if (self != null) self.Died -= OnDied; }
    private void OnDied(Creature c, CreatureID who) => Release();

    private void Start()
    {
        if (startActivated) activated = true;   // 레벨 설정: 시작부터 켜두기
        StartCoroutine(Loop());
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (self != null && !self.IsDead)
        {
            UpdateActivated();   // 한번 켜지면 계속 켜짐(영구 래치)

            Debug.Log($"[HBinder] {name} activated={activated} bound={(boundAA!=null)} nearestAA={NearestAAInRange(bindRange)?.name} bindRange={bindRange} room={self.currentRoom?.roomID}");
            if (activated)
            {
                if (boundAA == null)
                {
                    // 반경 안 AA 자동 붙잡기 (Think이 Chase로 다가오고, 가까워지면 여기서 bind)
                    Creature aa = NearestAAInRange(bindRange);
                    if (aa != null) Bind(aa);
                }
                else if (boundAA.IsDead
                         || boundAA.currentRoom != self.currentRoom
                         || Dist(boundAA) > releaseRange)   // 죽음·방 이탈·멀어짐 → 해제
                {
                    Debug.Log($"[HBinder] {name} RELEASE — dead={boundAA.IsDead} " +
                              $"roomMismatch={(boundAA.currentRoom != self.currentRoom)}(aa={boundAA.currentRoom?.roomID}/h={self.currentRoom?.roomID}) " +
                              $"dist={Dist(boundAA):F2}(release={releaseRange})");
                    Release();
                }
            }

            // 묶는 중엔 isBound(움직임만 멈춤, 활성색 유지), 각성 안 하면 dormant(휴면).
            self.isBound = (boundAA != null);
            if (think != null) think.dormant = !activated;

            yield return wait;
        }
        Release();
    }

    private float Dist(Creature c) => (c.rootTransform.position - self.rootTransform.position).magnitude;

    // 각성 래치: startActivated거나 방이 한번이라도 L이 되면 켜짐. 이후 영구 유지(끄지 않음).
    private bool activated;
    private void UpdateActivated()
    {
        if (activated) return;   // 한번 켜지면 유지
        if (startActivated) { activated = true; return; }
        var room = self.currentRoom;
        if (room != null && room.Activation == Room.RoomActivation.L) activated = true;
    }

    // 스캐너 인식범위 안, range 이내의 가장 가까운 살아있는 AA (같은 방)
    private Creature NearestAAInRange(float range)
    {
        if (scanner == null) return null;
        var results = scanner.Results;
        Vector3 p = self.rootTransform.position;
        Creature best = null;
        float bestSqr = range * range;
        for (int i = 0; i < results.Count; i++)
        {
            var c = results[i];
            if (c == null || c.IsDead || c.data == null || c.data.creatureID != CreatureID.AA) continue;
            if (c.currentRoom != self.currentRoom) continue;
            float sqr = (c.rootTransform.position - p).sqrMagnitude;
            if (sqr <= bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
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
        Popup.Instance?.BurstMessage(self, "bound", Color.cyan);
    }

    private void Release()
    {
        if (boundAA != null) boundAA.isBound = false;   // AA 정지 해제
        self.isBound = false;                            // H 정지 해제
        boundAA = null;
        if (bindLine != null) bindLine.Clear();          // 선 지움
    }

    // bind 선: H중심 ↔ AA중심. 매 프레임 갱신(둘 다 정지지만 부드럽게)
    private void LateUpdate()
    {
        if (bindLine == null) return;
        if (boundAA != null && !boundAA.IsDead)
            bindLine.DrawBetween(SelfCenter(), Center(boundAA));
        else
            bindLine.Clear();
    }

    private Vector3 SelfCenter() => self.rootTransform != null ? self.rootTransform.position : transform.position;
    private static Vector3 Center(Creature c) => c.rootTransform != null ? c.rootTransform.position : c.transform.position;

    // ── HUD ──
    public string HudStatus()
    {
        if (boundAA != null) return "bind";
        if (!IsAwake()) return "dormant";
        return "chase";   // 활성 상태면 AA를 쫓음
    }

    public Creature HeldAA() => boundAA;
}

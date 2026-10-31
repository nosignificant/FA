using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatureTypes;

// 플레이어가 포탈에 들어오는 순간 레벨의 L/A 방 개수로 판정(과반, 동점→A)하고 다음 씬으로 전환.
[RequireComponent(typeof(Collider))]
public class LevelPortal : MonoBehaviour
{
    [Header("옵션")]
    [Tooltip("Additive면 현재 씬 위에 겹쳐 로드, 아니면 교체")]
    public bool additive = false;
    [Tooltip("기록된 선택(ChoiceProgress)이 없을 때 쓸 기본 선택 — LevelFlow 분기·클리어 표시용")]
    public CreatureID choiceId = CreatureID.A;
    [Tooltip("이 포탈 통과 시 로딩 화면에 'level N clear L/A' 표시 (레벨 클리어 포탈이면 켬)")]
    public bool showClearOnPass = true;
    [Tooltip("들어온 뒤 이 시간(초) 대기 후 전환 (0이면 즉시)")]
    public float delay = 0f;
    [Tooltip("전환 전 timeScale을 1로 복구 (일시정지/빙의 등으로 0이던 경우 대비)")]
    public bool restoreTimeScale = true;

    private bool triggered = false;
    private CreatureID pendingChoice;

    private void Reset()
    {
        // 에디터에서 붙이면 콜라이더를 트리거로 자동 설정
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.GetComponentInParent<Player>() == null) return;   // 플레이어만
        Debug.Log($"[LevelPortal] {name}: 플레이어 진입 → 선택 캡처 시도", this);

        if (!TryCaptureChoice(out pendingChoice))
        {
            Debug.Log($"[LevelPortal] {name}: 캡처 실패 → 전환 취소 (씬에 R이 있는데 그 방 상태가 L/A가 아님)", this);
            return;
        }
        triggered = true;
        Debug.Log($"[LevelPortal] {name}: 전환 시작 choice={pendingChoice}", this);

        if (delay > 0f) StartCoroutine(LoadAfterDelay());
        else Load();
    }

    private IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);   // timeScale=0이어도 대기
        Load();
    }

    // 포탈 진입 순간 레벨의 L/A 방 개수로 판정한다. L 방이 더 많으면 L, 그 외(A가 많거나 동점·미정)는 A.
    private bool TryCaptureChoice(out CreatureID choice)
    {
        int countL = CountRooms(Room.RoomActivation.L);
        int countA = CountRooms(Room.RoomActivation.A);
        choice = countL > countA ? CreatureID.L : CreatureID.A;   // 동점·미정 → A(기본값)
        return true;
    }

    // 레벨 전체에서 이 상태(L/A)로 활성화된 방의 개수
    private int CountRooms(Room.RoomActivation state)
    {
        if (RoomManager.Instance == null || RoomManager.Instance.rooms == null) return 0;
        int n = 0;
        foreach (var r in RoomManager.Instance.rooms.Values)
            if (r != null && r.Activation == state) n++;
        return n;
    }

    private void Load()
    {
        CreatureID choice = pendingChoice;

        // select level 단독 플레이: 다음 레벨 대신 타이틀로 복귀 + 이번 레벨 선택(A/L) 초기화
        if (LevelFlow.standalonePlay)
        {
            if (restoreTimeScale) Time.timeScale = 1f;
            LevelFlow.standalonePlay = false;
            ChoiceProgress.Clear();
            if (SceneLoader.Instance != null) SceneLoader.Instance.Load(LevelFlow.TitleScene);
            else SceneManager.LoadScene(LevelFlow.TitleScene);
            return;
        }

        string scene = LevelFlow.GetNext(SceneManager.GetActiveScene().name, choice);
        Debug.Log($"[LevelPortal] {name}: 현재씬='{SceneManager.GetActiveScene().name}' → 다음='{scene ?? "(null!)"}' choice={choice} standalone={LevelFlow.standalonePlay}", this);

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"[LevelPortal] {name}: LevelFlow에 '{SceneManager.GetActiveScene().name}' 다음 씬이 없음.");
            return;
        }

        if (restoreTimeScale) Time.timeScale = 1f;

        // 이번 레벨의 판정(과반)을 확정하는 시점은 포탈 진입.
        ChoiceProgress.Record(choice);

        // 로딩바가 있으면 SceneLoader로 (additive는 로더 미지원 → 직접 로드)
        if (!additive && SceneLoader.Instance != null)
        {
            if (showClearOnPass) SceneLoader.Instance.RequestClear(choice);   // 통과 = 클리어 → clear 표시
            SceneLoader.Instance.Load(scene);
        }
        else
        {
            SceneManager.LoadScene(scene, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
    }

    private void OnDrawGizmos()
    {
        // 씬 뷰에서 전환 영역을 보이게
        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box) Gizmos.DrawCube(box.center, box.size);
        else Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}

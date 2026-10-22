using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatureTypes;

// 플레이어가 포탈에 들어오는 순간 R이 있는 방의 현재 상태를 기록하고 다음 씬으로 전환.
[RequireComponent(typeof(Collider))]
public class LevelPortal : MonoBehaviour
{
    [Header("옵션")]
    [Tooltip("비우면 씬의 R을 자동 검색")]
    public SignCreature signalSource;
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
    private bool capturedFromR;

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

        if (!TryCaptureChoice(out pendingChoice)) return;
        triggered = true;

        if (delay > 0f) StartCoroutine(LoadAfterDelay());
        else Load();
    }

    private IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);   // timeScale=0이어도 대기
        Load();
    }

    // 포탈 진입 순간의 R 상태를 캡처. 전환 지연 중 R 상태가 바뀌어도 선택은 바뀌지 않는다.
    private bool TryCaptureChoice(out CreatureID choice)
    {
        // Every return path must provide a value, including the "no signal state" path.
        choice = choiceId;
        SignCreature source = signalSource != null ? signalSource : FindObjectOfType<SignCreature>();
        if (source != null)
        {
            capturedFromR = true;
            Room.RoomActivation state = source.CurrentSignalState;
            if (state != Room.RoomActivation.L && state != Room.RoomActivation.A)
            {
                Popup.Instance?.BurstMessage(source, "no state", Color.gray);
                return false;
            }

            choice = SignCreature.ChoiceForState(state);
            return true;
        }

        // R이 없는 기존 씬은 기록된 선택 또는 인스펙터 기본값을 유지.
        capturedFromR = false;
        var all = ChoiceProgress.All();
        choice = all.Count > 0 ? all[all.Count - 1] : choiceId;
        return true;
    }

    private void Load()
    {
        CreatureID choice = pendingChoice;
        string scene = LevelFlow.GetNext(SceneManager.GetActiveScene().name, choice);

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"[LevelPortal] {name}: LevelFlow에 '{SceneManager.GetActiveScene().name}' 다음 씬이 없음.");
            return;
        }

        if (restoreTimeScale) Time.timeScale = 1f;

        // R 상태로 이번 레벨 선택을 확정하는 시점은 포탈 진입.
        if (capturedFromR) ChoiceProgress.Record(choice);

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

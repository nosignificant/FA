using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatureTypes;

// 트리거 영역에 플레이어가 들어오면 지정한 씬으로 전환한다.
// 레벨 전환 트리거를 이 한 스크립트로 관리 (전환할 영역마다 이 컴포넌트를 붙이면 됨).
// 예: 튜토리얼 끝 영역 → "production" 씬.
[RequireComponent(typeof(Collider))]
public class LevelPortal : MonoBehaviour
{
    [Header("전환 대상")]
    [Tooltip("켜면 LevelFlow 맵(현재 씬→다음 씬)에서 자동 결정. 각 씬마다 targetScene 안 바꿔도 됨")]
    public bool useFlowMap = true;
    [Tooltip("useFlowMap이 꺼져있거나 맵에 없을 때 쓸 씬 이름 (Build Settings 등록 필요)")]
    public string targetScene;
    [Tooltip("Additive면 현재 씬 위에 겹쳐 로드, 아니면 교체")]
    public bool additive = false;

    [Header("엔딩 분기용 선택 기록")]
    [Tooltip("이 포탈을 통과한 걸 '선택'으로 기록할지 (엔딩 분기용)")]
    public bool recordAsChoice = false;
    [Tooltip("이 문의 판정 종 (예: A문이면 A, L문이면 L)")]
    public CreatureID choiceId = CreatureID.A;
    [Tooltip("켜면 choiceId 대신 '방금 확정한 선택'(마지막 ChoiceProgress)으로 다음 씬·클리어 표시를 결정. 수신기(R) 확정 흐름용")]
    public bool useConfirmedChoice = false;
    [Tooltip("이 포탈 통과 시 로딩 화면에 'level N clear L/A' 표시 (레벨 클리어 포탈이면 켬)")]
    public bool showClearOnPass = true;

    [Header("옵션")]
    [Tooltip("들어온 뒤 이 시간(초) 대기 후 전환 (0이면 즉시)")]
    public float delay = 0f;
    [Tooltip("전환 전 timeScale을 1로 복구 (일시정지/빙의 등으로 0이던 경우 대비)")]
    public bool restoreTimeScale = true;

    private bool triggered = false;

    private void Reset()
    {
        // 에디터에서 붙이면 콜라이더를 트리거로 자동 설정
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[LevelPortal] OnTriggerEnter: {other.name} (Player={(other.GetComponentInParent<Player>() != null)})");

        if (triggered) return;

        // Room과 동일한 방식으로 플레이어 판별
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        Debug.Log($"[LevelPortal] 플레이어 진입 → '{targetScene}' (delay={delay})");
        triggered = true;

        // 이 포탈로 다음 레벨에 갔다 = 이 문을 선택 → 기록 (엔딩 분기용)
        // useConfirmedChoice면 수신기 확정에서 이미 기록했으므로 여기선 기록 안 함(중복 방지)
        if (recordAsChoice && !useConfirmedChoice) ChoiceProgress.Record(choiceId);

        if (delay > 0f) StartCoroutine(LoadAfterDelay());
        else Load();
    }

    private IEnumerator LoadAfterDelay()
    {
        // delay는 실제 시간 기준 (timeScale=0이어도 대기)
        yield return new WaitForSecondsRealtime(delay);
        Load();
    }

    // 분기에 쓸 선택: useConfirmedChoice면 방금 확정한 것(마지막 기록), 아니면 이 포탈의 choiceId
    private CreatureID EffectiveChoice()
    {
        if (useConfirmedChoice)
        {
            var all = ChoiceProgress.All();
            if (all.Count > 0) return all[all.Count - 1];
        }
        return choiceId;
    }

    private void Load()
    {
        CreatureID choice = EffectiveChoice();

        // 다음 씬 결정: useFlowMap이면 LevelFlow(현재 씬→다음 씬)에서, 아니면/없으면 targetScene.
        string scene = targetScene;
        if (useFlowMap)
        {
            string fromFlow = LevelFlow.GetNext(SceneManager.GetActiveScene().name, choice);
            if (!string.IsNullOrEmpty(fromFlow)) scene = fromFlow;
            else Debug.LogWarning($"[LevelPortal] {name}: LevelFlow에 '{SceneManager.GetActiveScene().name}' 다음 씬 없음 → targetScene 사용");
        }

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"[LevelPortal] {name}: 로드할 씬이 없음 (LevelFlow·targetScene 둘 다 비어있음)");
            return;
        }

        if (restoreTimeScale) Time.timeScale = 1f;

        // 로딩바가 있으면 SceneLoader로 (additive는 로더 미지원 → 직접 로드)
        if (!additive && SceneLoader.Instance != null)
        {
            Debug.Log($"[LevelPortal] SceneLoader로 '{scene}' 로드");
            if (showClearOnPass) SceneLoader.Instance.RequestClear(choice);   // 통과 = 클리어 → clear 표시
            SceneLoader.Instance.Load(scene);
        }
        else
        {
            Debug.Log($"[LevelPortal] 직접 '{scene}' 로드 (SceneLoader={(SceneLoader.Instance != null)})");
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

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatureTypes;

// 트리거 영역에 플레이어가 들어오면 LevelFlow가 정한 다음 씬으로 전환.
// 다음 씬은 LevelFlow(현재 씬 + 선택)로 결정. 엔딩 분기 선택은 발신기/수신기가 ChoiceProgress에 기록하고,
// 포탈은 그 기록된 선택으로 분기·클리어 표시만 한다.
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
        triggered = true;

        if (delay > 0f) StartCoroutine(LoadAfterDelay());
        else Load();
    }

    private IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);   // timeScale=0이어도 대기
        Load();
    }

    // 분기에 쓸 선택: 발신기/수신기가 기록한 마지막 ChoiceProgress, 없으면 choiceId
    private CreatureID EffectiveChoice()
    {
        var all = ChoiceProgress.All();
        return all.Count > 0 ? all[all.Count - 1] : choiceId;
    }

    private void Load()
    {
        CreatureID choice = EffectiveChoice();
        string scene = LevelFlow.GetNext(SceneManager.GetActiveScene().name, choice);

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"[LevelPortal] {name}: LevelFlow에 '{SceneManager.GetActiveScene().name}' 다음 씬이 없음.");
            return;
        }

        if (restoreTimeScale) Time.timeScale = 1f;

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

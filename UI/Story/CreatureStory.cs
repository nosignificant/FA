using System;
using System.Collections.Generic;
using UnityEngine;

// 생물이 죽을 때 흩어지는 단편 대사(파편) 저장소 + 수집(획득) 상태 관리.
// - storyline: 단계(Stage)별 대사 줄들(데이터)
// - collected: 플레이어가 실제로 획득한 단계들 (코덱스 UI가 이걸로 잠금/표시 결정)
public class CreatureStory : MonoBehaviour
{
    public static CreatureStory Instance { get; private set; }

    // 새로 획득했을 때 발행 (코덱스 UI가 구독해서 갱신)
    public event Action<int> OnCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 이전 씬까지의 획득 목록 복원
        foreach (int s in StoryProgress.LoadCollected()) collected.Add(s);
    }

    // ── 데이터: stage → 그 단계에서 뿌릴 대사 줄들 ──────────────
    private readonly Dictionary<int, string[]> storyline = new()
    {
        { 1, new[] { "세포가 죽는 경우", "외부적 문제에 의해 손상", "세포 스스로에 의해 조절되는 세포의 자살" , "세포가 스스로를 먹다가 죽는 경우"} },
        { 2, new[] { "첫째는 괴사(Necrosis)", "둘째는 세포자살(Apoptosis)", "셋째는 자가포식(Autophagy)" } },
        { 3, new[] { "세포자살",  "의 경우 괴사와 달리", "생물체에 해를", "끼치지 않으며", "생명주기에 유익한 것", } },
        {4, new[] { "a",
                    "일단 자살하기로 결심하면 그는 완전히 폐쇄되어",
                    "난공불락이면서도 절대적인 확신을 주는 세계로 진입해 들어가게 된다",
                    "이 세계 안에서는 가장 사소한 것 하나하나까지 모두 맞아 들어가고",
                    "모든 일이 하나같이 그의 결심을 강화해 준다"}},
        {5, new[] {"l" ,
                    "삶 봄철에 티파사에는 신들이 내려와 산다",
                    "슬픔과 기쁨을 만든다 삶은 조종할 수 없다"}}
    };

    // ── 수집 상태 ────────────────────────────────────────────
    private readonly HashSet<int> collected = new();

    // 현재 단계 대사를 가져오면서 '획득' 처리 (Popup이 뿌릴 때 호출)
    public string[] CollectAndGetCurrentLines()
    {
        int stage = Player.Instance != null ? Player.Instance.Stage : 0;
        Collect(stage);
        return GetLines(stage);
    }

    // 특정 단계를 획득 처리
    public void Collect(int stage)
    {
        if (!storyline.ContainsKey(stage)) return;   // 데이터 없는 단계는 무시
        if (!collected.Add(stage)) return;           // 이미 있으면 중복 발행 안 함
        StoryProgress.SaveCollected(collected);      // 씬 넘어가도 유지
        OnCollected?.Invoke(stage);
    }

    public bool IsCollected(int stage) => collected.Contains(stage);

    // 코덱스 UI용: 데이터에 존재하는 모든 단계 (오름차순)
    public IEnumerable<int> AllStages
    {
        get
        {
            var keys = new List<int>(storyline.Keys);
            keys.Sort();
            return keys;
        }
    }

    public string[] GetLines(int stage) =>
        storyline.TryGetValue(stage, out var lines) ? lines : Array.Empty<string>();
}

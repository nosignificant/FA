using System.Collections.Generic;
using CreatureTypes;

// 스토리 텍스트 데이터의 단일 출처 (레벨 인트로 / 엔딩 분기).
// MonoBehaviour 아님 — 표시는 LevelIntro·엔딩 씬 컴포넌트가, 튜토리얼 흐름은 TutorialGuide가 담당.
public static class P53
{
    // ── 레벨(씬) 인트로 대사 ──────────────────────────────────
    // 키는 실제 씬 파일 이름과 정확히 일치해야 함 (Build Settings 기준). LevelIntro가 읽어 표시한다.
    public static readonly Dictionary<string, string[]> IntroLines = new()
    {
        { "tutorial2", new[]
            {
                "당신이 문을 여는 행동은 저의 사고 회로를 연결하는 것과 같은 행동입니다.",
                "이번에는, 당신의 등 뒤의 SS 분해 조건이 열린 문을 열어보십시오.",
                "잊지 마십시오, 당신은 생물을 조종할 수 있습니다.",
            }
        },
        { "level1", new[]
            {
                "잊지 마십시오. 문을 여는 것은 생각하는 것입니다.",
                "몇몇 생물은 조종할 수 없습니다. 유의하십시오.",
                "열지 못하는 문은 존재하지 않습니다.",
            }
        },
        { "level2", new[]
            {
                "의체 사용 빈도 증가, 전자 두뇌 복제 등의 기술으로 인해 인류는 계를 구성하는 물질들의 복잡도가 임계를 넘을 시 해당 시스템 계를 생물으로 정의했습니다.",
            }
        },
        { "level4", new[]
            {
                "학문과 국가 그리고 인류는 생물으로 정의되고 거시생물학 또한 재정의되었습니다.",
                "이렇게 정의된 생물은 출생기와 쇠퇴기에 공유하는 특성이 있음이 두드러졌습니다.",
            }
        },
    };

    // 씬 이름으로 인트로 대사 조회 (없으면 null)
    public static string[] GetIntroLines(string sceneName) =>
        IntroLines.TryGetValue(sceneName, out var lines) ? lines : null;

    // ── 엔딩 분기 대사 ──────────────────────────────────────────
    // 앞 3레벨 모두 A문 선택
    public static readonly string[] EndingAllA =
    {
        "생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다. 국가는 반드시 쇠퇴하며 언어도 마찬가지로 살아가다 죽었으며 인류도 탄생의 시기의 생명력은 다시 찾아볼 수 없게 되었습니다.",
        "생태계의 순환이라는 명목 하에 이를 반복하고 있지만 거시적인 관점에서는 선형적이었습니다.",
    };

    // 그 외
    public static readonly string[] EndingOther =
    {
        "생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다. 인류는 이 순환을 깨닫고 있었고 괴로워했으며 몸부림쳤고 탄생 초기의 생기를 되찾고 싶어했습니다.",
        "인류는 시들고 말라 앙상해진, 한 손에 들어오는 애처로운 모습으로 호소했습니다...",
    };

    // 선택 기록에 따라 엔딩 대사 선택 (앞 3레벨 모두 A → EndingAllA)
    public static string[] GetEnding() =>
        ChoiceProgress.AllChose(CreatureID.A, 3) ? EndingAllA : EndingOther;
}

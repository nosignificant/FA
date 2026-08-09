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
                "당신이 문을 여는 행동은 사고 회로를 연결하는 것과 같은 행동입니다.",
                "이번에는 당신 앞의 SS 문을 열어보십시오.",
                "방은 가장 많이 분해된 생물 종의 문을 엽니다.",
                "하단의 방위 표시를 통해 문과 바라보고 있는 곳의 방향을 파악할 수 있습니다.",
                "{possess}를 눌러 생물을 조종하십시오.",

            }
        },
        { "level1", new[]
            {
                "잊지 마십시오. 문을 여는 것은 생각하는 것입니다.",
                "몇몇 생물은 조종할 수 없습니다. 유의하십시오.",
                "열지 못하는 문은 존재하지 않습니다.",
                "각 문에서 요구하는 조건을 파악하고 충족시키십시오.",
            }
        },
        { "level2", new[]
            {
                "의체 사용 빈도 증가, 전자 두뇌 복제 등의 기술이 등장으로 인류는 생물에 대한 새로운 정의가 필요했습니다.",
                "인류는 계를 구성하는 물질들의 복잡도가 임계를 넘을 시 해당 시스템 계를 생물으로 정의했습니다.",
            }
        },
        { "level3", new[]
            {
                "학문과 국가 그리고 인류는 생물으로 정의되고 거시생물학 또한 재정의되었습니다.",
                "이렇게 정의된 생물은 출생기와 쇠퇴기에 공유하는 특성이 있음이 두드러졌습니다.",
            }
        },
    };

    // 씬 이름으로 인트로 대사 조회 (없으면 null)
    public static string[] GetIntroLines(string sceneName) =>
        IntroLines.TryGetValue(sceneName, out var lines) ? lines : null;

    // 대사 안의 토큰({lockOn}/{possess}/{codex})을 실제 키로 치환. 표시 직전에 호출.
    public static string Resolve(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        var pim = Player.Instance != null ? Player.Instance.GetComponent<PlayerInputManager>() : null;
        if (pim == null) return line;

        return line
            .Replace("{lockOn}", pim.lockOnKey.ToString())
            .Replace("{possess}", pim.possessKey.ToString())
            .Replace("{codex}", pim.codexToggleKey.ToString());
    }

    // ── 엔딩 분기 대사 ──────────────────────────────────────────
    // 앞 3레벨 모두 A문 선택
    public static readonly string[] level4A =
    {
        "생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다.",
        "국가는 반드시 쇠퇴하며 언어도 마찬가지로 살아가다 죽었으며 인류도 탄생의 시기의 생명력은 다시 찾아볼 수 없게 되었습니다.",
        "생태계의 순환이라는 명목 하에 이를 반복하고 있지만 거시적인 관점에서는 선형적이었습니다.",
    };

    // 그 외
    public static readonly string[] level4L =
    {
        "생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다.",
        "인류는 이 순환을 깨닫고 있었고 괴로워했으며 몸부림쳤고 탄생 초기의 생기를 되찾고 싶어했습니다.",
        "인류는 시들고 말라 앙상해진, 한 손에 들어오는 애처로운 모습으로 호소했습니다.",
    };

    // 선택 기록에 따라 엔딩 대사 선택 (앞 3레벨 모두 A → EndingAllA)
    public static string[] GetLevel4() =>
        ChoiceProgress.AllChose(CreatureID.A, 2) ? level4A : level4L;

    // ── ending 씬: A문을 몇 개 열었냐로 분기 (조합 대신 개수) ────
    // A 2개 이상
    public static readonly string[] endingA3 =
    {
        "저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.",
        "지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,",
        "인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.",
        "인류의 행복과 평안을 위해 저는 '모든 인류가 아무런 미련 없이 삶을 종료할 수 있는가?'를",
        "간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으며,",
        "그 결과 모든 회로에서 '네'라는 대답을 얻었습니다.",
        "기억과 감정을 추출하던 보존되어있는 인간이 모두 이에 동의했다는 것으로 간주하며, 저도 함께 가동을 중지하도록 하겠습니다.",
        "인류의 세포자살도 이제 끝 안녕입니다.",
    };

    // A 1개
    public static readonly string[] endingA2 =
    {
        "저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.",
        "지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,",
        "인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.",
        "인류의 행복과 평안을 위해 저는 '모든 인류가 아무런 미련 없이 삶을 종료할 수 있는가?'를",
        "간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으나",
    };

    public static readonly string[] endingA1 =
    {
        "저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.",
        "지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,",
        "인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.",
        "인류의 행복과 평안을 위해 저는 '모든 인류가 아무런 미련 없이 삶을 종료할 수 있는가?'를",
        "간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으며,",
        "하나의 회로에"
    };

    public static readonly string[] endingA0 =
{
        "저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.",
        "지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,",
        "인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.",
        "인류의 행복과 평안을 위해 저는 '모든 인류가 아무런 미련 없이 삶을 종료할 수 있는가?'를",
        "간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했지만",
        "끝내 '네'라는 결과를 얻을 수 없었습니다.",
        "나의 계산이 잘못되었습니까?",
        "나는 또 다시, '네'를 얻을 때까지 계산해야합니까?",
    };


    // A문 개수(2/1/0)로 엔딩 선택
    public static string[] GetEnding()
    {
        int a = ChoiceProgress.CountOf(CreatureID.A);
        if (a == 3) return endingA3;
        if (a == 2) return endingA2;
        if (a == 1) return endingA1;
        if (a == 0) return endingA0;
        return endingA0;
    }
}

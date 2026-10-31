using UnityEngine;
using System.Collections;
using CreatureTypes;
using UnityEngine.SceneManagement;

// Ending 씬 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
// 엔딩 연출 헬퍼(ApplyEndingAColor/ApplyEndingChoiceRooms/DropObject)와 관련 필드는 TutorialGuide.cs에 그대로 있다.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator EndingIntro()
    {
        lv4_Door.DoorCloseAndOpen(true);
        yield return null;

    }
    IEnumerator Ending_tut2()
    {
        keepAcrossRooms = true;
        yield return new WaitForSeconds(1f);

        lv4_Door.DoorCloseAndOpen(false);

        FindObjectOfType<P53Think>()?.SetGuide(tf_ending1);

        yield return SayLine("감사합니다.");
        yield return SayLine("이제 나머지 부분 정도는 스스로의 힘으로 회복할 수 있을 것 같습니다.");

        ApplyEndingChoiceRooms();
        if (endingDropTarget != null) yield return DropObject(endingDropTarget, endingDropY, endingDropDuration);
        yield return SayLine("이쪽으로 오시겠어요?");
        keepAcrossRooms = false;

    }



    IEnumerator Ending_tut3()
    {
        FindObjectOfType<P53Think>()?.SetGuide(tf_ending2);
        keepAcrossRooms = true;

        yield return SayLine("저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.");
        int a = ChoiceProgress.CountOf(CreatureID.A);
        if (a >= 3)
        {
            yield return SayLine("지금껏 저는 인간의 삶과 감정을 간략한 형태로 끝없이 시뮬레이션 했으며 그 결과 인류라는 생명체는 죽음이 임박했음을,");
            yield return SayLine("인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.");
            yield return SayLine("저는 간략화된 형태의 인간의 감정을 바탕으로 시뮬레이션 했으며,");
            yield return SayLine("이를 연산하던 구역 과반에서 '네'라는 대답을 얻었습니다.");
            yield return SayLine("아주 오래 고민하고 결정을 미뤄왔지만 당신이 도와주셨어요.");
            ApplyEndingAColor();   // 등록한 오브젝트들의 WallActivationTint 모두 A색으로

            yield return SayLine("인류의 세포자살도 이제 끝 안녕입니다.");
        }
        else
        {
            yield return SayLine("지금까지 당신이 거쳐온 곳들은 인간의 삶과 감정을 간략한 형태로 시뮬레이션하던 곳이었습니다.");
            yield return SayLine("당신이 내가 시뮬레이션하던 곳에 간섭까지 했지만, 그래도 그곳에서 답을 찾을 수 없었습니다.");
            yield return SayLine("남은 인류는 외부 환경을 버티지 못해, 모두 저의 내부로 이주했습니다만");
            yield return SayLine("그들은 끝없이 작고 나약해졌고 제가 할 수 있는 최선은 현상유지였습니다.");
            yield return SayLine("저 아래, 이곳 사람들의 기억을 추출하고 정제하는 공정이 이루어지는 곳이 있습니다.");
            yield return SayLine("그것들을 보고 알아와주시겠나요? 이 이상으로 삶에 긍정할 가치가 있는지?");
        }
        keepAcrossRooms = false;

    }

    IEnumerator Ending_tut5()
    {
        yield return null;
        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(thankYouScene);
        else SceneManager.LoadScene(thankYouScene);
    }
}

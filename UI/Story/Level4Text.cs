using UnityEngine;
using System.Collections;
using CreatureTypes;

// level3 / level3_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator Level4Intro()
    {
        if (ChoiceProgress.MoreOf(CreatureID.A, CreatureID.L))
        {
            yield return SayLine("국가는 반드시 쇠퇴하며 언어도 마찬가지로 살아가다 죽었으며 인류도 탄생의 시기의 생명력은 다시 찾아볼 수 없게 되었습니다.");
            yield return SayLine("생태계의 순환이라는 명목 하에 이를 반복하고 있지만 거시적인 관점에서는 선형적이었습니다.");
        }
        else
        {
            yield return SayLine("사실은 아까 말하지 못한 것이 있습니다.");
            yield return SayLine("그들이 이대로 사라지기보다는 제가 그들이 이 상황을 타개할 방법을 계산해낼 때까지 사람들을 보존해두는 게 나을 것 같아...");
            yield return SayLine("그냥 그렇게 얼려두었습니다.");
            yield return SayLine("저는 사람들이 갖고 있던 기억을 바탕으로 생물을 만들어 시뮬레이션을 계속했습니다만...");
            yield return SayLine("...");
        }

    }
    IEnumerator Lv4AfterIntro()
    {
        lv4_Door.DoorCloseAndOpen(true);
        yield return null;
    }
}

using UnityEngine;
using System.Collections;
using CreatureTypes;

// level3 / level3_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator Level4Intro()
    {
        yield return SayLine("사실은 말하지 않은 게 있는데, 말해도 괜찮을까요?");

        if (ChoiceProgress.MoreOf(CreatureID.A, CreatureID.L))
        {
            yield return SayLine("가공하기 이전 데이터의 출처는 제 시설 내부로 이주한 사람들, 그들의 기억입니다. 당신도 포함해서요.");
            yield return SayLine("남은 자원은 끝없이 고갈되었고, 그들을 보호하는 동시에 연산하는 것은 매우 어려운 일이었기에 답을 찾을 때까지 보존해두는 것이 저의 최선이었습니다.");
            yield return SayLine("하지만 저의 고장도 그렇고, 이제는 결론을 내려야겠죠.");

        }
        else
        {
            yield return SayLine("저의 내부에 있는 생물은, 추출하고 정제된 데이터를 기반으로 움직입니다. 여기까지 말씀 드렸었죠.");
            yield return SayLine("제가 추출해오는 곳... 그 데이터의 출처가 제 시설 내부로 이주해온 사람들이라는 것입니다.");
            yield return SayLine("그들이 사라지게 되면 저의 존재 이유도 사라지므로 저는 저의 시설 내부로 이주한 사람들을 보존하는 것을 택했습니다... ");
            yield return SayLine("당신도 저의 일부라면, 나의 시설 내부에서 활동하는 생물이라면 저를 이해해주리라고 믿습니다.");
        }

    }
    IEnumerator Lv4After_tut2()
    {
        yield return SayLine("생산되는 에너지, 활동하는 생물들, 모두 남은 사람들의 기억을 바탕으로 합니다.");
        yield return SayLine("그러므로 생산된 에너지를 바탕으로 연산을 이어나갈지, 또는 모두 끝낼지 판단할 기준이 될 수 있다고 생각합니다.");

        yield return SayLine($"에너지를 생산 중인 방이 총합 20개 이상이 되면, 저를 바라보고 {Pim.possessKey}를 눌러주십시오.");
        yield return SayLine($"또한, 이전에 왔던 구역은 언제든지 돌아갈 수 있습니다. 준비가 되면 알려주십시오.");

    }

}

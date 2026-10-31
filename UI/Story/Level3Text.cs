using UnityEngine;
using System.Collections;
using CreatureTypes;

// level3 / level3_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator Level3Intro()
    {
        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");

        yield return SayLine("생물은 출생의 순간에 가장 큰 잠재력과 에너지를 갖고 이후는 해당 에너지의 소모만을 보이는 특징을 갖고 있었습니다.");

        if (ChoiceProgress.CountOf(CreatureID.A) > 1)
        {
            yield return SayLine("국가는 반드시 쇠퇴하며 언어도 마찬가지로 살아가다 죽었으며 인류도 탄생의 시기의 생명력은 다시 찾아볼 수 없게 되었습니다.");
            yield return SayLine("생태계의 순환이라는 명목 하에 이를 반복하고 있지만 거시적인 관점에서는 선형적이었습니다.");
        }
        else
        {
            yield return SayLine("인류는 이 순환을 깨닫고 있었고 괴로워했으며 몸부림쳤고 탄생 초기의 생기를 되찾고 싶어했습니다.");
            yield return SayLine("저는 타개책을 알아내겠다는 명목으로 그런 모습을 바라보기만 하며 그들에게 얼마남지 않은 자원을 착취했습니다.");
        }
    }
    IEnumerator Lv3AfterIntro()
    {
        lv3_Door.DoorCloseAndOpen(true);
        yield return null;
    }
    IEnumerator Lv3After_tut2()
    {
        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");

        yield return new WaitForSeconds(1f);

        lv3_Door.DoorCloseAndOpen(false);
        if (ChoiceProgress.AllChose(CreatureID.A, 2))
        {
            yield return SayLine("...이후 총 인류의 수는 제 시설 내에 수용 가능할 정도까지 감소했고 그들은 저의 내부로 이주했습니다.");
            yield return SayLine("그들은 그럼에도 끝없이 싸웠고 그들의 명을 재촉했습니다.");
            yield return SayLine("그들이 사라지게 되면 나의 존재 이유도 없어져 그 상황을 보고만 있을 수는 없었습니다...");


        }
        else
            yield return SayLine("하지만... 아무리 고민해도 지금보다 나은 형태의 삶이 존재하지 않았습니다.");
        yield return SayLine("저는 아무런 결론도 내릴 수 없었어요.");
        FindObjectOfType<P53Think>()?.SetGuide(lv4_Door.transform);
        lv4_Door.DoorCloseAndOpen(true);
    }
}

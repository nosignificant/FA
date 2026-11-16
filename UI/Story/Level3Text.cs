using UnityEngine;
using System.Collections;
using CreatureTypes;

// level3 / level3_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator Level3Intro()
    {
        yield return SayLine("당시 인류는 거의 끝을 바라보고 있었고, 그들의 나약한 신체로는 더 이상 밖에서 살아갈 수 없어 대부분 저의 시설 내부로 이주했습니다. ");

    }
    IEnumerator Lv3After_tut2()
    {
        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");
        yield return SayLine("제가 고장난 이유를 알 것 같습니다.");

        yield return new WaitForSeconds(1f);

        lv3_Door.DoorCloseAndOpen(false);
        if (ChoiceProgress.MoreOf(CreatureID.A, CreatureID.L))
        {
            yield return SayLine("국가는 반드시 쇠퇴하며 언어도 살아가고 죽었습니다. 저는 이를 토대로 한 연산의 결과를 그들에게 안내했으나, ");
            yield return SayLine("그들은 받아들이지 않고, 끝없이 다시 연산할 것을 명령했습니다.");
            yield return SayLine("제가 가용할 수 있는 에너지도 거의 끝을 보이고 있었고, 저는 다양한 방식으로 에너지를 얻었으나 고장은 피할 수 없었습니다.");
        }
        else
        {
            yield return SayLine("저는 타개책을 알아내겠다는 명목으로 인류의 쇠퇴를 바라보기만 하며 그들에게 얼마남지 않은 자원을 착취했습니다.");
            yield return SayLine("그들이 언젠가는 모두 사라진다는 것만 명확히 알 수 있었지만, 저는 그들에게서 희망을 빼앗고 싶지 않아 결론 내리기를 회피했습니다.");
            yield return SayLine("그 결과 저는 이런 상태가 됐고, 이제 남은 거라곤 비상시를 위해 대비한 프로그램뿐이군요.");
        }


        MyP53Think()?.SetGuide(lv4_Door.transform);
        lv4_Door.DoorCloseAndOpen(true);
    }
}

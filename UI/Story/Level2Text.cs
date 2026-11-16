using UnityEngine;
using System.Collections;

// level2 / level2_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator lv2_0(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine("벽을 파괴하거나 수복하는 생물이 있는데, 그게 발신기까지 도달하는 게 도움이 될지도 모릅니다.");
        ShowPanelText("또, 방이 특정 에너지를 생산해야만 활성화되는 생물도 있습니다.");
        doneRooms.Add(room.roomID);

    }

    IEnumerator Lv2After_tut2()
    {
        keepAcrossRooms = true;
        yield return new WaitForSeconds(1f);
        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");

        lv2_Door.DoorCloseAndOpen(false);
        yield return SayLine("저는 인류 전체의 효율적인 자원 관리를 위해 설계되었고, 최근까지 그와 관련된 문제의 답을 찾고 있었습니다.");
        yield return SayLine("제 설비들이 고장나기 전, 대부분의 학문은 새로이 정립되고 있었습니다.");
        yield return SayLine("그 이유는 계 내부에 일정 이상 복잡도를 가질 때 그것은 생물이라고 부를 수 있다는 발견 떄문이었는데,");
        yield return SayLine("그에 따라 인류는 생물의 수명을 완벽하게 계산해낼 수 있게 되었습니다.");
        yield return SayLine("그들은 제게 '인류'라는 생물의 수명을 계산을 맡겼습니다... ");

        MyP53Think()?.SetGuide(lv3_Door.transform);
        lv3_Door.DoorCloseAndOpen(true);
        keepAcrossRooms = false;
    }
}

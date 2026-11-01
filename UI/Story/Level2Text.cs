using UnityEngine;
using System.Collections;

// level2 / level2_after 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator lv2_0(Room room)
    {
        keepAcrossRooms = true;
        yield return SayLine("벽을 파괴하거나 수복하는 생물이 있는데, 그게 발신기까지 도달하는 게 도움이 될지도 모릅니다.");
        yield return SayLine("또, 방이 특정 에너지를 생산해야만 활성화되는 생물도 있습니다.");

        yield return SayLine($"{Pim.lockOnKey}으로 생물의 정보를 파악하며 나아가주시길 바랍니다.");
        doneRooms.Add(room.roomID);

    }

    IEnumerator Lv2AfterIntro()
    {
        lv2_Door.DoorCloseAndOpen(true);
        yield return null;

    }
    IEnumerator Lv2After_tut2()
    {
        keepAcrossRooms = true;
        yield return new WaitForSeconds(1f);
        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");

        lv2_Door.DoorCloseAndOpen(false);
        yield return SayLine("의체 사용 빈도 증가, 전자 두뇌 복제 등의 기술으로 인해 인류는 계를 구성하는 물질들의 복잡도가 임계를 넘을 시 해당 시스템 계를 생물으로 정의했습니다.", 8f);
        yield return SayLine("새로운 정의에 따라 다양한 학문과 개념이 개편되었으나, 계속되는 인류 문명의 쇠퇴는 막을 수 없었습니다.", 8f);
        MyP53Think()?.SetGuide(lv3_Door.transform);
        lv3_Door.DoorCloseAndOpen(true);
        keepAcrossRooms = false;
    }
}

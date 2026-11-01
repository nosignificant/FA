using UnityEngine;
using System.Collections;

public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator lv1_0(Room room)
    {
        yield return SayLine("아까 말씀드렸듯, 각 방에는 생물이 있고, 각 생물이 에너지를 생산해 제게 전달해주었습니다.");
        yield return SayLine("하지만 에너지를 과도하게 사용해버려서인지 대부분의 시설이 제대로 기능하고 있지 않군요...");
        doneRooms.Add(room.roomID);

    }
    IEnumerator lv1_S1E1(Room room)
    {
        keepAcrossRooms = true;
        yield return new WaitForSeconds(3f);

        yield return SayLine("문이... 제가 조작할 수도 없습니다.");
        yield return SayLine("문을 관찰하면, 문을 열 수 있는 조건을 알 수 있을 지도 모릅니다.");
        ShowPanelText($"{Pim.lockOnKey}와 {Pim.possessKey}를 통해 생물을 안내할 수 있음을 잊지 마십시오.");

        while (lv1_S1E1Door == null || !lv1_S1E1Door.isOpen) yield return null;
        keepAcrossRooms = false;
        doneRooms.Add(room.roomID);
    }

    IEnumerator lv1_S2(Room room)
    {
        keepAcrossRooms = true;

        yield return SayLine("아까 있던 문 중 하나는 L이 4 이상일 때라고 적혀있었죠.");
        ShowPanelText("그 문을 열기 위해서는, L 에너지를 생산하는 방이 4곳 이상이어야 할 것 같습니다.");
        keepAcrossRooms = false;

        doneRooms.Add(room.roomID);

    }
    IEnumerator lv1_S1(Room room)
    {
        keepAcrossRooms = true;

        yield return SayLine("문이 열렸군요!");
        yield return SayLine("이제 돌아가봅시다. 제가 다시 메모리를 읽어보겠어요...");
        if (!ReceiverConfirmedInRoom(room)) yield return SayLine("발신기(R)는 자신이 있는 방의 상태를 읽습니다. 그 방을 L이나 A로 만들면 연결된 문이 열릴 거예요.");

        keepAcrossRooms = false;

        doneRooms.Add(room.roomID);

    }
    IEnumerator Lv1AfterIntro()
    {
        lv1_Door.DoorCloseAndOpen(true);
        yield return null;

    }
    IEnumerator Lv1After_tut0()
    {
        keepAcrossRooms = true;

        yield return new WaitForSeconds(1f);
        lv1_Door.DoorCloseAndOpen(false);

        yield return SayLine("방금 수복한 영역에서 {stat:L이|A가} 잃어버린 메모리를 가지고 돌아왔네요.");
        yield return SayLine("생물이 에너지를 생산하는 원리는 다음과 같습니다...");
        yield return SayLine("LL이 정제 구역에서 에너지를 전달 받아, L을 생산합니다.");
        yield return SayLine("생산된 L은 제 시설 내부 곳곳의 상태를 점검하고 정보를 전달합니다. 필요할 경우, A로 변환됩니다.");
        yield return SayLine("이후 수명이 다하면 분해되는데, 이때 에너지가 생산되어 제게 전달됩니다.");

        lv2_Door.DoorCloseAndOpen(true);
        yield return SayLine("이런 걸 알게 됐지만 제가 계산해내야 했던 것이 뭔지는 아직 모르겠네요...");
        MyP53Think()?.SetGuide(lv2_Door.transform);

        keepAcrossRooms = false;
    }
}

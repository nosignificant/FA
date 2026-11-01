using UnityEngine;
using System.Collections;
using CreatureTypes;

// tutorial 씬 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
public partial class TutorialGuide : MonoBehaviour
{
    IEnumerator tut0_1(Room room)
    {
        keepAcrossRooms = true;
        TutorialUI.Instance?.DismissKeyboard();   // 첫 안내 시작 → 키보드 조작 이미지 숨김
        yield return SayLine("들리세요?", 3f);
        yield return SayLine("이쪽을 봐주실래요?", 3f);


        Say($"{Pim.lockOnKey}을 눌러 절 바라봐 주세요.", messageInterval);
        int lockBaseFrame = Pim != null ? Pim.lockOnPressedFrame : -1;
        while (Pim == null ||
               (Pim.lockOnPressedFrame == lockBaseFrame && (Player.Instance == null || !Player.Instance.isTracking)))
            yield return null;
        if (P53 != null) Pl.ForceLock(P53);
        yield return SayLine($"감사합니다. 이 상태는 esc를 눌러 해제할 수 있습니다.", 3f);

        yield return SayLine("저는 p53, 시설을 관리하는 인공지능입니다.");
        yield return SayLine("저의 일부가 과열로 인해 파괴되어, 대부분의 시설과 연결이 끊어졌습니다.");
        yield return SayLine("앞의 큰 방의 연결을 회복하면, 나머지의 지역의 상태를 알 수 있을 것 같습니다...");

        MyP53Think()?.SetGuide(tf_tutL);
        yield return SayLine("주변에 생물 L 또는 A가 있을 겁니다. 시설 가동에 필요한 에너지를 생산합니다...");
        yield return SayLine("일반적인 경우라면 스스로 방을 이동해 탐색을 하나 경로 안내 시스템이 고장을 일으킨 모양입니다.");
        yield return SayLine("해당 생물들을 옆의 방으로 옮겨주시겠나요?");
        Pl.Unpin();
        yield return SayLine($"다른 생물을 바라보던 중 {Pim.lockOnKey}을 다시 눌러 관찰 중인 생물을 전환할 수 있습니다.");
        // 플레이어가 L을 바라보는 순간 안내(guideTarget) 해제 (fire-and-forget 감시)
        StartCoroutine(ClearGuideWhenLockedL());

        ShowPanelText($"또, 생물을 바라보던 중 {Pim.possessKey}를 눌러 안내할 수 있습니다.");
        while (RoomSpeciesCount("tut_2", CreatureID.L) < 2)
            yield return null;
        keepAcrossRooms = false;
        doneRooms.Add(room.roomID);

    }

    // 플레이어가 L 생물을 락온하는 순간 p53 안내선을 끈다. (tut0_1에서 fire-and-forget으로 시작)
    IEnumerator ClearGuideWhenLockedL()
    {
        while (Player.Instance == null || !Player.Instance.isTracking
               || Pl.targetCreature == null || Pl.targetCreature.data.creatureID != CreatureID.L)
            yield return null;
        MyP53Think()?.ClearGuide();
    }

    IEnumerator tut0_2(Room room)
    {

        yield return LightWhenL(room);

        yield return SayLine("감사합니다... 하지만 아직 수리가 더 필요할 것 같습니다.");

        yield return SayLine($"{Pim.codexToggleKey}를 누르면 저의 상태를 파악할 수 있습니다.", 0.5f);
        while (!StatuesMenu.AnyOpen) yield return null;
        yield return SayLine($"{Pim.codexToggleKey}를 다시 눌러 해제하십시오.", 0.5f);
        while (StatuesMenu.AnyOpen) yield return null;

        yield return SayLine("저는 알아내야 하는 것이 있었고... 많은 에너지를 요하는 연산이었습니다.");
        yield return SayLine("그 이상에 대해서는... 메모리에 접근하기 어려워 말씀드리기 어려울 것 같습니다.");
        yield return SayLine("저를 계속 도와주신다면 ");

        yield return SayLine("저는 시설을 관리하는 인공지능 p53입니다.");
        yield return SayLine("다음 장소로 안내해드릴 테니, 부디 따라와주시길 바랍니다.");

        MyP53Think()?.SetGuide(tf_nextLv);

        tut0Door.DoorCloseAndOpen(true);
        doneRooms.Add(room.roomID);
    }
}

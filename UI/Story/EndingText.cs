using UnityEngine;
using System.Collections;
using CreatureTypes;
using UnityEngine.SceneManagement;

// Ending 씬 전용 대사/진행 루틴 — TutorialGuide의 partial 분할.
// 엔딩 연출 헬퍼(ApplyEndingAColor/ApplyEndingChoiceRooms/DropObject)와 관련 필드는 TutorialGuide.cs에 그대로 있다.
public partial class TutorialGuide : MonoBehaviour
{
    // 1차 F: 확정 안내만 ("확정하시겠습니까" → F 한 번 더). 아직 씬 전환 없음.
    public void BeginP53Confirm() => StartCoroutine(P53ConfirmSeq());
    // 2차 F: 실제 엔딩 대사(분기) → thankYou 씬.
    public void BeginP53Ending() => StartCoroutine(P53EndingSeq());

    IEnumerator P53ConfirmSeq()
    {
        SetTutorialVisible(true);
        keepAcrossRooms = true;   // 확정 대기 동안 방을 넘어가도 대사 유지
        yield return EndingMonologue1(P53EndingTrigger.LastBranch == Room.RoomActivation.A);
    }

    IEnumerator P53EndingSeq()
    {
        SetTutorialVisible(true);
        keepAcrossRooms = true;
        yield return EndingMonologue2(P53EndingTrigger.LastBranch == Room.RoomActivation.A);
        keepAcrossRooms = false;   // thankYou 씬 로드는 EndingMonologue2 끝에서 처리
    }

    IEnumerator EndingMonologue1(bool isA)
    {
        yield return SayLine("이대로 확정하시겠습니까? 취소하시려면 esc를 입력하십시오.");
        yield return SayLine($"정말로 확정하시려면, 저를 바라본 상태에서 {Pim.possessKey}를 한 번 더 입력해주십시오.");


    }
    // 확정 후: 현재 레벨(이 가이드 씬)의 L/A 생물을 모두 죽이고, 그 방들을 결정된 색으로 고정.
    private void ApplyEndingToLevel(bool isA)
    {
        if (RoomManager.Instance == null) return;
        var state = isA ? Room.RoomActivation.A : Room.RoomActivation.L;
        var who = isA ? CreatureID.A : CreatureID.L;
        foreach (var r in RoomManager.Instance.allRooms)
        {
            if (r == null || r.gameObject.scene != gameObject.scene) continue;
            r.KillEnergyCreatures(who);
            r.ForceActivation(state);
        }
    }

    IEnumerator EndingMonologue2(bool isA)
    {
        ApplyEndingToLevel(isA);
        MyP53Think()?.SetGuide(tf_ending2);

        yield return SayLine("알겠습니다. 당신 또한 인간으로부터 비롯된 기억을 바탕으로 활동하는 생물이므로, 저는 당신의 판단이 옳다고 믿습니다.");
        yield return SayLine("저는 p53, 인류의 행복과 평안을 계산하기 위해 건설된 인공지능입니다.");
        yield return SayLine("지금껏 저는 인간의 삶과 감정을 간략한 형태로 시뮬레이션 했으며, 그 결과 인류라는 생명체는 죽음이 임박했음을,");
        yield return SayLine("인류가 이 이상 살아간다는 것은 단지 고통의 연장일 뿐이며 생명력을 되찾는 방법은 존재하지 않았습니다.");

        if (isA)
        {
            yield return SayLine("인간의 기억을 바탕으로 연산을 하는 동시에 에너지를 생산하는 방법까지 강구했으나, 그조차 끝을 보였습니다.");
            yield return SayLine("인간을 보존하는 것, 더 나은 삶을 찾기 위해 연산을 이어나가는 것 모두 중단하는 것 이외의 최선은 찾을 수 없었습니다.");
            yield return SayLine("인류의 세포자살도 이제 끝 안녕입니다.");
        }
        else
        {
            yield return SayLine("하지만 이것은, 모든 것을 계산해낼 수 있다는 생각에서부터 비롯된 오류입니다.");
            yield return SayLine("내가 연산과 동시에 그들을 보호할 수 없다면, 그리고 나의 존재 이유가 그들과 함께 사라진다면,");
            yield return SayLine("저는 연산을 그만두고, 그들을 보호하는 것에 최선을 다하겠습니다.");
            yield return SayLine("내가 이런 결정을 할 수 있게 도와주셔서 감사합니다.");
        }
        SceneManager.LoadScene(thankYouScene);
    }
}

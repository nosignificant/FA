# farewellApoptosis 동작 치트시트

> 실제 코드 기준 동작 요약. 세부 구현은 링크된 파일 참고.

---

# 핵심 시스템

## 방 (Room) — 서로 독립된 3개 축
| 축 | 뜻 | 효과 |
|---|---|---|
| **isActive** | 시뮬 on/off | active → 방 안 생물 `LetsThink()`가 정상 틱(`waitInterval`). inactive → Think 정지(단 `simulateInactiveRooms` 켜지면 느린 틱). ※ Think만 멈춤 — 다리·텐타클·물리 관성은 잠깐 더 미끄러짐 |
| **Activation** (None/L/A) | 방 안 L/A 입자 중 우세한 쪽 | 문 열림 + `gateByRoomState` 생물 각성 기준. 우세 + `activationMinCount` 이상 + 히스테리시스(깜빡임 방지) — [ComputeActivation](Room/Room.cs) |
| **IsHot** | 과밀(발열) | `HeatLoad > heatThreshold`면 D가 깨어나 분해 |

## 생물 공통 — 움직이기 전 걸러지는 게이트
`LetsThink()` 맨 앞에서 하나라도 걸리면 **정지** ([Think2](Think/Think2.cs)):
`manualControl` / `IsGrabbed` / `isBound` / `dormant` / (`gateByRoomState`인데 방 Activation≠requiredRoomState) / (방 inactive & 상시시뮬 꺼짐)

## 생물별 동작
| 생물             | 이동  | 동작                                                                                                                                    |
| -------------- | --- | ------------------------------------------------------------------------------------------------------------------------------------- |
| **L** (9)      | ○   | 수동 입자. 상호작용 없음. 열린 문/뚫린 벽으로 흐름. 방을 **L**로 활성화 >> controllable으로 변경: 플레이어가A해서B한다는 과정 더 명확히 하기위해                                        |
| **A** (5)      | ○   | 수동 입자. 상호작용 없음. 열린 문/뚫린 벽으로 흐름. 방을 **A**로 활성화                                                                                         |
| **AA** (6)     | ○   | **H한테서 도망**. **L을 쫓아 잡고**(grab→hold `aaSingleGrabTimeout`→**A로 변환, 1:1**). LL도 추적(안 잡음). 못 잡는 L은 `giveUpTime`/`giveUpDist`로 추격 포기     |
| **LL** (14)    | ○   | 생산기. 방 isActive & 조종중 아님일 때 간격마다 **L 생성**. **AA한테서 도망**(AA 없는 인접 방으로)                                                                 |
| **H** (1)      | ○*  | 도구. 방이 L 되면(또는 startActivated) **영구 각성** → **AA 자동 추적→bindRange 안에 들면 bind**. 묶인 AA는 isBound 정지. 1H=1AA. *canMigrate 강제 안 함(조종 이동 가능) |
| **S** (3)      | ○   | 도구. 방이 **A일 때 각성**(gateByRoomState). 방 안 **BreakableWall로 직진해 부숨**. 없으면 배회. Think 추적 대상 아님                                            |
| **D** (10)     | ○   | 분해자. **방이 IsHot일 때만**. 방에서 **가장 오래된 분해가능 L/A**를 쫓아 분해(개체수 조절, 산물 없음). `decomposeCooldown`. 잡힌/조종중 개체 제외                               |
| **Timid** (15) | ○   | 겁쟁이. **문·동족 빼고 모든 생물한테서 도망**                                                                                                          |
| **T** (12)     | —   | 발신기. 몸 고정, 텐타클이 연결된 수신기(R)로 뻗음                                                                                                        |
| **R** (13)     | —   | 수신기. 연결된 방의 신호 받음                                                                                                                     |
| **Door** (97)  | —   | 조건 충족 시 열림 (아래 라벨 참고)                                                                                                                 |

## 방 이동(migration)
흘러다니는 건 **`canMigrate=true`인 생물만** ([RoomMigration](Creature/RoomMigration.cs)). S는 방에 묶임(`canMigrate=false`).
플레이어 조종 이동은 canMigrate와 무관 — `UpdateRoomMembership`이 bounds 기반이라 끌고 가면 바뀜.

**Wander 상태 자율 이동 조건** (전부 만족):
1. `canMigrate = true`
2. **쿨다운 지남** (`migrateCooldown`: L/A·AA=5초, LL=8초) — 방 도착 후 이 시간은 이동 안 함(=최소 머무는 시간)
3. **갈 수 있는 통로** — 열린 문(`isOpen`) 또는 부서진 벽(`Broken`), 그 옆방이 회피대상 없음(`ShouldAvoidRoom`)·과밀 아님(`IsCrowded`)
4. **확률 당첨** (`wanderMigrateChance`: **L/A=1(100%)**, AA·LL=0.7). 쿨다운마다 1회 추첨 → L/A는 5초 지나면 통로 있는 한 바로 이동
5. **근처 weed 없음** (있으면 3~10초 머무름, `TryLingerNearWeed`)

통로에 `migrateDoorReachDist`(L/A=13) 이내로 붙으면 옆방 중심으로 관통.
**방 소속 변경**([Creature.UpdateRoomMembership](Creature/Creature.cs)): 위치가 옆방 bounds에 들어가고 **그 지점이 통로(열린 문/부서진 벽) 근처(`passageReach`=5)일 때만** 등록. 통로 없으면 안 바뀜 → **안 부서진 벽 뚫고 넘어가는 것 방지**(`NearConnectingPassage`). 되돌림(PushInsideRoom)은 없음 — 막히면 현재 방 유지.
**도구(canMigrate=false, S 등)는 방 소속 고정** — 경계 벽 쫓아 넘어가도 방 안 바뀜(조종 중이면 예외).
**이동 포기** — 이동하기로 정하면(`wanderDrifting`) 통로로 향하는데, `migrateTimeout`(기본 5초) 안에 못 넘어가면 **포기 → wander 복귀**(쿨다운 후 재추첨). 막혀서 통로만 바라보는 것 방지.
**재방문 금지 없음** — 방금 온 방으로도 쿨다운·확률만 맞으면 바로 되돌아감(과밀 제외는 유지).

## 문 HUD 라벨 (lock-on 시) — name / verb / object 3칸
- **RoomState**: `open / at / A` (방이름 안 씀)
- **LevelCount**: `rooms ≥ / N`
- **SignalGate**: 게이트 이름 (AND/OR/NOT/equals)
- **LevelChoice**: `level{N} / is / {기록상태}` — 지정 레벨(`choiceLevelIndex`)을 A/L 중 무엇으로 클리어했는지(ChoiceProgress) 표시. **개폐 안 함(표시/tint 전용)**. 기록 없으면 `None`
- **커스텀 라벨**: `useCustomLabel` 켜면 `customName/customVerb/customObject` 3칸을 모드 무관하게 그대로 표시
- 등록 주의: **비활성(SetActive off) 문은 `Door.Start`가 안 돌아 방에 등록 안 됨** → 통로 인식 못 함. 상시 통로는 문 켜둔 채 `Always Open` + 메시만 숨길 것
- **`Always Open` 문**: 통로 유지 + **락온 제외**(`self.lockable=false`) + **불 안 켬**
- **특수 벽 tint**: [WallActivationTint](Room/WallActivationTint.cs)에 `byLevelChoice` 켜면 방 상태 대신 **지정 레벨 클리어 선택(A/L) 색**으로 고정(Start에서 1회). `choiceLevelIndex`로 레벨 지정

---

# 연출 (팝업 / 카메라)

## 상태 변화 팝업 (텍스트 없는 색 마커)
공용 발사: `Popup.BurstDirectional(pos, dir, msg, color)` — 위치·크기·속도·수명 랜덤. Popup 인스펙터 `Directional Size Range`/`Directional Life Jitter`/`speed`.

**① 방 상태 변화** — 방이 L/A로 바뀌는 순간([Room.AnnounceActivation](Room/Room.cs)) **Room 자식의 모든 BreakableWall**에서 방 중심으로 발사(L=초록/A=빨강).
- 위치: `GetComponentsInChildren<BreakableWall>(true)` 각 벽의 `WallPoint()`(렌더러 bounds 안 랜덤). ⚠️ `breakableWalls` 등록 리스트 안 씀(breakable=false인 tint 전용 벽이 빠지므로)
- Room 인스펙터: `Announce Activation Popup`, `Popups Per Wall`(기본 2), `Announce L/A Color`

**② 생물 각성** — `IsDormant()` true→false 전환 시([Think2.FireActivationPopup](Think/Think2.cs)) 생물 위치에서 방 상태 색으로 방사.
- Think2 인스펙터: `Activation Popup`, `Activation Popup Count`(기본 3), `Activation Popup Cooldown`(기본 2초)

## DoF 블러 (focusDistance 조절)
[StatuesMenu.Update](UI/Story/StatuesMenu.cs)가 매 프레임 상태에 따라 **DoF(Bokeh) `focusDistance`**를 목표값으로 수렴(`blurFadeSpeed`, unscaledTime):
- **상태창(C) 열림 → `statusFocus`(0, 전체 흐림)** — 우선순위 최상
- **생물 조종 중 → `possessFocus`(15, 약한 흐림)** — `CreaturePossess.IsPossessing`
- **그 외 → 원래 focusDistance**(Start에서 저장한 값)
- Volume은 StatuesMenu `DoF Volume`에 직접 넣거나, 비우면 **PlayerLockOn 카메라에서 자동 탐색**. DoF 오버라이드(Bokeh, active, weight 1) 필요.
- 평소 선명하려면 DoF 기본 `Focus Distance`/`Aperture`를 적당히 둘 것(이 값이 "그 외" 복구값).

---

# 색 (CreatureActivationTint)
- `tintByRoomState` ON: 방 L/A/None 색으로 칠함.
- `tintByRoomState` OFF: **각성=프리팹 원래 색 복원 / 휴면=`Dormant Color`**. (Active Color 필드 없음 — 각성은 원본 그대로)
- 텍스처 스왑: `swapTexture` ON이면 휴면=텍스처 제거 / 각성=`activeTexture`(비우면 원본).

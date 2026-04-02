# 싼 월세집은 나 혼자가 아니었다 — 프로젝트 구조 설계
> **Status**: 초안 | **Created**: 2026-04-01
> **엔진**: Unity 6 (6000.3.12f1) | **언어**: C#
> **참조**: [stats-design.md](stats-design.md) · [ui-layout.md](ui-layout.md) · [scenes.md](scenes.md)

---

## 1. 디렉토리 구조

```
Assets/
├── Scripts/
│   ├── Core/                    # 게임 루프, 매니저
│   │   ├── GameManager.cs       # 싱글톤, 전체 게임 상태 관리
│   │   ├── TimeManager.cs       # 시간 체계 (턴, 시간대, 보름달 주기)
│   │   ├── SaveManager.cs       # 저장/로드
│   │   └── EventBus.cs          # 이벤트 시스템 (시스템 간 통신)
│   │
│   ├── Stats/                   # 수치 시스템
│   │   ├── StatSystem.cs        # 스탯 기반 클래스 (범위, 클램프, 변동)
│   │   ├── PlayerStats.cs       # HP, SAN, 포만감, 양기, GOLD
│   │   ├── GhostStats.cs        # 호감도, 만족도, 성감맵
│   │   ├── SensitivityMap.cs    # 부위별 성감 수치 + 유형별 곡선
│   │   └── StatModifier.cs      # 수치 변동 처리 (버프/디버프)
│   │
│   ├── SIM/                     # 생활 시뮬레이션
│   │   ├── ActionSystem.cs      # 행동 선택 + 턴 소비
│   │   ├── Actions/             # 개별 행동
│   │   │   ├── EatAction.cs
│   │   │   ├── WorkAction.cs
│   │   │   ├── SleepAction.cs
│   │   │   ├── PhoneAction.cs
│   │   │   ├── CleanAction.cs
│   │   │   ├── VRAction.cs
│   │   │   ├── GameAction.cs
│   │   │   ├── MusicAction.cs
│   │   │   └── MeditateAction.cs
│   │   ├── EconomySystem.cs     # 수입/지출/고정비/할부
│   │   ├── WorkEventSystem.cs   # 출근 랜덤 이벤트 15종
│   │   └── InventorySystem.cs   # 인벤토리 (식품, 제물 등)
│   │
│   ├── Horror/                  # 공포 시스템
│   │   ├── HorrorEventSystem.cs # 공포 이벤트 발생 규칙 + 에스컬레이션
│   │   ├── HorrorEvents/        # 개별 공포 이벤트
│   │   │   ├── SoundEvent.cs
│   │   │   ├── ShadowEvent.cs
│   │   │   ├── ObjectMoveEvent.cs
│   │   │   └── TemperatureEvent.cs
│   │   └── GhostBehavior.cs     # 귀신 행동 패턴 (막별 변화)
│   │
│   ├── ARC/                     # 아케이드 인터랙션
│   │   ├── ARCBase.cs           # ARC 공통 (성공/실패 분기)
│   │   ├── SleepParalysis.cs    # 가위눌림 (키 연타)
│   │   ├── HeadTurn.cs          # 고개돌리기 (균형 조절)
│   │   └── BreastFeeding.cs     # 젖빨기 (좌우 조작)
│   │
│   ├── SEX/                     # 성감/성교 시뮬레이션
│   │   ├── AnchorSystem.cs      # 4앵커 (입/왼손/오른손/성기)
│   │   ├── IntensitySystem.cs   # 강도 (1~10)
│   │   ├── ExcitementSystem.cs  # 흥분도 (0~100) + 절정
│   │   ├── PositionSystem.cs    # 자세 관리 + 호감도 소모
│   │   └── SensitivityCurves.cs # 성감 유형 12종 곡선 계산
│   │
│   ├── VN/                      # 비주얼 노벨 연출
│   │   ├── DialogueSystem.cs    # 대사창 표시, 타이핑 효과
│   │   ├── DialogueData.cs      # 대사 데이터 구조 (ScriptableObject)
│   │   ├── ChoiceSystem.cs      # 선택지 표시 + 분기
│   │   ├── CharacterDisplay.cs  # 캐릭터 스프라이트 관리
│   │   ├── VNController.cs      # VN 진행 제어 (스킵/오토)
│   │   └── ScriptParser.cs      # 외부 스크립트 파싱 (Ink 또는 자체 포맷)
│   │
│   ├── UI/                      # UI 시스템
│   │   ├── HUDController.cs     # 시간 표시, 환경 피드백
│   │   ├── PhoneUI.cs           # 핸드폰 (쇼핑/배달/시간떼우기)
│   │   ├── InventoryUI.cs       # 인벤토리 UI
│   │   ├── SettingsUI.cs        # 설정 (텍스트 속도, 오토, 해상도 등)
│   │   └── TransitionUI.cs      # 모드 전환 연출
│   │
│   ├── Ending/                  # 엔딩 시스템
│   │   ├── EndingChecker.cs     # 엔딩 분기 조건 체크
│   │   └── EndingData.cs        # 엔딩별 데이터
│   │
│   └── Data/                    # 데이터 정의
│       ├── GameConstants.cs     # 상수 (수치 범위, 기본값 등)
│       ├── FoodData.cs          # 음식 데이터
│       ├── ItemData.cs          # 아이템 데이터
│       └── EventData.cs         # 이벤트 데이터
│
├── Data/                        # ScriptableObject 에셋
│   ├── Foods/
│   ├── Items/
│   ├── Dialogues/               # 대사 데이터 파일
│   └── Events/
│
├── Art/                         # 아트 에셋
│   ├── Characters/
│   ├── Backgrounds/
│   ├── UI/
│   └── Effects/
│
├── Audio/
│   ├── BGM/
│   ├── SFX/
│   └── Ambience/
│
├── Scenes/                      # Unity 씬
│   ├── Title.unity
│   ├── Game.unity               # 메인 게임 (SIM 루프)
│   └── Ending.unity
│
└── Prefabs/
    ├── UI/
    └── Effects/
```

---

## 2. 핵심 시스템 설계

### 2-1. 게임 루프 (GameManager)
```
[게임 시작]
    ↓
[아침] → 행동 선택 (2~3턴)
    ↓
[낮] → 출근 or 자유 행동 (2~3턴)
    ↓
[저녁] → 퇴근, 행동 선택 (2~3턴)
    ↓
[밤] → 행동 선택 (2~3턴)
    ↓
[심야] → 수면 (자동) → 가위눌림/젖물리기/몽정 판정
    ↓
[다음 날] → 스탯 정산, 이벤트 체크, 엔딩 체크
    ↓
[반복]
```

### 2-2. 이벤트 시스템 (EventBus)
시스템 간 결합도를 낮추기 위해 이벤트 버스 패턴 사용.
```
StatChanged → UI 업데이트, 환경 피드백 변경
TurnEnded → 공포 이벤트 판정, 포만감 하락
DayEnded → 고정비 차감, 엔딩 조건 체크
MoonPhaseChanged → 보름달 이벤트 트리거
```

### 2-3. 대사/연출 데이터 관리
- **외부 파일 기반**: 대사/연출 데이터를 코드에 하드코딩하지 않음
- **스크립트 파일** (JSON 또는 Ink): 대사, 분기, 연출 지시를 외부 파일로 관리
- **ScriptableObject**: 음식/아이템/이벤트 등 게임 데이터
- **수정 용이성**: 코드 변경 없이 대사/연출을 추가·수정·삭제 가능

### 2-4. 대사 스크립트 포맷 (자체 정의)
```json
{
  "id": "act1_first_night_01",
  "act": 1,
  "trigger": "first_sleep",
  "lines": [
    {
      "speaker": null,
      "text": "...이상한 소리가 들린다.",
      "effect": "screen_shake",
      "duration": 0.5
    },
    {
      "speaker": null,
      "text": "아무것도 아니겠지.",
      "choices": [
        { "text": "무시하고 잔다", "next": "act1_first_night_02a" },
        { "text": "확인한다", "next": "act1_first_night_02b" }
      ]
    }
  ]
}
```

---

## 3. 구현 우선순위

### Phase 1: 코어 루프
1. GameManager + TimeManager (턴/시간대/일수)
2. StatSystem + PlayerStats (HP/SAN/포만감/양기/GOLD)
3. ActionSystem (행동 선택 → 스탯 변동 → 턴 소비)
4. EconomySystem (수입/지출/고정비)
5. 기본 UI (시간 표시, 행동 메뉴)

### Phase 2: VN + 이벤트
6. DialogueSystem (대사창, 타이핑, 스킵/오토)
7. ScriptParser (외부 대사 파일 로드)
8. ChoiceSystem (선택지)
9. HorrorEventSystem (공포 이벤트 발생)
10. WorkEventSystem (출근 랜덤 이벤트)

### Phase 3: ARC + SEX
11. ARCBase + 가위눌림/고개돌리기
12. GhostStats (호감도/만족도)
13. SensitivityMap + 성감 곡선
14. AnchorSystem + IntensitySystem
15. PositionSystem (자세 + 호감도 소모)

### Phase 4: 통합 + 엔딩
16. EndingChecker (전 엔딩 분기)
17. SaveManager (저장/로드)
18. SettingsUI (텍스트 속도, 난이도 등)
19. 밸런스 조율

---

## 미확정
- [ ] Ink 도입 여부 (자체 JSON 포맷으로도 충분한지 평가 후 결정)
- [ ] 렌더 파이프라인 (URP vs Built-in)
- [ ] 오디오 시스템 상세 (BGM 전환, 공포 효과음 등)

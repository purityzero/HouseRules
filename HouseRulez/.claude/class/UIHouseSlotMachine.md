# UIHouseSlotMachine

연관: [[UISlotMachineReel]], [[UISlotMachineSymbol]], [[UISlotMachineReelState]](Glory, `Assets/Scripts/Glory/UI/SlotMachine/`), `UIHouseSlotReel`, `UIHouseSlotSymbol`(이 문서의 프로젝트 파생), `HouseRecord`/`HouseSpriteLoader`(`Assets/Scripts/Table/HouseRecord.cs`), `PlayerManager.GetSelectedHouseRecord()`

## 2026-08-26-0 — 3x3 슬롯머신 프로젝트 연결부 신설

### 개요
Glory 릴 프레임워크(이식 완료, `.claude/class/UISlotMachineReel.md` 참고)에 HouseRulez 종족 말 스프라이트를 물려 실제로 돌아가는 3x3 슬롯머신을 만들었다. 판정 규칙(전력→소환 수 환산)이 아직 미정이라 판정기는 없다 — 결과가 주입되지 않으면 무작위로 굴러간다.

### 만든 파일
- `Assets/Scripts/InGame/Slot/UIHouseSlotSymbol.cs` — `HouseSlotSymbolSprite`(struct) + `UIHouseSlotSymbol`
- `Assets/Scripts/InGame/Slot/UIHouseSlotReel.cs` — `UIHouseSlotReel`
- `Assets/Scripts/InGame/Slot/UIHouseSlotMachine.cs` — `UIHouseSlotMachine`

프리팹(`Assets/Resources/Prefab/UIHouseSlotMachine.prefab`)은 **만들지 않았다** — 아래 "프리팹 미작성 사유" 참고.

### 계층 구조 (런타임 생성, 프리팹 미존재)
```
UIHouseSlotMachine (컨트롤러)
├─ FrameImage (Image) — frame_{종족키} 교체 대상
├─ HeaderBarImage (Image, 선택) — AccentColor 교체 대상
├─ SymbolTemplate (UIHouseSlotSymbol, 비활성 원본 1개) — Awake에서 SetActive(false)
└─ ReelList[3] (UIHouseSlotReel) — 각 릴이 Awake에서 SymbolTemplate을 m_SymbolCountPerReel(기본 7)개 복제해 자기 자식으로 둔다
```

### Glory 베이스와의 역할 분담
| 무엇 | 담당 |
|---|---|
| 릴 가감속/순환 버퍼 스크롤/FSM 4상태 전이 | Glory `UISlotMachineReel`(베이스, 그대로 사용) |
| 결과 정착 트윈, 블러 속도 임계값 | Glory `UISlotMachineReel`(베이스) |
| **칸 오브젝트를 몇 개 만들지 + 실제로 만드는 것** | `UIHouseSlotReel.BuildSymbols()` — 베이스엔 이 API가 없어 추가 |
| **스핀 중 어떤 심볼이 굴러올지 (콜백 내용)** | `UIHouseSlotReel.ApplySpritePool()` — `OnRequestSymbol`에 종족 풀 범위 무작위 함수를 채움 |
| **블러 시 어떤 스프라이트로 바뀌는지** | `UIHouseSlotSymbol.SetBlur()` 오버라이드 — 베이스는 빈 확장 지점만 제공 |
| 종족 스프라이트 풀 로딩(1회) + 릴 3개에 분배 | `UIHouseSlotMachine.BuildSpritePool()` — 심볼 칸마다 중복 조회 방지 |
| 프레임/색 교체, 릴 3개 동시 스핀/순차 정지 지휘 | `UIHouseSlotMachine` |
| 판정(전력→소환 수 환산) | **없음** — 아래 참고 |

### 심볼 타입(int)의 의미
`UISlotMachineSymbol.symbolType`은 Glory 입장에선 의미 없는 정수다. 이 프로젝트에서는 **`Apply(record)` 시점에 로드한 종족 스프라이트 풀의 인덱스**로 쓴다 — `UIHouseSlotSymbol`은 자기 풀을 직접 들지 않고 `SetSpritePool()`로 주입받은 뒤 `symbolType`을 인덱스로 클램프해 참조한다(풀은 `UIHouseSlotMachine`이 한 번만 로드해 릴 3개에 공유).

### 칸 수를 7로 정한 근거
`UISlotMachineReel`은 `m_SymbolList` 전체를 순환 스크롤 버퍼로 쓰고, 보이는 창은 `(버퍼 칸 수 - 결과 개수) / 2`로 가운데 정렬한다(`.claude/class/UISlotMachineReel.md` 참고). 보이는 3칸에 위아래로 스크롤 버퍼가 필요해 대칭 2칸씩 얹어 **7칸**(2+3+2)으로 잡았다 — `UIHouseSlotMachine.m_SymbolCountPerReel`(인스펙터 필드)로 조정 가능. 실제 프리팹에서 칸 크기(32px)와 릴 RectTransform 높이가 이 칸 수와 맞물려야 스크롤이 매끄럽다 — 프리팹을 조립할 때 릴 루트 RectTransform.height ≈ 32 * 7 = 224 정도로 맞출 것.

### 런타임 생성 방식
`BuildSymbols()`는 `SetSymbols()` 정식 API로 런타임에 생성된 칸 목록을 주입한다(2026-08-26 추가). 프리팹에 미리 깔아둔 칸은 인스펙터 직렬화 값이 유지된다.

### Public API
**`UIHouseSlotSymbol`** (`Assets/Scripts/InGame/Slot/UIHouseSlotSymbol.cs:14`)
- `public void SetSpritePool(IReadOnlyList<HouseSlotSymbolSprite> _spritePool)`
- `protected override void SetBlur(bool _isBlur)`

**`UIHouseSlotReel`** (`Assets/Scripts/InGame/Slot/UIHouseSlotReel.cs:8`)
- `public void BuildSymbols(UIHouseSlotSymbol _symbolTemplate, int _symbolCount)`
- `public void ApplySpritePool(IReadOnlyList<HouseSlotSymbolSprite> _spritePool)`

**`UIHouseSlotMachine`** (`Assets/Scripts/InGame/Slot/UIHouseSlotMachine.cs:9`)
- `public void Apply(HouseRecord _record)` — 종족 교체 진입점. 스프라이트 풀 재로드 + 프레임/색 교체 + 릴 3개 `Open()`.
- `public void Spin()` — 릴 3개 동시 스핀 시작.
- `public void SetResult(int[][] _resultSymbolTypesByReel)` — 판정기가 결과를 확정하면 호출(릴 인덱스 → 그 릴의 보이는 칸 결과). `Spin()`과 `StopAll()` 사이에서 호출하는 것을 전제.
- `public void StopAll()` — 릴마다 `m_ReelStopInterval` 간격으로 순차 정지. `SetResult()`가 호출되지 않았으면 내부에서 무작위 결과를 만들어 채운 뒤 정지시킨다.

### 판정기 연결이 왜 비어 있는가
전력(파워) → 소환 수 환산 규칙이 GDD/기획 확정 전이라 "무엇을 3x3 결과로 환산할지"를 정의할 수 없다. `UIHouseSlotMachine.SetResult()`/`StopAll()` 사이에 `// TODO:` 주석으로 연결 지점만 표시해뒀다 — 규칙이 정해지면 판정기가 `Spin()` 직후(또는 스핀 연출 도중) `SetResult(int[][])`를 호출해 끼워 넣으면 된다. 그 전까지는 `StopAll()`이 스프라이트 풀 범위에서 무작위로 채워 항상 정상적으로(막히지 않고) 굴러간다.

### 프리팹 미작성 사유
`Assets/Resources/Prefab/UIHouseSlotMachine.prefab`을 만들지 않았다:
- 이 프로젝트에는 **참고할 기존 프리팹이 하나도 없다**(`find Assets -iname "*.prefab"` 결과 0건) — PREFAB.MD의 "신규 블록 작성 순서 1) 동일 컴포넌트가 쓰인 기존 프리팹을 grep으로 탐색"이 원천적으로 불가능해, YAML을 순수 창작해야 하는 상태다.
- Unity MCP 미연결로 컴파일/실행 검증도 불가능해, 손으로 쓴 프리팹이 깨져도 그 자리에서 확인할 방법이 없다.
- 프레임 이미지(`frame_{종족키}.png`)가 다른 에이전트에 의해 아직 생성 중이라 최종 크기/레이아웃을 지금 확정하기 이르다.
- 스크립트 3개는 모두 완성되어 있으므로, Unity 에디터에서 인스펙터로 직접 조립하는 편이 안전하다(아래 "인스펙터 조립 가이드" 참고).

### 인스펙터 조립 가이드 (사용자 작업)
1. 빈 GameObject에 `UIHouseSlotMachine` 부착 → Canvas 하위에 배치.
2. `FrameImage`용 자식 `Image` 1개, 선택적으로 `HeaderBarImage`용 자식 `Image` 1개 생성 후 필드 연결.
3. `UIHouseSlotSymbol`이 붙은 자식(자기 밑에 `Image`가 연결된 32x32 정도 크기)을 1개 만들어 **비활성 상태로** 두고 `SymbolTemplate` 필드에 연결.
4. 릴 루트로 쓸 빈 GameObject(RectTransform, height ≈ 224) 3개를 만들어 각각 `UIHouseSlotReel` 부착 → `ReelList` 배열에 순서대로 연결. 릴 루트는 프레임 안쪽 96x96 창(3칸x32px) 안에 가로로 나란히 배치.
4-1. **마스크 필수(2026-08-26 보완)**: 릴 루트 3개의 공통 부모로 "보이는 창" 크기(3칸 분량, 예: 96x96)의 빈 GameObject를 하나 두고 `RectMask2D`를 부착한 뒤 그 밑에 릴 3개를 자식으로 넣는다. 릴 루트 실제 높이(칸수 7 × 칸크기)가 보이는 창보다 훨씬 크므로, 마스크가 없으면 스크롤 중인 위아래 버퍼 칸들이 프레임 밖으로 삐져나와 화면 전체에 흘러다닌다(`InGameScene.unity`의 `ReelWindow`가 이 역할, `.claude/class/InGameScene.md` 참고 — 최초 작성 시 이 문서에서 빠져 있던 단계).
5. `m_SymbolCountPerReel`(기본 7), `m_ReelStopInterval`(기본 0.2초)는 필요시 인스펙터에서 조정.
6. 완성 후 `PlayerManager.instance.GetSelectedHouseRecord()`로 얻은 레코드를 `Apply()`에 넘겨 첫 세팅, 이후 `Spin()`/`StopAll()`로 구동.

### 2026-08-26-1 — 검토 세션: 누락된 .meta 보완
`UIHouseSlotMachine.cs`/`UIHouseSlotReel.cs`/`UIHouseSlotSymbol.cs` 3개와 신규 폴더(`Assets/Scripts/InGame`, `Assets/Scripts/InGame/Slot`)의 `.meta`가 빠져 있어 프로젝트 전체 GUID와 겹치지 않는 새 GUID로 보완했다(같은 폴더의 기존 스크립트 `.meta` 포맷을 그대로 따름 — `fileFormatVersion`/`guid`만 있는 최소 포맷). Glory API 호출 정합성(`SetSymbols`/`Open`/`SetResult`/`AllBlur`/`OnRequestSymbol`/`SetBlur` 접근 수준 등), 칸 수 7 유도식((7-3)/2=2, 정확히 가운데), 블러 파일명 규칙은 모두 실제 코드와 대조해 이상 없음을 확인했다.

### 검증 상태 — 미검증
이번 세션은 Unity MCP가 연결되지 않아 컴파일/Play Mode 확인을 하지 못했다. 코드 리딩 기반으로만 작성했다. `BuildSymbols`로 늘어난 칸 수에서 `PosmaxDownY`/`AnswerPosY` 스크롤이 매끄럽게 보이는지, 블러 스프라이트 파일명 규칙(`{원본이름}_blur`)이 실제 리소스 파일명과 정확히 일치하는지 모두 **실제 프리팹 조립 + 플레이 테스트로 확인 필요**.

### 2026-08-26-2 — InGameScene에 실제 배치
프리팹 대신 `Assets/Scenes/InGameScene.unity`에 직접 배치해 처음으로 실제 씬에 연결했다. 계층/좌표/마스크 구조는 `.claude/class/InGameScene.md` 참고. `Apply()`/`Spin()`/`StopAll()` 호출부는 `InGameScene.OnSetup()`/`OnClickSpinButton()`.

## 2026-08-30-0 — 판정과 화면이 같은 3×3을 쓰도록 좌표계 통합

### 증상
릴이 스스로 무작위를 굴려 멈추면, 화면에 보이는 3×3과 `Judge`가 판정한 3×3이 서로 다른 값이 된다.
"보이는 대로 소환된다"가 깨진다.

### 원인
결과 생성 주체가 둘이었다. 릴(`Glory` 슬롯머신)이 자기 결과를 만들고, 판정기는 별도의 배열을 받았다.

### 수정 — 결과를 먼저 만들고 그 하나로 둘 다 돌린다
| 추가 멤버 | 하는 일 |
|---|---|
| `CreateRandomGrid()` | 심볼 개수 안에서 9칸 무작위 배열을 만든다. 칸 인덱스 = `row * 3 + reel` |
| `SetResultByGrid(int[])` | 그 배열대로 릴 3개의 정지 결과를 지정한다 |
| `spritePool` | 이미 로드해 둔 `HouseSlotSymbolSprite` 목록 공개. 전장/전투가 재로드 없이 그대로 쓴다 |

호출 순서(`InGameScene.CoSpinAndStop`):
`Spin()` → `CreateRandomGrid()` → `SetResultByGrid(grid)` → `Judge.Evaluate(houseKey, grid)` → `StopAll()`

### 칸 인덱스 규칙 (프로젝트 공통)
`cell = row * 3 + reel`, 0~8 행 우선. `Judge`·`JudgeResult`·[[UIInGameField]]·[[UIInGameBattle]]이 전부 이 좌표계다.

### 검증 상태 — Codex QA 통과 (2026-08-30)

## 2026-08-31-0 — 릴 강조를 판정 결과에 연결 (거짓 신호 제거)

### 증상
"종족마다 소환 규칙이 이해가 안 된다"(사용자, 2026-08-31).

### 원인 — 화면이 판정과 다른 규칙으로 반짝이고 있었다
`PlayWinEffect()`가 `SlotLineTable` 5라인을 훑어 **"라인 3칸이 전부 같은 심볼"**이면 흔들었다.
종족과 무관한 슬롯머신 규칙이라 **7종족 중 6종족에서 거짓 신호**였다.

| 종족 | 실제 판정 | 옛 연출 |
|---|---|---|
| 슬롯 | 5라인 3매치 **+ 2매치** | 3매치만 |
| 체스 | 8라인(세로 포함) 정렬 **+ 반정렬** | 세로 누락, 반정렬 없음 |
| 장기 | **A–B–A**(양끝만 같음) | **절대 안 반짝임** |
| 포커 | 스트레이트·트리플·페어 | 트리플만 우연히 |
| 화투 | 세로 열의 2장 족보 | "같은 심볼" 개념 자체가 없음 |
| 마작 | 면자(연속 숫자 포함), 위치 무관 | 반짝여도 그게 이유가 아님 |
| 윷 | 가로줄 이동값 합계 | 완전 무관 |

**플레이어는 반짝인 걸 보고 규칙을 유추한다.** 틀린 신호는 없느니만 못하다.

### 수정
`PlayWinEffect()` → `PlayJudgeEffect()`. `JudgeResult`가 채운 칸을 그대로 쓴다.
- `ListHitCell` — 주판정. 강하게(각도 12 / 0.5초)
- `ListPartialCell` — 절반 성립. **0.45배로 약하게**(`PARTIAL_SHAKE_RATIO`)

`InGameScene`이 판정 직후 `SetJudgeResult()`로 결과를 넘겨두고, **정지 코루틴이** 릴 정착이 끝난 뒤 튼다 —
그 시점을 이미 아는 쪽에 맡기는 게 밖에서 타이밍을 다시 재현하는 것보다 정확하다.

`IsLineMatched()`는 삭제했다(판정과 무관한 로직이라 남길 이유가 없다).

### 검증 — Codex QA 통과 (2026-08-31, Play Mode 실측)
장기 A–B–A grid `[2,3,2, 4,5,6, 0,1,4]` 주입 → 132프레임 샘플링,
칸별 최대 회전각 `[4.377, 4.377, 4.377, 0, 0, 0, 0, 0, 0]`.
**판정 칸 3개만 회전, 나머지 6칸 정확히 0°.** 플래그가 아니라 `localEulerAngles.z` 실측.

# 씬 편집 브리프 — 추가 스핀 버튼 겹침 수정 (2026-09-10)

대상: `D:\Unity\HouseRules\HouseRulez` / 브랜치 `work/2026-09-10-run-end-flow`
Unity 에디터는 켜져 있고 unityMCP가 붙어 있다. 컴파일 에러 0건.

## 배경

GDD 03장의 **추가 스핀 구매**(골드 25 → 코인 +1, 연차당 2회)를 붙이면서
`Assets/Scenes/InGameScene.unity`의 ACTION 영역에 버튼을 하나 추가했다.
코드 쪽(`RunData`·`InGameScene`·`UIInGameAction`·CSV)은 **이미 끝났고 배선도 됐다.**
남은 건 **버튼 위치가 형제와 겹치는 것 하나**다.

## 문제 — 좌표가 겹친다

`Action`(폭 1044) 아래 자식들의 x 구간을 실측한 결과:

```
Panel              [ -24 ~   24]
BattleStartButton  [  24 ~  312]
SwapPipRoot        [ 336 ~  408]
SwapText           [ 424 ~  584]
ExtraSpinButton    [ 476 ~  740]   ← 476~584 가 SwapText와 겹침
BattleSpeedButton  [ 756 ~ 1020]
```

`ExtraSpinButton`은 `BattleSpeedButton`을 복제해 만든 것이라 폭이 **264**인데,
비어 있는 구간은 **584~756 = 172px**뿐이라 그대로는 안 들어간다.

## 해야 할 일

`ExtraSpinButton`이 **어느 형제와도 겹치지 않게** 만든다. 방법은 맡긴다 — 예를 들어

- 폭을 172 미만으로 줄이고 그 구간에 넣거나(라벨이 잘리지 않는지 확인할 것),
- ACTION 영역의 다른 요소를 밀어 자리를 만들거나,
- 세로로 여유가 있으면 2단으로 배치하거나.

**제약**
- `Action`의 폭 1044을 넘지 않는다(GDD 10장 ScreenZones의 ACTION 영역 = 네이티브 348 × 3).
- 다른 형제의 위치·크기를 바꿨다면 그 사실을 보고에 명시할 것.
- 버튼 라벨은 코드가 매 `Refresh()`마다 채운다(`ActionExtraSpin` 키, 예: `추가 스핀 25G (2회)`).
  **로컬라이제이션 키를 붙이지 말 것** — 정적 라벨이 아니다.
- `UIInGameAction`의 직렬화 배선(`m_ExtraSpinButton`, `m_ExtraSpinText`)은 이미 연결돼 있다.
  **끊지 말 것.** 오브젝트를 새로 만들었다면 다시 연결하고, 연결됐는지 확인해 보고할 것.

## 지켜야 할 것

- **`.cs` 파일과 CSV는 건드리지 마라.** 이 작업의 소유 범위는 `Assets/Scenes/InGameScene.unity` 하나다.
  (코드는 Claude Code가 소유 중이다 — 동시에 만지면 충돌한다)
- Play Mode에 들어갈 필요 없다. 씬 편집과 좌표 검증만 하면 된다.
- 계측·임시 산출물은 `Temp/` 아래에만.

## 보고

- 최종 x 구간 표(위와 같은 형식)로 **겹침이 없음을 수치로** 보일 것
- 바꾼 오브젝트와 그 전/후 값
- `m_ExtraSpinButton` · `m_ExtraSpinText` 배선이 유효한지

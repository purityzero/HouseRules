# UIRunResult

연관: [[InGameScene]], [[RunData]], `UIPopup`(Glory), [[StringTable]], `PlayerManager`, [[UIRunResult]](프리팹은 `.claude/prefab/UIRunResult.md`)

런 종료 결과 팝업. 파일: `Assets/Scripts/InGame/UI/UIRunResult.cs`

## 2026-09-10-0 — 신설

### 개요
런이 닫힐 때 **도달 연차와 획득 옥새**를 보여준다. 기획 스펙 `.claude/design/run-end-flow.html` Q1-A 채택.

**왜 배너 한 줄이 아니라 팝업인가** — 옥새를 화면에 안 보여주면 플레이어가 **영구 성장을 배울 통로가 없다.**
런이 끝나는 순간이 메타 진행을 알리는 유일한 지점이라 그 자리를 배너로 흘려보낼 수 없다.

### 파일
- `Assets/Scripts/InGame/UI/UIRunResult.cs` (신규)
- `Assets/Resources/Prefabs/UI/UIRunResult.prefab` (신규 — `.claude/prefab/UIRunResult.md`)
- `Assets/Resources/Table/UITable.csv` — `UIRunResult,Popup,Prefabs/UI/UIRunResult` 행 추가
- `Assets/Resources/Table/StringTable.csv` — 아래 키 6개

### eRunEndReason
런이 닫힌 사유. 제목 문구가 이걸로 갈린다. 이 파일 상단에 함께 선언했다 —
`eBattleResult`가 `UIInGameBattle.cs`에 같이 있는 관례를 따랐다.

| 값 | 조건 | 제목 키 |
|---|---|---|
| `HomeFallen` | 본거지 HP 0 | `RunEndHomeFallen` |
| `Cleared` | 최종 연차 최종 웨이브 승리 | `RunEndCleared` |
| `OutOfSpinCoin` | 코인 0 **이면서 추가 스핀도 못 삼** | `RunEndOutOfSpinCoin` |

### 공개 API
```csharp
public void Apply(eRunEndReason _reason, int _year, int _yearMax, int _royal)
```
**옥새 지급은 이 팝업이 하지 않는다.** `InGameScene.EndRun()`이 이미
`PlayerManager.AddRoyal()`로 끝냈고, 여기는 결과를 **읽어 그리기만** 한다.
지급과 표시를 한 곳에 두면 팝업이 안 떴을 때 지급도 안 되는 결합이 생긴다.

### ★ Show() 직후 Rect를 다시 잡는다
`UIManager` 루트가 크기 0이라 그 아래 생성되면 화면을 못 채운다.
[[UISetting]]이 같은 이유로 쓰는 처리를 그대로 따랐다 — 부모 Rect 크기에 기대지 않고
`pivot/anchor/anchoredPosition/sizeDelta(1920×1080)`를 명시한다.

### ★ 뒤로가기를 닫기로 두지 않았다
런이 끝난 화면이라 그냥 닫으면 **인게임에 아무것도 못 하는 상태로 남는다.**
`OnPressBackBtn()`을 오버라이드해 타이틀로 나가는 것만 허용한다.

### 연타 가드
`NextScene()`은 부를 때마다 같은 커맨드 묶음을 큐에 더 쌓기만 해서 연타에 취약하다
([[TitleScene]]에서 겪은 것과 같은 함정). `m_isClosing`으로 1회만 통과시킨다.

### 이벤트 구독
`m_TitleSceneButton.onClick`은 `RemoveListener` 후 `AddListener` 한다 —
팝업이 재사용되므로 `+=`만 하면 런을 반복할수록 리스너가 쌓인다.

### StringTable 키
| Key | Kr | En | Cn | Jp |
|---|---|---|---|---|
| `RunEndHomeFallen` | 본거지 함락 | Home Fallen | 本营陷落 | 本拠地陥落 |
| `RunEndCleared` | 완주! | Cleared! | 通关! | 完走! |
| `RunEndOutOfSpinCoin` | 스핀 코인 소진 | Out of Spin Coins | 旋转币耗尽 | スピンコイン切れ |
| `RunEndYear` | {0}/{1}연차 도달 | Reached Year {0}/{1} | 到达 {0}/{1} 年 | {0}/{1}年 到達 |
| `RunEndRoyal` | 옥새 +{0} | Royal Seal +{0} | 玉玺 +{0} | 玉璽 +{0} |
| `RunEndToTitle` | 타이틀로 | To Title | 返回标题 | タイトルへ |

중국어·일본어 글리프는 TTF cmap으로 **사전 실측**했다(Vonwaon·PixelMplus 전부 커버).
`柶`(U+67F6)가 어느 아틀라스에도 없어 `□`가 된 전례가 있어, 새 문구는 항상 먼저 잰다.

### ✅ 검증 (2026-09-10, Codex Play Mode)
- 팝업이 `1920×1080`으로 실제 표시됨
- 표시값 `Home Fallen` · `Reached Year 2/12` · `Royal Seal +2` — 키 문자열이 아닌 실제 값
- 팝업 뒤 SPIN/BATTLE 레이캐스트가 `DimmedBackground`에 막혀 클릭 후 코인 불변
- `타이틀로` 클릭으로 `TitleScene` 도달
- 옥새 60 → **62** 단일 지급(64가 아님) — `m_isRunEnded` 멱등 가드 정상

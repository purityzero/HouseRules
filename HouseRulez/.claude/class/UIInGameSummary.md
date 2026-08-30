# UIInGameSummary

연관: [[UIInGameField]], `UIButton`, `JudgeResult`

## 2026-08-30-0 — 신설 (접이식 판정 요약 패널)

### 개요
판정 요약(`진 2 / 전력 1.9 · 소환 2기`)을 **평소엔 감춰두고 화살표 버튼으로 꺼내 보는** 패널.
왼쪽 중앙의 ◀/▶ 버튼을 누르면 왼쪽(슬롯머신 뒤)에서 밀려 나온다.

파일: `Assets/Scripts/InGame/UI/UIInGameSummary.cs`

### 왜 항상 띄우지 않는가
요약 텍스트를 전장 위에 늘 띄워두면 성문·유닛과 겹쳐 읽기도 나쁘고 화면도 지저분하다.
필요할 때만 꺼내 보는 쪽이 맞다(사용자 요청, 2026-08-30).

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ Summary            RectTransform 480x104, anchoredPos (432, 470)   ← UIInGameSummary (GO 30520355)
   ├─ Panel           RectTransform 408x104, anchoredPos (-420, 0)     ★ 닫힘 위치 (rt 1197462422)
   │  └─ Body         TMP, stretch -32/-16                             (rt 1395362768)
   └─ Toggle          RectTransform 48x104                             ← UIButton (rt 629126043)
      └─ Arrow        TMP, stretch                                     (rt 2046289901)
```

### 닫힘/열림 좌표
| 필드 | 값 | 근거 |
|---|---|---|
| `m_ClosedX` | -420 | 슬롯머신 뒤로 완전히 숨는 좌표. 그리기 순서상 슬롯머신이 위에 있어 가려진다 |
| `m_OpenX` | 56 | 화살표 버튼 오른쪽에 붙어 나온다 |
| `m_SlideDuration` | 0.22 | 감속 이징만(`1-(1-t)²`). 과한 이징은 픽셀아트에서 미끄러져 보인다 |

★ **Panel 폭이 420이면 12px이 삐져나온다.** 슬롯머신의 실제 가시 폭이 408이라, 패널이 420이면
닫혀도 오른쪽 12px이 노출된다. 폭을 408로 맞추고 `m_ClosedX`를 -420으로 둬서 완전히 숨긴다.
(Codex QA에서 발견 → 수정 → 재검증: 닫힘 패널 x [12,420] ⊂ 슬롯머신 x [12,420] 통과)

### 버튼을 프리팹 영구 호출로 연결하지 않는 이유
`Awake()`에서 `RemoveListener` → `AddListener`로 붙인다.
씬 오브젝트를 복제해 만들면 **남의 UnityEvent 영구 호출을 그대로 물고 온다** — 이 세션에서만 세 번 겪었다
(강화 버튼이 닫기 버튼의 이벤트를 물고 와 팝업을 닫아버린 사고 등).
런타임 등록으로 통일하면 그 사고 자체가 안 생기고, `-=` 후 `+=`라 중복 구독도 막힌다.

### 비활성 상태에서의 SetOpen
`gameObject.activeInHierarchy == false`면 코루틴을 못 돌리므로 `ApplyImmediate`로 즉시 반영한다.
코루틴을 그냥 `StartCoroutine` 하면 조용히 무시되어 패널이 중간 좌표에 멈춘다.

### 공개 API
| 메서드 | 하는 일 |
|---|---|
| `SetText(string)` | 본문 문구 설정. `UIInGameField.ShowSummon`/`Clear`가 부른다 |
| `OnClickToggleButton()` | 열림/닫힘 반전 |
| `SetOpen(bool)` | 상태 지정. 같은 상태면 아무것도 안 한다 |

### 검증 상태 — Codex QA 통과 (2026-08-30)
`ToggleButton.onClick.Invoke()`(프로덕션 진입점)로 열고 닫으며 확인:
닫힘 패널 x [12,420] 완전 은폐 통과 / 열림 anchored x 56 통과 / 본문 `진 2` + `전력 1.9 · 소환 2기` 통과.

# UIInGameBanner

연관: [[InGameScene]], [[UIInGameHud]], `StringTable`, `UIManager`(쓰지 못한 대안)

## 2026-08-31-0 — 신설 (화면 중앙 알림 배너)

### 개요
화면 한가운데 한 줄을 크게 띄웠다 지운다. 지금은 **윷·모 무료 스핀 알림**에만 쓴다.
파일: `Assets/Scripts/InGame/UI/UIInGameBanner.cs`

### ★ 왜 `UIManager.ShowToast()`를 안 썼나
Glory에 `UIToastMessage` + `UIManager.ShowToast()`가 이미 있어서 처음엔 그걸 썼다. **동작하지 않았다.**

- 로드 대상인 `Resources/Prefabs/UI/UIToastMessage` **프리팹이 프로젝트에 없다.**
  `MemoryPooling.Pop()`이 null을 돌려주고 `ShowToast`가 조용히 반환한다
- QA 실측: `ShowToast()` 호출 후 **활성 토스트 0개 / `UIToastMessage` 인스턴스 0개**
- 그 시스템이 기대는 `TweenEffectPlayer`도 **이 프로젝트의 어떤 씬·프리팹에서도 쓰인 적이 없다**

프리팹을 새로 만들려면 한 번도 돌려본 적 없는 트윈 체인을 설정해야 하고,
`TweenEffectPlayer`는 이펙트 배열이 비면 **에러만 찍고 완료 콜백을 안 부른다** —
그러면 토스트가 영원히 안 닫히고 풀이 샌다. 알림 하나 때문에 그 위험을 지지 않았다.

> 교훈: **클래스가 있는 것과 에셋이 있는 것은 다르다.**
> 재사용 후보를 찾을 때 코드만 보고 "쓸 수 있다"고 단정하면 안 된다.
> (토스트 되살리기는 `UNFINISHED.md`에 별도 항목)

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ BonusBanner   RectTransform 900x140, anchoredPos (0, 140)   ← UIInGameBanner
   │             CanvasGroup(알파) + Image(배경판 #363B4A a=0.85)
   └─ Message    TMP, 부모에 stretch, fontSize 72, 중앙 정렬
```
**SafeRoot의 마지막 자식**이라 다른 UI 위에 그려진다.

배경판을 깐 이유: 밝은 배경(윷 민속마을 등)에서 흰 글씨만으로는 대비가 약하다.
색은 GDD의 최암부 한계 `#363B4A`를 지킨다 — 이보다 어두우면 "검정은 적군 전용" 규칙 위반이다.

### 연출
페이드인 0.12s(+ `OutBack` 스케일 0.7 → 1.0) → 0.9s 유지 → 페이드아웃 0.35s.
픽셀아트라 과한 이징은 미끄러져 보여 살짝만 준다.

### ★ 이전 시퀀스를 반드시 죽인다
`Show()`가 `KillSequence()` 후 알파 0 / 스케일 0.7에서 다시 시작한다.
안 죽이면 연속 호출 시 중간 알파·스케일에서 출발해 그 값에 남는다.
`OnDisable()`에서도 죽이고 알파를 0으로 되돌린다 — 트윈이 살아 있는 채 꺼지면 다음에 켤 때 중간값이 남는다.

### 씬 편집 방법 (MCP 없이)
이 세션은 Unity MCP가 안 붙어서(에디터가 세션 시작 시 꺼져 있었다)
`unity` CLI로 씬을 편집했다: `open_scene` → `create_gameobject` → `add_component` /
`attach_script` → `set_serialized_field` → `save_scene`.
YAML 수기 작성보다 안전하다 — fileID·GUID를 직접 만들지 않아도 되고 실패가 즉시 드러난다.

★ Git Bash에서 `--parent "/Canvas/SafeRoot"`를 넘기면 MSYS가 Windows 경로로 바꿔버린다
(`C:/Program Files/Git/Canvas/SafeRoot`). **PowerShell을 쓰거나 instanceId로 지정한다.**
★ 오브젝트 참조(`m_fontAsset` 등)는 JSON이 아니라 **에셋 경로 문자열**을 넘긴다.

### 검증 — Codex QA 통과 (2026-08-31, Play Mode)
- `alpha` 0.00 → 0.75 → 1.00 → 1.00 → 0.25 → 0.00 (0/0.06/0.12/1.02/1.195/1.37초)
- 실제 문자열 `무료 스핀!`, 한글 4글자 전부 `DungGeunMo Bitmap` 글리프. **`□` 없음**
- 화면 영역 (510,610)–(1410,750), 1920×1080 내부. SafeRoot 자식 7/8(마지막)
- 연속 호출: 두 번째 직후 알파 0 / 스케일 0.7로 초기화, 최종 알파 0 / 스케일 (1,1,1)

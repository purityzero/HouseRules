# UIInGameFieldSlot

연관: [[UIInGameField]], [[UIHouseSlotMachine]], `HouseSlotSymbolSprite`, `SummonSlot`

## 2026-08-30-0 — 신설 (전장 한 칸)

### 개요
전장 3×3의 한 칸. 소환된 유닛 하나를 보여준다. 표시만 하고 전투 로직은 없다
(전투는 [[BattleUnit]]이 맡는다 — 소환 표시와 전투 유닛은 별개의 오브젝트다).

파일: `Assets/Scripts/InGame/UI/UIInGameFieldSlot.cs`

### 계층 구조
```
SlotTemplate        RectTransform 100x100 (코드가 96x96으로 덮어씀)   ← UIInGameFieldSlot (GO 421209072)
├─ Symbol           Image, stretch                                    (rt 1059830052)
└─ Grade            TMP 60x28, 우하단 (-2, 2)                          (rt 1026478292)
```

### 아군 아트를 따로 만들지 않는다
릴 심볼 스프라이트(`HouseSlotSymbolSprite.NormalSprite`)를 그대로 쓴다.
"릴에 나온 말이 그대로 전장에 선다"는 GDD 컨셉과 맞고 아트 비용도 들지 않는다.

### 1성은 등급 표시를 하지 않는다
`GRADE_HIDE_BELOW = 2`. 소환의 대부분이 1성이라(체스 95%) 전부 표시하면 화면이 ★1로 덮인다.
2성 이상만 `★{등급}`을 켠다.

### 공개 API
| 메서드 | 하는 일 |
|---|---|
| `SetUnit(Sprite, int grade)` | 심볼 대입 + 스프라이트 null이면 `Image.enabled = false`, 등급 표시 토글 |
| `Clear()` | 심볼/등급 모두 끔 |

`sprite`만 null로 두고 `enabled`를 안 끄면 흰 사각형이 남는다 — 두 개를 항상 세트로 다룬다.

### 검증 상태 — Codex QA 통과 (2026-08-30)

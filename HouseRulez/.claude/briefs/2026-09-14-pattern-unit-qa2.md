# 판정 고유 유닛 — 재검증 (SpawnAllies 수정 후)

브랜치 `work/2026-09-14-pattern-unit`. 앞선 QA 보고는 `D:/Orca/reports/pattern-unit-qa-2026-09-14.md` 입니다.
**지적해 주신 결함을 고쳤습니다. 그 한 가지만 확인해 주시면 됩니다.**

## 무엇을 고쳤나

`UIInGameBattle.SpawnAllies` 의 인덱스 검사가 `GetBattleStat` **앞**에 있어서,
스탯 조회를 고쳤어도 그 전에 고유 유닛이 전부 걸러졌습니다. 짚어주신 그대로입니다.

원인은 **같은 판단을 두 곳이 각자 들고 있던 것**이었습니다 —
`UIInGameField` 에는 고유 유닛 분기를 넣고 `UIInGameBattle` 에는 빠뜨렸습니다.
그래서 이번에는 사본을 지우고 공통 함수로 뺐습니다.

```
HouseSpriteLoader.FindUnitSprite(RunUnit, houseKey, symbolPool)
  ← UIInGameField.RefreshSlots 와 UIInGameBattle.SpawnAllies 가 **둘 다** 이것을 부른다
```

고유 유닛과 심볼 유닛을 가르는 자리가 이제 프로젝트에 하나뿐입니다.
패턴 스프라이트 캐시도 그쪽으로 옮겨 전장과 전투가 같은 파일을 두 번 읽지 않습니다.

## 제가 확인한 것

같은 경로를 재현해 **아군 1기 생성(`Hp 400 / Atk 80`, 스프라이트 `poker_pattern_triple_0`)**,
심볼+고유 섞인 로스터에서 **2/2** 둘 다 생성되는 것까지 봤습니다. `verify-compile` 통과.

**다만 실제로 싸우는 것은 못 봤습니다.** 생성 조건까지만 확인했습니다.

## 봐주셨으면 하는 것

1. **포커 전투가 정상으로 도는가** — 고유 유닛이 서고, 움직이고, 공격하고, 승패가 나는지.
   지난번 `SpawnAllies` Error 가 사라졌는지도 함께 봐주세요
2. **심볼 유닛과 섞였을 때** — 페어로 나온 심볼 유닛과 고유 유닛이 한 전장에 함께 서는 경우
3. **다른 종족 전투가 안 망가졌는가** — 공통 함수로 바꾸면서 심볼 유닛 경로도 함께 지났습니다.
   체스나 화투로 전투 한 번만 돌려보시면 됩니다

Hp 400 짜리가 웨이브를 쉽게 이기는 것은 의도된 것입니다(전력 보존).
밸런스는 별도로 다시 잴 예정이라 이번에는 판단하지 않으셔도 됩니다.

## 제약

소스와 CSV 는 수정하지 말아 주세요 — 고칠 곳을 찾으시면 위치와 근거만 주시면 제가 고치겠습니다.
산출물은 `Temp/` 아래에만, 폰트 아틀라스는 건드리지 마시고,
선택 종족은 끝나고 `poker` 로, QA 키는 남기지 말아 주세요.
`unity command` 에는 `--project-path "D:/Unity/HouseRules/HouseRulez"` 를 붙이셔야 합니다.

## 보고

`D:/Orca/reports/pattern-unit-qa2-2026-09-14.md` — 위 셋의 결과와 Console 상태만.

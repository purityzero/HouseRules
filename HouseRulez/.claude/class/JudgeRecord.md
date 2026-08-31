# JudgeRecord / JudgeTable

연관: [[Judge]], [[JudgeResult]], `HouseRecord`

## 2026-08-30-0 — 신설 문서 (구현은 커밋 8efc247)

### 개요
종족별 판정 계수 9행. 파일: `Assets/Scripts/Table/JudgeRecord.cs` / `Assets/Resources/Table/JudgeTable.csv`

| HouseKey | PatternKey | Coef |
|---|---|---|
| chess | ChessLineTriple | 8.0 |
| chess | ChessLinePair | 1.3 |
| janggi | JanggiJump | 3.8 |
| janggi | JanggiCannon | 3.8 |
| janggi | JanggiEdge | 0.95 |
| poker | PokerTriple | 40 |
| poker | PokerStraight | 11 |
| poker | PokerPair | 1 |
| hwatu | HwatuScale | 0.2055 |

### 계수만 테이블에 있고 족보는 코드에 있다
섯다 족보 값(삼광 300, 땡 90 …)은 **섯다의 규칙 그 자체**라 튜닝 대상이 아니다.
화투에서 조정하는 건 `HwatuScale` 하나뿐이고, 그 하나로 종족 전체 전력이 스케일된다.
→ CODE.MD 「튜닝값은 테이블로」의 예외가 아니라 적용 결과다: **튜닝 대상만 테이블에 있다.**

### 아직 없는 것
슬롯·마작·윷 3종족의 행. GDD에 계수가 없어 비워뒀다 — 각 종족 기획 문서를 확정한 뒤 채운다.

## 2026-08-30-1 — 슬롯 12행 · 마작 5행 추가 (9행 → 26행)

| HouseKey | PatternKey | Coef | 비고 |
|---|---|---|---|
| slot | `SlotMatch3_0`~`_5` | 3 / 5 / 8 / 13 / 20 / 33 | 등비 약 1.6 사다리 |
| slot | `SlotMatch2_0`~`_5` | 0.5 / 0.8 / 1.2 / 2 / 3 / 5 | 3매치의 약 15%. **CV를 정하는 유일한 레버** |
| mahjong | `MahjongMeld` | 2.4 | 면자 1개당 |
| mahjong | `MahjongTenpai` | 3.6 | 화료 못 했을 때만 |
| mahjong | `MahjongWin` | 24 | 3면자 성립 |
| mahjong | `MahjongKotsu` | 8 | 화료했을 때 커쯔 1개당 |
| mahjong | `MahjongIkkitsukan` | 56 | 화료 + 1~9 각 1장 |

### 슬롯만 패턴 키가 인덱스로 갈리는 이유
다른 종족은 "무슨 패턴이 성립했나"가 전력을 정하지만 슬롯은 **"무엇이 맞았나"**가 정한다.
그래서 상수도 고정 문자열이 아니라 접두사(`SLOT_MATCH3_PREFIX`)에 심볼 인덱스를 붙여 만든다.

### 윷 행은 없다
기획 미확정이라 비워뒀다. 사유와 막힌 항목은 [[Judge]] 2026-08-30-1 절 참고.

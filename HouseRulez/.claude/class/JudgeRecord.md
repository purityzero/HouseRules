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

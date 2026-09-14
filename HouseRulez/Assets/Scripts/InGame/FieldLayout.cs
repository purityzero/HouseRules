using UnityEngine;

// 전장 좌표 규칙의 **유일한 소유자**.
//
// 이전에는 같은 상수(열 간격 108 · 레인 높이 52 · 레인 x밀기 30)를 UIInGameField 와
// UIInGameBattle 이 각자 갖고 있었다. 화면 배치와 전투 생성이 같은 자리를 가리켜야 하는데
// 소유자가 둘이면 한쪽만 고쳤을 때 조용히 어긋난다(CLAUDE.md 「소유자는 하나다」).
//
// 2026-09-14 자유 배치를 넣으면서 좌표가 **저장 대상**이 됐으므로 여기로 모았다.
public static class FieldLayout
{
    // 전장은 3x3 격자에서 출발한다. 자유 배치가 붙은 뒤에도 **자동 배치가 쓰는 기본 자리**로 남는다.
    public const int COLUMN_COUNT = 3;

    // 열 간격. 사거리·이동속도가 칸 단위라 이 값이 곧 1칸이다(BattleUnit.CELL_TO_PIXEL 과 같은 눈금).
    public const float COLUMN_SPACING = 108f;

    // 뒤 레인일수록 위로 올리고 오른쪽으로 민다 — 바닥면이 비스듬히 깔린 것처럼 보이게.
    public const float LANE_STEP_Y = 52f;
    public const float LANE_STEP_X = 30f;

    // 자유 배치가 허용되는 영역. 기본 격자가 차지하던 범위 그대로다.
    // **넓히지 않는 이유** — 영역을 늘리면 적과의 거리가 바뀌어 전투 밸런스가 함께 움직인다.
    // 2026-09-14에 후반 곡선을 이 거리 위에서 잡았다.
    public const float AREA_MIN_X = 0f;
    public const float AREA_MAX_X = 276f;
    public const float AREA_MIN_Y = 0f;
    public const float AREA_MAX_Y = 104f;

    // 두 유닛이 이보다 가까이 붙지 못한다.
    //
    // **왜 필요한가** — 겹쳐 쌓으면 FindTarget 이 전부 같은 거리로 보고, 적이 한 기씩 때리는 동안
    // 아군 9기가 동시에 때린다. 즉 "한 점에 모으기"가 지배 전략이 되어 배치 선택이 사라진다.
    // 격자 열 간격(108)의 절반이라, 기본 격자보다 촘촘히 놓을 수는 있되 포개지지는 않는다.
    public const float MIN_DISTANCE = 54f;

    // 칸 번호의 기본 좌표. 자동 배치가 이 자리에 세운다.
    public static Vector2 GetDefaultPosition(int _cell)
    {
        int lane = _cell / COLUMN_COUNT;
        int column = _cell % COLUMN_COUNT;

        // row 0 이 가장 뒤 레인이라 화면에서 가장 위로 간다.
        int laneFromFront = (COLUMN_COUNT - 1) - lane;

        return new Vector2(
            column * COLUMN_SPACING + laneFromFront * LANE_STEP_X,
            laneFromFront * LANE_STEP_Y);
    }

    // 영역 밖으로 나가지 못하게 자른다.
    public static Vector2 Clamp(Vector2 _position)
    {
        return new Vector2(
            Mathf.Clamp(_position.x, AREA_MIN_X, AREA_MAX_X),
            Mathf.Clamp(_position.y, AREA_MIN_Y, AREA_MAX_Y));
    }
}

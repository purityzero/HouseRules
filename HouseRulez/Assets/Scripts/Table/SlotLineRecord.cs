using System.Collections.Generic;

// 슬롯 페이라인 한 줄. 릴 인덱스별로 "몇 번째 보이는 행을 쓰는지"만 들고 있다.
// 가로 3줄 + 대각 2줄 같은 구성이 코드가 아니라 CSV에서 정해지도록 테이블로 뺐다 —
// 라인을 늘리거나 빼는 건 밸런싱이라 코드 수정 없이 조정할 수 있어야 한다.
public class SlotLineRecord : Record
{
    public string Key;
    public int Reel0Row;
    public int Reel1Row;
    public int Reel2Row;

    // 릴 수는 화면 구조상 3으로 고정이다. 늘어나면 컬럼과 이 분기를 함께 늘린다.
    public int GetRow(int _reelIndex)
    {
        switch (_reelIndex)
        {
            case 0:
                return Reel0Row;
            case 1:
                return Reel1Row;
            case 2:
                return Reel2Row;
        }

        Logger.Error($"[SlotLineRecord] GetRow Failed! reelIndex out of range - {_reelIndex}");
        return -1;
    }
}

public class SlotLineTable : Table<SlotLineRecord>
{
    public SlotLineTable(List<SlotLineRecord> _listRecord) : base(_listRecord) { }
}

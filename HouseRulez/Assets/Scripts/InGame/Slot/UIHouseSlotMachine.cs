using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 3x3 슬롯머신 컨트롤러. 릴 3개를 묶고, 현재 종족의 말 스프라이트 풀/프레임을 세팅하고, 스핀을 지휘한다.
// 판정(결과 확정)은 아직 규칙이 없어 이 클래스가 하지 않는다 — 결과가 안 오면 무작위로 굴러간다.
public class UIHouseSlotMachine : MonoBehaviour
{
    [SerializeField] private Image m_FrameImage;
    [SerializeField] private Image m_HeaderBarImage; // 종족 AccentColor를 물릴 헤더 바(없으면 비워둔다)
    [SerializeField] private UIHouseSlotReel[] m_ReelList;
    [SerializeField] private UIHouseSlotSymbol m_SymbolTemplate; // 비활성 원본 1개, 릴마다 복제된다

    // 칸 크기 32px 기준, 보이는 3칸 + 위아래 버퍼 2칸씩 = 7칸.
    // Glory 릴은 m_SymbolList 전체를 순환 스크롤 버퍼로 쓰고 보이는 창을 가운데(GetVisibleStartIndex)로 잡으므로,
    // 버퍼가 위아래로 대칭이면서 3칸보다 넉넉해야 스크롤 중 빈 칸이 안 보인다.
    [SerializeField] private int m_SymbolCountPerReel = 7;

    [SerializeField] private float m_ReelStopInterval = 0.2f;

    private List<HouseSlotSymbolSprite> m_SpritePool = new List<HouseSlotSymbolSprite>();
    private HouseRecord m_Record;
    private Coroutine m_StopRoutine;
    private bool m_HasExternalResult;

    private void Awake()
    {
        if (m_SymbolTemplate != null)
            m_SymbolTemplate.gameObject.SetActive(false);

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            m_ReelList[index].BuildSymbols(m_SymbolTemplate, m_SymbolCountPerReel);
            m_ReelList[index].Init(index);
        }
    }

    // 종족이 바뀔 때(또는 최초 진입 시) 호출 — 스프라이트 풀/프레임/색을 다시 세팅한다.
    public void Apply(HouseRecord _record)
    {
        if (_record == null)
        {
            Logger.Error("[UIHouseSlotMachine] Apply Failed! record == null");
            return;
        }

        m_Record = _record;

        BuildSpritePool(_record);
        ApplyFrame(_record);

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            m_ReelList[index].ApplySpritePool(m_SpritePool);
            m_ReelList[index].Open();
        }
    }

    private void BuildSpritePool(HouseRecord _record)
    {
        m_SpritePool.Clear();

        // 풀 로딩은 여기서 한 번만 한다 — 심볼 칸마다 조회하면 칸 수만큼 중복 조회가 된다.
        List<Sprite> normalSprites = HouseSpriteLoader.Load(_record);
        for (int index = 0; index < normalSprites.Count; ++index)
        {
            Sprite normalSprite = normalSprites[index];

            // 블러 스프라이트가 없는 경우가 있을 수 있어 ResUtil.Load(에러 로그 발생) 대신
            // Resources.Load로 조용히 조회한다 — 없으면 null로 두고 UIHouseSlotSymbol이 원본으로 대체한다.
            Sprite blurSprite = Resources.Load<Sprite>($"Image/InGame/Actor/{_record.SpriteFolder}/{normalSprite.name}_blur");

            HouseSlotSymbolSprite spriteSet = new HouseSlotSymbolSprite();
            spriteSet.NormalSprite = normalSprite;
            spriteSet.BlurSprite = blurSprite;
            m_SpritePool.Add(spriteSet);
        }
    }

    private void ApplyFrame(HouseRecord _record)
    {
        if (m_FrameImage != null)
        {
            Sprite frameSprite = ResUtil.Load<Sprite>($"Image/InGame/Reel/frame_{_record.Key}");
            if (frameSprite != null)
                m_FrameImage.sprite = frameSprite;
        }

        if (m_HeaderBarImage != null)
        {
            Color accentColor;
            if (ColorUtility.TryParseHtmlString($"#{_record.AccentColor}", out accentColor) == true)
                m_HeaderBarImage.color = accentColor;
            else
                Logger.Error($"[UIHouseSlotMachine] AccentColor 파싱 실패 - {_record.AccentColor}");
        }
    }

    // 릴 3개를 동시에 돌리기 시작한다.
    public void Spin()
    {
        if (m_StopRoutine != null)
        {
            StopCoroutine(m_StopRoutine);
            m_StopRoutine = null;
        }

        m_HasExternalResult = false;

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            m_ReelList[index].fsm.SetState(eReelState.Spin);
        }
    }

    // 판정기가 결과를 확정하면 호출한다. 배열 순서 = 릴 인덱스, 각 원소는 그 릴의 보이는 칸 결과(스프라이트 풀 인덱스).
    // TODO: 전력 -> 소환 수 환산 규칙이 확정되면 판정기가 이 메서드를 Spin()과 StopAll() 사이에서 호출해 연결한다.
    // 규칙이 아직 없으므로 지금은 이 메서드가 호출되지 않아도(무작위 결과로) 정상 동작한다.
    public void SetResult(int[][] _resultSymbolTypesByReel)
    {
        if (_resultSymbolTypesByReel == null)
        {
            Logger.Error("[UIHouseSlotMachine] SetResult Failed! resultSymbolTypesByReel == null");
            return;
        }

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            if (index >= _resultSymbolTypesByReel.Length)
                continue;

            m_ReelList[index].SetResult(_resultSymbolTypesByReel[index]);
        }

        m_HasExternalResult = true;
    }

    // 릴마다 시간차를 두고 순차 정지시킨다.
    public void StopAll()
    {
        if (m_HasExternalResult == false)
            ApplyRandomResult();

        m_StopRoutine = StartCoroutine(CoStopReelsSequentially());
    }

    // 판정기가 아직 없어 SetResult가 호출되지 않았을 때 무작위 결과로 채운다.
    private void ApplyRandomResult()
    {
        int poolCount = m_SpritePool.Count;
        if (poolCount <= 0)
            return;

        int[][] randomResult = new int[m_ReelList.Length][];
        for (int reelIndex = 0; reelIndex < m_ReelList.Length; ++reelIndex)
        {
            randomResult[reelIndex] = new int[3];
            for (int rowIndex = 0; rowIndex < randomResult[reelIndex].Length; ++rowIndex)
            {
                randomResult[reelIndex][rowIndex] = Random.Range(0, poolCount);
            }
        }

        SetResult(randomResult);
    }

    private IEnumerator CoStopReelsSequentially()
    {
        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            if (m_ReelList[index].IsState(eReelState.Spin) == false)
                continue;

            m_ReelList[index].fsm.SetState(eReelState.Stop);
            yield return new WaitForSeconds(m_ReelStopInterval);
        }

        m_StopRoutine = null;
    }
}

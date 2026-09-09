# 이어서 할 일

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

## 이어서 할 일

해소된 항목은 `.claude/archive/UNFINISHED-2026-08.md`로 옮겼다. 아래는 **아직 안 끝난 것만**이다.

### 게임 시스템
- **골드를 쓰는 쪽이 없다** — 배당은 있다(`RunData.AwardGoldByPower`,
  `GameConfigTable.GoldPerPower = 2`). 추가 스핀 구매 같은 **소모 경로가 없어 골드가 쌓이기만 한다**

### 아트·연출
- **종족 강조색(`AccentColor`)이 인게임에 안 물린다** — `UIHouseSlotMachine.m_HeaderBarImage`가
  미연결(fileID 0)이고, 물릴 헤더 바 오브젝트 자체가 씬에 없다. 종족 구분이 배경 + 프레임으로만 난다
- **종족 테마 프레임 아트 보강** — 구조는 맞으나 종족 구분이 약하다. 타이틀 배경 5종만큼 문화권이 안 드러난다
- **윷 심볼이 형제 종족보다 3.4배 작다** — 위 `2026-08-31-0` 절 참고
- 슬롯 아트 20개 **사람 눈 확인만 남음** — 임포트·순서·크기는 MCP로 검증 완료.
  남은 것은 ①배경이 스크롤에서 이음매 없이 도는지 ②프레임 창에 릴 칸이 실제로 맞는지.
  관련: 위 20개 png + `.meta`, `.claude/design/slot-house.html` §9.6

### 저장소 정리
- **`Assets/Screenshots/`(32개)가 저장소에 그대로 있다** — QA 캡처는 이후 `QACapture/`로 옮겨
  무시 처리했는데, 이 폴더는 "지우거나 `.gitignore`에 넣자"는 판단이 **실행되지 않은 채 남아 있다**
- 프레임 `.meta`의 `textureCompression: 0`(전 플랫폼 무압축)이 선례(`ui_white.png.meta`)와 다르다 — 빌드 용량 기준이 서면 재검토

### ★ 오판 방지 — 배경은 Sprite가 아니라 Texture다
`bg_*` 배경 이미지는 **Sprite가 아니라 Texture로 임포트하는 게 이 프로젝트 관례다**
(`type=Default`, 기존 5장 전부 동일). 코드도 `ResUtil.Load<Texture>`로 받는다
(`Assets/Scripts/Title/UIHouseSelect.cs:271`, `Assets/Scripts/InGame/InGameScene.cs:156`).
`Resources.Load<Sprite>`로 조회하면 기존 배경도 전부 null이 나오니 **이걸 결함으로 오판하지 말 것.**

---


# 미완료 작업

## 2026-08-26-0 — PlayerData / PlayerManager + Glory 저장 프레임워크 (구현 완료, 검증 대기)

- **브랜치**: `work/2026-08-26-player-data`
- **상태**: 코드 작성 완료. **Unity MCP 미연결 세션이라 컴파일/Play Mode 미검증** — main에 합류시키기 전 사람이 직접 확인해야 한다.
- **검증할 것**:
  1. 재컴파일 에러 0건
  2. 종족 선택 → 패널 닫기 → 다시 열기 시 고른 종족이 유지되는가
  3. 앱 재시작(에디터 재생 정지 후 재생) 후에도 유지되는가
  4. 마작(잠금)을 눌러도 저장되지 않고 미리보기만 되는가
- **관련 파일**: `Assets/Scripts/Glory/Data/` 4개, `Assets/Scripts/Player/` 2개, `Assets/Scripts/Title/UIHouseSelect.cs`, `Assets/Scripts/Title/TitleScene.cs`
- **문서**: `.claude/class/SaveData.md`, `.claude/class/PlayerManager.md`, `.claude/class/UIHouseSelect.md`(2026-08-26-1)

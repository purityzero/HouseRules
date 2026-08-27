# TableManager

연관: [[UIRecord]], [[SoundRecord]], [[ToggleListRecord]], [[ToggleMenuRecord]]

Glory 공용 라이브러리(`Assets/Scripts/Glory/Table/TableManager.cs`)의 CSV 테이블 로더. `GameManager`(프로젝트에서 아직 미구현) 등 부팅 시점에서 `TableManager.instance.init()`을 호출해 사용.

## 2026-08-18-0 — GeometryDefender에서 Glory 라이브러리 이식, `init()` 프로젝트 종속 제거

### 개요
GeometryDefender 프로젝트의 `Assets/Scripts/Glory/` 전체를 HouseRulez로 이식(사용자 요청). 원본 `TableManager.init()`은 Glory 폴더 안에 있음에도 EnemyRecord/TowerRecord/WaveRecord/CardRecord/MetaTreeRecord/DifficultyRecord 등 GeometryDefender 게임 고유 데이터 테이블 15개를 하드코딩하고 있어(Glory 프로젝트 비의존 원칙 위반 — `.claude/rules/glory.md`에 문서화된 기존 예외 목록에도 없던 것), 그대로 복사하면 HouseRulez엔 해당 Record 클래스 자체가 없어 컴파일이 깨짐.

### 수정 (함수: `init()`)
**이전**: EnemyTable/TowerTable/ProjectileTable/WaveTable/WaveSpawnTable/GameConfigTable/TowerSlotTable/MetaTreeTable/DifficultyTable/UITable/ToggleListTable/ToggleMenuTable/StringTable/CardTable/SoundTable, 총 15개 테이블을 로드+등록.

**이후**: Glory 자신이 직접 참조하는 최소 인프라 테이블만 남김 — `UITable`(`UIManager.Get<T>()`), `SoundTable`(`BaseScene.PlaySfx()`), `ToggleListTable`/`ToggleMenuTable`(`ToggleButtonList`). 이 4개의 Record 클래스(`UIRecord`, `SoundRecord`, `ToggleListRecord`, `ToggleMenuRecord`)는 GeometryDefender의 `Assets/Scripts/Table/`에서 함께 가져와 HouseRulez의 `Assets/Scripts/Table/`에 배치(게임 데이터가 아닌 범용 CSV 스키마라 판단). 대응 CSV(`Assets/Resources/Table/{UITable,SoundTable,ToggleListTable,ToggleMenuTable}.csv`)는 헤더 행만 있는 빈 템플릿으로 새로 생성.

그 외 게임 고유 테이블(Enemy/Tower/Projectile/Wave/GameConfig/TowerSlot/MetaTree/Difficulty/String/Card)은 이식하지 않음 — HouseRulez가 실제 게임 데이터를 설계하면 그때 `init()`에 같은 패턴으로 추가.

### 검증 상태 — 미검증
Unity 에디터로 실제 컴파일 확인 안 됨(파일 시스템 조작만 수행). 다음 세션에서 에디터를 열어 컴파일 에러 0건인지, `UITable`/`SoundTable`/`ToggleListTable`/`ToggleMenuTable` 조회가 정상 동작(빈 테이블이라도 예외 없이)하는지 확인 필요.

## 2026-08-27-0 — `GameConfigTable` / `SutdaBetTable` 등록

### 개요
인게임 HUD의 런 초기값과 화투 판돈 배수를 담을 테이블 두 개를 `init()`에 추가했다.
위 2026-08-18-0 항목이 "GameConfig는 이식하지 않음 — 실제 게임 데이터를 설계하면 그때 같은 패턴으로 추가"라고
적어둔 그 시점이 왔다. **다만 이식이 아니라 HouseRulez 스키마로 새로 만든 것**이다
(GeometryDefender의 GameConfigTable과 컬럼이 다르다).

### 수정 (함수: `init()`)
기존 7개 테이블 뒤에 같은 3줄 패턴으로 이어 붙였다.
```csharp
List<GameConfigRecord> gameConfigRecords = LoadCsvTable<GameConfigRecord>("Table/GameConfigTable");
List<SutdaBetRecord> sutdaBetRecords = LoadCsvTable<SutdaBetRecord>("Table/SutdaBetTable");
...
GameConfigTable gameConfigTable = new GameConfigTable(gameConfigRecords);
SutdaBetTable sutdaBetTable = new SutdaBetTable(sutdaBetRecords);
...
m_TableDictionary.Add(typeof(GameConfigTable), gameConfigTable);
m_TableDictionary.Add(typeof(SutdaBetTable), sutdaBetTable);
```

상세는 [[GameConfigTable]], [[SutdaBetTable]] 참고.

### 검증 상태 — 미검증
MCP 미연결 세션이라 컴파일/로드 확인을 못 했다. CSV 헤더와 Record 필드명 일치는 눈으로 대조했다.

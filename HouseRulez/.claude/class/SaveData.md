# SaveData / ISaveStorage / SaveDataProxy / SaveDataRegistry — Glory 저장 프레임워크

연관: [[PlayerManager]], [[MonoSingleton]], [[Logger]]

`Assets/Scripts/Glory/Data/` — 프로젝트 비의존 공용 저장 계층. 프로젝트는 `SaveData`를 상속한 데이터 클래스와 그걸 소유하는 매니저만 만들면 된다.

## 2026-08-26-0 — 신규 작성 (proxy 패턴 승격)

### 개요
사용자 요청: "저장 시스템 — 지오메트리 커맨더, `D:\BackUp\NewSlot`(proxy 패턴) 근거로 만들고, proxy가 괜찮으면 Glory 공용 프레임워크로 승격."

두 원본을 대조해 취한 것과 뺀 것:

| 출처 | 가져온 것 | 뺀 것 |
|---|---|---|
| `D:\BackUp\NewSlot` (`Server/Common/GameData.cs`, `GameDataControl.cs`, `ServerLogic.cs`) | ① 데이터 베이스의 **더티 플래그**(`isNeedSave`) ② 데이터 1덩어리를 대신 책임지는 **프록시**(`GameDataControl<T>`) ③ 타입으로 찾는 **레지스트리**(`Dictionary<Type, IGameDataControl>`) ④ 로컬/서버 구현 **교체 지점**(`ServerLogic_Local` vs `ServerLogic_Http`) | 패킷 계층 전체(`SendPacket`, `ePACKET_TYPE`), `ServerMgr`/`Manager` 결합, `Logout()`, 파일 기반 `FileUtil` |
| `D:\Unity\GeometryDefender` (`Assets/Scripts/PlayerManager.cs`) | ① `MonoSingleton` + `PlayerPrefs` + `JsonUtility` 조합 ② 진행도/옵션/재화 **저장 키 분리** ③ 첫 실행 판정 후 기본 언어 세팅 ④ `OnApplicationPause`에서 저장 ⑤ 옵션을 실제 시스템에 반영하는 `ApplyFpsOption` 패턴 | 데이터 필드를 `public`으로 열어두고 밖에서 직접 대입하던 구조(→ NewSlot의 Set 메서드 방식으로 교체) |

### proxy 채택 — 근거
**채택했다.** 다만 NewSlot 원본이 프록시를 쓴 이유(서버/로컬 이중 구현)를 그대로 옮기지 않고, **교체 지점을 `ISaveStorage` 하나로 좁혔다.**

- 채택 이유: 저장 매체(PlayerPrefs → 파일 → 서버)는 이 프로젝트에서 바뀔 가능성이 실제로 있고, 그때 갈아야 할 곳이 `ISaveStorage` 구현 1개로 고정된다. 데이터 클래스와 호출부는 손대지 않는다.
- 얇게 만든 이유: NewSlot의 `ServerLogic` 계층은 패킷 송수신까지 겸해서 500줄이 넘는다. 저장만 놓고 보면 그 절반 이상이 이 프로젝트엔 없는 개념이라 그대로 옮기면 죽은 코드가 된다.
- **더티 플래그를 함께 가져온 게 핵심**이다. 프록시만 있고 더티 플래그가 없으면 호출부가 매번 `Save()`를 직접 불러야 하고, 그 호출을 빠뜨리면 에러 없이 조용히 저장만 안 된다(원본이 이 문제를 Set 메서드 + 더티 플래그로 막고 있었다).

### 구조
```
SaveData (abstract)            데이터 베이스. Version + 더티 플래그 + OnChanged
   └ 파생 클래스가 [SerializeField] private 필드 + Set 메서드만 노출
SaveDataProxy<T> : ISaveDataProxy   T 1개를 소유. Load/Save/UpdateLogic(더티면 저장)
SaveDataRegistry               Dictionary<Type, ISaveDataProxy>. Add/Get/LoadAll/SaveAll/UpdateLogic
ISaveStorage                   저장 매체. 구현: PlayerPrefsSaveStorage
```

호출 흐름: `매니저 → Registry.Get<T>() → 데이터의 Set 메서드 → SetChanged() → 다음 프레임 Registry.UpdateLogic()이 더티 감지 → Proxy.Save() → Storage.Save() → Storage.Flush() 1회`

### 구현 요점
- **필드를 절대 public으로 열지 않는다.** `[SerializeField] private` + 읽기 전용 프로퍼티 + `Set메서드()` 구성. 밖에서 필드에 직접 대입할 수 있으면 `SetChanged()`를 빠뜨리게 되고, 그러면 **에러 없이 저장만 안 되는** 버그가 된다. 이게 이 구조를 쓰는 실질적 이유다.
- **`JsonUtility.FromJson`이 아니라 `FromJsonOverwrite`를 쓴다.** `FromJson`은 새 인스턴스를 반환하므로 로드 순간 `OnChanged` 구독자가 전부 끊긴다(원본 NewSlot/GeometryDefender 양쪽 다 이 문제가 있다). Overwrite는 인스턴스 동일성을 유지해서, 매니저가 캐싱한 참조도 로드 후 그대로 유효하다.
- **`Flush()`를 매체에서 분리했다.** `PlayerPrefs.SetString`은 싸지만 `PlayerPrefs.Save`는 비싸다. `Registry.UpdateLogic()`이 한 프레임에 여러 데이터가 바뀌어도 `Flush()`는 **실제로 쓴 게 있을 때 1회만** 부른다(`ISaveDataProxy.UpdateLogic()`이 bool을 반환하는 이유).
- 저장본이 깨졌을 때 `FromJsonOverwrite`가 던지는 예외를 잡아 `Init()`으로 되돌린다 — 여기서 안 잡으면 앱이 아예 못 뜬다.
- `SaveData.Version`은 마이그레이션 분기용 자리만 잡아뒀다. **실제 마이그레이션 코드는 아직 없다** — 스키마가 처음 바뀌는 시점에 파생 클래스가 `Load` 후 분기를 넣는다.

### 프로젝트 비의존 확인
`.claude/rules/glory.md`의 원칙대로 이 4개 파일은 프로젝트 고유 클래스를 참조하지 않는다. 의존은 `UnityEngine`(JsonUtility/PlayerPrefs)과 같은 Glory 소속인 `Logger`뿐이다.

### 검증 상태 — **미검증**
Unity MCP 미연결 세션이라 컴파일/Play Mode 확인을 못 했다. 파일 신규 작성만 마친 상태.

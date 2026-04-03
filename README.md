# Memorium

`Memorium`은 스테이지 진행, 던전, 스킬/장비 성장, 가챠, 픽시, 빙고, 어빌리티 스톤 등 여러 성장 시스템을 포함한 Unity 기반 게임 프로젝트입니다.

데이터는 `Addressables + CSV 테이블` 중심으로 로드되며, 인벤토리와 저장은 매니저/모듈 단위로 나뉘어 관리됩니다.

## 프로젝트 개요

- 엔진 버전: Unity `6000.3.7f1`
- 렌더 파이프라인: `URP`
- 주요 씬: `TitleScene`, `StageScene`, `DungeonScene`
- 데이터 로드 방식: Addressables 라벨 기반 CSV 로드
- 저장 방식: JSON 저장 + 주기적 자동 저장
- 입력 시스템: `com.unity.inputsystem` 패키지 사용

## 주요 기능

### 1. 스테이지 진행 및 전투 UI

- `StageManager`를 중심으로 현재 층, 보스 스테이지, 실패 재도전 상태, 최대 도달 스테이지를 관리합니다.
- `StageUIController`는 스테이지 정보, 진행도, 보스 HP, 보스 소환 버튼 상태를 표시합니다.
- 일반 스테이지와 던전 씬에서 공통 UI 컨트롤러 패턴을 공유합니다.

### 2. 액티브 스킬 시스템

- `ActiveSkillUIController`가 스킬 목록, 프리셋 버튼, 장착 슬롯, 스킬 상세 패널을 관리합니다.
- `SkillInventoryModule`에서 스킬 해금, 레벨업, 프리셋 슬롯 장착을 처리합니다.
- 스킬 상세 정보창과 장착 패널이 연결되어 있어 상세 확인 후 바로 장착 흐름으로 이어집니다.

### 3. 장비 인벤토리 및 강화

- `EquipTabUIController`가 무기/방어구 타입별 장비 목록을 티어 단위로 구성합니다.
- `EquipCurrentUIController`, `EquipReinforceUIController`가 현재 장착 상태와 강화/합성 결과 팝업을 담당합니다.
- 장비는 티어, 등급 색상, 보유 수량, 잠금 여부를 UI에 반영합니다.

### 4. 픽시 시스템

- `PixieContentsUIController`가 픽시 목록, 상세 정보, 성장 버튼, 소환 상태를 관리합니다.
- 픽시 타입별 색상 구분, 버프/디버프 슬롯 표시, 3D 미리보기 렌더링이 포함되어 있습니다.
- `PixieInventoryModule`과 연동하여 해금 및 성장 상태를 반영합니다.

### 5. 어빌리티 스톤 시스템

- `StoneUI`가 스톤 목록, 보너스 옵션, 등급/티어 상태, 강화 패널을 통합 관리합니다.
- 스톤 보너스 정보와 강화 확인 팝업은 공통 팝업 스택 구조를 사용합니다.
- `AbilityStoneManager`와 연동하여 단계별 성장과 재설정 흐름을 처리합니다.

### 6. 던전 시스템

- `DungeonUIController`가 던전 목록, 입장 가능 여부, 열쇠 보유량, 보상 미리보기를 표시합니다.
- 던전 입장 확인은 `DungeonLevelPopupUI`를 통해 팝업으로 처리됩니다.
- `RewardManager`와 연결되어 던전 보상 목록을 UI에 재구성합니다.

### 7. 가챠 시스템

- `CachaUIController`에서 무기, 방어구, 스킬 스크롤 가챠 탭을 전환합니다.
- 결과 팝업, 결정 부족 팝업, 티켓 수량 반영 UI가 연결되어 있습니다.
- `GachaConfig`는 기본 뽑기 수치와 천장 관련 값을 제공하고, 필요 시 `ConfigTable` 값으로 덮어쓸 수 있습니다.

### 8. 빙고 시스템

- `BingoUI`와 `BingoBoardManager`가 빙고 슬롯, 시너지 라인, 재시도 UI, 링크 아이템 카운터를 관리합니다.
- 인벤토리 수량 변경 이벤트와 연결되어 빙고 관련 아이템 수량을 즉시 갱신합니다.

### 9. 퀘스트 및 성장 보조 시스템

- `QuestManager`가 현재 퀘스트 진행도, 완료 여부, 보상 지급, 다음 퀘스트 이동을 담당합니다.
- `PlayerLevel`, `PlayerStat`, `Trait` 관련 UI 폴더가 각각 레벨 성장, 스탯 강화, 특성 트리를 담당합니다.
- `CharacterInfo`, `Misc`, `Option` 등 메뉴 UI가 공통 패턴으로 묶여 있습니다.

### 10. 공통 UI 패턴

- 대부분의 UI는 `UIControllerBase`를 상속해 `Initialize -> Subscribe -> RefreshView -> Unsubscribe` 흐름을 따릅니다.
- 팝업은 `PopupStackService`로 관리되며, 공용 배경과 바깥 영역 터치 닫기 동작을 공유합니다.
- 최근 작업된 상세 팝업류도 런타임 탐색보다 직렬화 참조를 우선하는 구조로 맞춰져 있습니다.

## 데이터 및 저장 구조

### 데이터 로드

- `DataManager`가 Addressables 라벨 `CSV_Data`로 모든 테이블을 로드합니다.
- 장비, 스킬, 문자열, 던전, 가챠, 어빌리티 스톤, 빙고, 사운드, 오프라인 보상 등 다수의 테이블 딕셔너리를 메모리에 보관합니다.
- 문자열은 `StringDict`를 통해 로컬라이징 텍스트를 조회합니다.
- CSV 로드가 끝난 뒤 각 시스템 매니저가 자신이 필요한 저장 데이터를 초기화합니다.

### 인벤토리 구조

- `InventoryManager`는 공통 진입점이며 내부에서 기능별 모듈을 등록합니다.
- 현재 등록되는 주요 모듈은 `CurrencyInventoryModule`, `SkillInventoryModule`, `GemInventoryModule`, `PassiveSkillModule`, `StackItemInventoryModule`, `EquipmentInventoryModule`, `PixieInventoryModule`입니다.
- 공통 이벤트 `OnItemAmountChanged`를 통해 UI가 수량 변경을 구독합니다.

### 저장 구조

- JSON 저장은 `JSONService`를 통해 이루어집니다.
- 기능별 저장 객체는 `SaveCurrencyData`, `SaveSkillData`, `SaveEquipmentData`, `SaveQuestData`, `SaveStageData`, `SavePixieData`, `SaveGemData`, `SaveBingoData` 등으로 분리되어 있습니다.
- `AutoDataSaveManager`가 기본 `300`초 간격으로 자동 저장을 수행합니다.
- 앱 일시정지 또는 종료 시점에도 Dirty 상태의 데이터를 저장합니다.

## 씬 구성

현재 Build Settings에 등록된 씬은 아래 3개입니다.

| Scene | 역할 |
| --- | --- |
| `Assets/01. Scenes/TitleScene.unity` | 시작 씬, 초기 진입 및 데이터 준비 |
| `Assets/01. Scenes/StageScene.unity` | 메인 성장/전투/UI가 모이는 핵심 씬 |
| `Assets/01. Scenes/DungeonScene.unity` | 던전 전용 씬 |

## 중요 설정값

아래 값들은 프로젝트 파악이나 빌드/운영 시 먼저 확인할 만한 항목들입니다.

| 항목 | 값 | 설명 |
| --- | --- | --- |
| Unity Editor | `6000.3.7f1` | 현재 프로젝트 기준 엔진 버전 |
| Company / Product | `Bravo6` / `Memorium` | Player Settings 기본 정보 |
| Bundle Version | `0.1.20260402` | 앱 버전 문자열 |
| Android Version Code | `9` | Android 빌드 버전 코드 |
| Android Package | `com.Bravo6.Memorium` | Android 앱 식별자 |
| Min / Target SDK | `25` / `35` | Android 지원 범위 |
| Default Resolution | `1024 x 768` | 기본 화면 크기 |
| Color Space | `Linear` | 렌더링 색 공간 |
| Orientation | Portrait only | 세로 모드 중심 설정 |
| Scripting Define | `UNITY_PIPELINE_URP` | Android / Standalone 공통 정의 |
| Data Label | `CSV_Data` | `DataManager`가 읽는 CSV Addressables 라벨 |
| Prefab Preload Label | `Prefabs` | 선로딩 대상 프리팹 라벨 |
| CSV Load Retry Count | `3` | 데이터 로드 재시도 횟수 |
| Retry Delay | `2s` | CSV 로드 재시도 간격 |
| Catalog Timeout | `15s` | Addressables 카탈로그 확인 제한 시간 |
| Asset Timeout | `30s` | 일반 에셋 다운로드 제한 시간 |
| Prefab Timeout | `300s` | 프리팹 다운로드 제한 시간 |
| Auto Save Delay | `300s` | 자동 저장 주기 |
| Equipment Merge Count | `3` | 장비 합성/카운트 표시 기준값 |
| Skill Merge Count | `3` | 스킬 조각/합성 관련 기본 기준값 |
| Dungeon Required Key Count | `1` | 던전 입장 최소 열쇠 수량 |
| Pixie Unlock Cost | `50` | 픽시 해금 관련 기본 비용 |
| Gacha Draws Per Level | 기본값 `100` | `ConfigTable`에서 override 가능 |
| Gacha Pity Draw Count | 기본값 `40` | `ConfigTable`에서 override 가능 |

## 사용 중인 주요 패키지 / SDK

`Packages/manifest.json` 기준 주요 패키지는 아래와 같습니다.

- `com.unity.addressables` `2.8.1`
- `com.unity.ai.navigation` `2.0.9`
- `com.unity.inputsystem` `1.18.0`
- `com.unity.render-pipelines.universal` `17.3.0`
- `com.unity.timeline` `1.8.10`
- `com.unity.toonshader` `0.13.4-preview`
- `com.unity.ugui` `2.0.0`
- `com.unity.visualeffectgraph` `17.3.0`

레포 내에는 패키지 외에도 아래 SDK/플러그인 폴더가 존재합니다.

- `Assets/Firebase`
- `Assets/GoogleSignIn`
- `Assets/ExternalDependencyManager`
- `Assets/Plugins`

## 폴더 구조

### 최상위 폴더

| 경로 | 설명 |
| --- | --- |
| `Assets/` | 게임 에셋, 스크립트, 씬, 프리팹 |
| `Docs/` | 발표 자료 등 문서 |
| `Packages/` | Unity 패키지 의존성 |
| `ProjectSettings/` | Unity 프로젝트 설정 |
| `ServerData/` | Addressables 빌드 결과물(플랫폼별) |
| `BuildBackups/` | 빌드 백업 자산 |
| `README.md` | 프로젝트 소개 문서 |

### Assets 폴더

| 경로 | 설명 |
| --- | --- |
| `Assets/00. Scripts/` | 런타임 로직과 시스템 코드 |
| `Assets/01. Scenes/` | 실제 사용 중인 씬 |
| `Assets/02. Prefabs/` | UI 및 게임 오브젝트 프리팹 |
| `Assets/03. ExcludeGit/` | 버전관리 제외 자산 |
| `Assets/03.ExcludeGit/` | 버전관리 제외 자산(이전 구조 포함) |
| `Assets/04. CSV/` | 원본 테이블 데이터 |
| `Assets/AddressableAssetsData/` | Addressables 설정 |
| `Assets/Editor/` | 에디터 확장 코드 |
| `Assets/Resources/` | Resources 로드 자산 |
| `Assets/StreamingAssets/` | 런타임 직접 접근 자산 |
| `Assets/Firebase/` | Firebase SDK 자산 |
| `Assets/GoogleSignIn/` | Google Sign-In 관련 자산 |
| `Assets/ExternalDependencyManager/` | EDM4U 관련 자산 |
| `Assets/Plugins/` | 네이티브/외부 플러그인 |

### Scripts 폴더 분류

| 폴더 | 설명 |
| --- | --- |
| `AbilityStone` | 어빌리티 스톤 로직 |
| `Bingo` | 빙고 보드 및 시너지 로직 |
| `Camera` | 카메라 관련 로직 |
| `Data` | 저장, JSON, CSV, 데이터 유틸리티 |
| `Dungeon` | 던전 관련 로직 |
| `Enemy` | 적/몬스터 관련 로직 |
| `Equipment` | 장비, 강화, 합성 관련 로직 |
| `Familiar` | 소환체/보조 엔티티 관련 로직 |
| `Gacha` | 가챠 로직 및 설정 |
| `Generic` | 범용 유틸리티 |
| `Interface` | 인터페이스 정의 |
| `Item` | 아이템 관련 공통 로직 |
| `Manager` | 전역 매니저, 데이터 로더, 스테이지 매니저 등 |
| `Map` | 맵 관련 로직 |
| `Player` | 플레이어 스탯/성장 관련 |
| `Quest` | 퀘스트 시스템 |
| `Refactoring` | 리팩토링 중인 공통 시스템, 모듈형 인벤토리 포함 |
| `ScreenShot` | 스크린샷 관련 기능 |
| `Service` | 서비스성 유틸리티 |
| `Skill` | 스킬 로직 |
| `SO` | ScriptableObject 정의 |
| `TestScripts` | 테스트/검증 스크립트 |
| `UI` | 기능별 UI 컨트롤러 |

### UI 폴더 분류

| 폴더 | 설명 |
| --- | --- |
| `AbilityStone` | 스톤 목록, 강화, 보너스 팝업 UI |
| `BingoUI` | 빙고 보드 UI |
| `Common` | 팝업, 오버레이, 공통 UI 컴포넌트 |
| `Core` | `UIControllerBase` 등 공통 기반 클래스 |
| `Currency` | 재화 표시 UI |
| `Debug` | 디버그 UI |
| `Dungeon` | 던전 입장/보상 UI |
| `Equipment` | 장비 리스트, 장착, 강화 UI |
| `Gacha` | 가챠 화면 및 결과 팝업 UI |
| `Menu` | 옵션, 캐릭터 정보, 잡화/상세 팝업 UI |
| `Pixie` | 픽시 메뉴 및 상세 UI |
| `PlayerLevel` | 레벨 성장 UI |
| `PlayerStat` | 스탯 강화 UI |
| `Quest` | 퀘스트 진행 UI |
| `Skill` | 액티브 스킬 및 상세/장착 UI |
| `SKillUI` | 기존 스킬 UI 자산/구조 |
| `Stage` | 전투 HUD, 보스 UI |
| `Trait` | 특성 노드 및 그룹 UI |
| `Utility` | 보조 UI 유틸리티 |

## 주요 진입 스크립트

처음 코드를 읽을 때는 아래 파일부터 보면 전체 구조를 빠르게 파악할 수 있습니다.

- `Assets/00. Scripts/Manager/DataManager.cs`
- `Assets/00. Scripts/Refactoring/Inventory/Core/InventoryManager.cs`
- `Assets/00. Scripts/UI/Core/UIControllerBase.cs`
- `Assets/00. Scripts/UI/Common/PopupStackService.cs`
- `Assets/00. Scripts/Data/Manager/AutoDataSaveManager.cs`
- `Assets/00. Scripts/Manager/Stage/StageManager.cs`
- `Assets/00. Scripts/Quest/QuestManager.cs`

## 실행 / 점검 가이드

1. Unity `6000.3.7f1`으로 프로젝트를 엽니다.
2. `Packages/manifest.json` 기준 패키지가 정상 설치되었는지 확인합니다.
3. `AddressableAssetsData` 설정과 `ServerData/<Platform>` 결과물이 현재 빌드 타깃과 맞는지 확인합니다.
4. CSV 테이블 에셋에 `CSV_Data` 라벨이 유지되고 있는지 확인합니다.
5. `TitleScene` 또는 `StageScene`부터 실행해 데이터 로드 및 UI 연결 상태를 확인합니다.

## 작업 시 참고 사항

- 이 프로젝트는 `기존 매니저 구조`와 `Refactoring/Inventory` 기반의 신규 구조가 일부 공존합니다.
- 인벤토리 수량 반영 문제를 수정할 때는 `InventoryManager` 이벤트 구독 여부를 함께 확인하는 편이 안전합니다.
- 팝업을 새로 만들 때는 `PopupStackService`와 공통 오버레이 패턴을 우선 사용하는 것이 일관성에 맞습니다.
- UI 연결은 가능하면 런타임 탐색보다 직렬화 참조를 우선하는 편이 현재 작업 흐름과 잘 맞습니다.
- Addressables 데이터 변경 시 `ServerData` 재빌드 여부를 반드시 확인해야 합니다.

## 문서 업데이트 권장 항목

- 실제 게임 플레이 루프와 재화 흐름도
- 저장 파일 위치와 포맷 예시
- Addressables 빌드 절차
- QA 체크리스트와 플랫폼별 빌드 방법
- CSV 테이블 명세서 링크

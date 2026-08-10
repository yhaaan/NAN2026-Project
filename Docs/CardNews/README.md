# 첫 경기 카드뉴스

게임 첫 경기 시작 전에 중앙에 한 번 표시되는 5장 카드뉴스다.

## 최종 문구와 이미지

| 장 | 문구 | 런타임 이미지 |
| --- | --- | --- |
| 1 | 유닛으로 싸우며 오목을 완성하는, 전투 오목입니다! | `Assets/Resources/CardNews/01_Overview.jpg` |
| 2 | 상점에서 유닛 하나를 골라 빈 교차점에 배치하세요. | `Assets/Resources/CardNews/02_Placement.jpg` |
| 3 | 양쪽이 하나씩 배치하면, 최대 10초 동안 자동 전투가 시작됩니다. | `Assets/Resources/CardNews/03_Combat.jpg` |
| 4 | 쓰러진 유닛은 보드에서 사라집니다. 배치와 조합으로 내 진형을 지키세요! | `Assets/Resources/CardNews/04_Defeat.jpg` |
| 5 | 유닛 다섯을 한 줄로 연결하고 끝까지 지켜내면 승리합니다! | `Assets/Resources/CardNews/05_Victory.jpg` |

## UI 동작

- 첫 장에서는 왼쪽 버튼이 비활성화된다.
- 오른쪽 버튼으로 다음 장, 왼쪽 버튼으로 이전 장을 연다.
- 다섯 개 진행 표시와 현재 페이지 숫자를 함께 표시한다.
- 모든 문구는 TextMeshProUGUI이며, 본문·버튼은 `Maplestory Bold SDF`, 페이지 숫자는 `Maplestory Light SDF`를 사용한다.
- 마지막 장에서는 오른쪽 버튼 대신 `게임 시작` 버튼을 표시한다.
- `게임 시작`을 누르기 전까지 첫 경기와 COM 배치를 시작하지 않는다.
- `게임 시작`을 누르면 `NAN2026.FirstMatchCardNews.Seen.v1` PlayerPrefs 값을 즉시 저장한다.
- 저장된 값을 발견한 다음 진입부터는 카드뉴스를 만들지 않고 바로 첫 경기를 시작한다.
- 닫힘 애니메이션이 끝나면 카드뉴스를 비활성화하고 첫 경기를 시작한다.
- 타이틀의 `GuideButton`은 시청 여부와 관계없이 카드뉴스를 다시 열며, 완료하면 본 게임으로 이동한다.
- 타이틀의 `ExitButton`은 빌드에서 게임을 종료하고 Unity Editor에서는 Play Mode를 종료한다.

## 구현 및 검수 자료

- UI 구현: `Assets/01. Scripts/07. UI/FirstMatchCardNewsView.cs`
- 경기 시작 연결: `Assets/01. Scripts/06. Match/GomokuGameController.cs`
- Play Mode 검증: `Assets/99. Tests/01. PlayMode/GomokuMvpPlayModeTests.cs`
- 첫 장 UI 촬영본: `UI/cardnews_page1.png`
- 마지막 장 UI 촬영본: `UI/cardnews_page5.png`
- 기존 1920×1080 원본 촬영본은 `Screenshots/`에 보관한다.
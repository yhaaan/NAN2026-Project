# 유닛 프리팹 제작 가이드

이 문서는 새로운 유닛의 월드 외형과 연출을 만드는 작업자를 위한 가이드입니다. 유닛의 능력치와 전투 로직은 프리팹에 넣지 않고 `UnitDefinitionSO`와 `UnitActionSO`에서 관리합니다.

## AI 에이전트에게 작업을 전달하는 방법

다른 작업자나 AI 에이전트에게 유닛 프리팹 제작을 요청할 때는 이 문서의 일부를 복사하지 말고 `Docs/UNIT_PREFAB_GUIDE.md` 경로를 함께 전달합니다. 에이전트가 저장소의 `AGENTS.md`와 이 문서를 먼저 끝까지 읽고, 기존 프리팹을 기준으로 작업하도록 요청합니다.

아래 요청문에서 대괄호 부분만 작업 대상에 맞게 바꿔 전달할 수 있습니다.

```text
저장소의 AGENTS.md와 Docs/UNIT_PREFAB_GUIDE.md를 먼저 끝까지 읽고 지침을 따라주세요.

다음 유닛의 월드 프리팹과 연결 에셋을 제작 또는 수정해주세요.
- Unit ID: [유닛 ID]
- Sprite: [Sprite 에셋 경로]
- UnitDefinitionSO: [Definition 에셋 경로]
- UnitActionSO: [Action 에셋 경로 또는 사용할 기존 Action]
- Accent Color: [역할색]
- Death Duration: [사망 연출 시간]

보드와 유닛 Sprite의 PPU는 65로 통일하고, 크기 차이는 Transform Scale이 아니라 원본 이미지 픽셀 크기로 표현해주세요.
기존 유닛 프리팹의 UnitRoot/Body 구조와 UnitView 참조를 유지하고, 필요한 .meta 파일도 함께 처리해주세요.
작업 전 기존 Git 변경을 확인하고 덮어쓰지 마세요. 작업 후 프리팹 참조, 컴파일, Unity Console 오류를 검증하고 미검증 항목을 보고해주세요.
```

Sprite나 Action이 아직 정해지지 않았다면 해당 항목에 `미정`이라고 적습니다. 이 경우 에이전트는 임의의 영구 에셋이나 전투 규칙을 만들지 않고, 기존 임시 에셋 또는 런타임 대체 표현을 사용해야 합니다.

## 1. 기본 구조

유닛 프리팹은 `Assets/02. Prefabs/01. Units/`에 둡니다. 새 프리팹은 기존 유닛 프리팹을 복제해서 만드는 것을 권장합니다.

```text
UnitRoot
├─ UnitView
└─ Body
   └─ SpriteRenderer
```

### UnitRoot

- 바둑판의 셀 위치와 공격 이동을 담당합니다.
- `UnitView`를 하나만 가집니다.
- Root에는 `SpriteRenderer`를 추가하지 않습니다.
- Root의 위치, 회전, 크기는 런타임에서 제어되므로 외형 조절에 사용하지 않습니다.

### Body

- 유닛의 실제 이미지와 시각 연출을 담당합니다.
- `SpriteRenderer`에 사용할 Sprite를 지정합니다.
- 위치와 회전 등 시각 기준은 Body에서 관리하고, 동작 연출은 `UnitView`의 DOTween 시퀀스가 처리합니다. 기본 표시 크기는 공통 PPU와 원본 이미지 픽셀 크기로 결정합니다.
- Sprite가 비어 있으면 런타임에 기본 원형 돌과 역할색 점이 생성됩니다.

### 연출 앵커

`VfxRoot`는 유닛에 종속되는 파티클의 부모입니다. 연결하지 않으면 런타임에 UnitRoot 원점에 자동 생성됩니다.

| 앵커 | 용도 | 기본 위치 |
| --- | --- | --- |
| `VfxRoot` | 피격·회복 피드백 파티클의 부모 | `(0, 0, 0)` |

## 2. UnitView 설정

| 필드 | 설정 방법 |
| --- | --- |
| `Body Renderer` | Body의 `SpriteRenderer`를 연결합니다. |
| `Body Root` | Body Transform을 연결합니다. |
| `Normalize Sprite Size` | 공통 PPU와 원본 픽셀 크기로 월드 크기를 결정하므로 기본적으로 끕니다. 런타임 대체 이미지처럼 크기가 불명확한 Sprite에만 사용합니다. |
| `Visual Diameter` | `Normalize Sprite Size`를 켠 예외 상황에서만 사용하는 목표 지름입니다. 기본값은 `0.78`입니다. |
| `Feedback Particle Material` | 선택 사항입니다. 비워 두면 런타임에 URP `Particles/Unlit` 머티리얼을 생성합니다. 직접 지정할 때도 같은 URP 호환 셰이더를 사용합니다. |
| `Vfx Root` | 선택 사항입니다. 유닛 VFX의 부모를 연결합니다. |

보드와 유닛 Sprite는 공통으로 `65 PPU`를 사용합니다. 월드 크기는 `원본 픽셀 크기 ÷ 65`로 결정되므로, 기본 유닛 크기 약 `0.78 world unit`에는 `51×51 px` 원본을 사용합니다. 더 크거나 작은 유닛은 Body Scale이 아니라 원본 이미지의 픽셀 크기로 차이를 표현합니다.

`Normalize Sprite Size`는 기본적으로 끕니다. 켜면 원본 픽셀 크기 차이가 무시되고 가장 긴 변이 `Visual Diameter`에 강제로 맞춰지므로, 런타임 대체 이미지처럼 원본 규격을 신뢰할 수 없는 경우에만 사용합니다.

## 3. DOTween 연출

`UnitView`는 Animator 없이 다음 공통 연출을 DOTween으로 실행합니다.

| 메서드 | 연출 | 기본 시간 |
| --- | --- | --- |
| `PlayAction` | 대상 방향으로 이동한 뒤 원위치로 복귀 | `0.22초` |
| `PlayHit` | 주황색 플래시와 피격 파티클 | `0.18초` |
| `PlayHeal` | 초록색 플래시와 회복 파티클 | `0.18초` |
| `PlayDeath` | Root 축소와 Sprite 페이드아웃 | Presentation의 `Death Duration` |

같은 종류의 연출을 다시 실행하면 기존 Tween을 종료하고 새 Tween을 시작합니다. 유닛을 다시 바인딩하거나 오브젝트를 파괴할 때도 실행 중인 Tween을 정리합니다. 파티클 머티리얼은 URP 호환 `Particles/Unlit`을 사용합니다.

## 4. 새 유닛 제작 순서

1. `Assets/02. Prefabs/01. Units/`에서 기존 유닛 프리팹 하나를 복제합니다.
2. 프리팹 이름을 `<UnitId>Unit` 형식으로 변경합니다.
3. UnitRoot의 `UnitView`와 Body 자식 구조를 유지합니다.
4. Body의 `SpriteRenderer`에 새 Sprite를 지정합니다.
5. 필요하면 Body의 위치와 회전을 조절합니다.
6. 보드에서 크기를 확인하고, 크기를 바꿔야 하면 공통 PPU는 유지한 채 원본 이미지의 픽셀 크기를 조정합니다.
7. 별도 파티클 부모가 필요하면 `VfxRoot`를 만들고 `UnitView`에 연결합니다.
8. Project 창에서 `Create > NAN2026 > Unit Presentation`을 선택해 `UnitPresentationSO`를 만듭니다.
9. Presentation의 `World Prefab`에 만든 `UnitView` 프리팹을 연결합니다.
10. `Accent Color`와 `Death Duration`을 설정합니다.
11. `UnitDefinitionSO`의 `Action`과 `Presentation`에 해당 에셋을 연결합니다.
12. 새 Definition을 게임에서 사용하려면 `UnitCatalogSO`의 Units 목록에 등록합니다.

권장 에셋 위치는 다음과 같습니다.

```text
Assets/02. Prefabs/01. Units/<UnitId>Unit.prefab
Assets/06. Data/00. Units/<UnitId>.asset
Assets/06. Data/01. Actions/<ActionName>.asset
Assets/06. Data/02. Presentations/<UnitId>Presentation.asset
```

## 5. 데이터와 외형의 역할 분리

| 대상 | 담당 내용 |
| --- | --- |
| `UnitDefinitionSO` | 이름, 설명, 역할, 체력, 공격력, 사거리, 행동 주기 |
| `UnitActionSO` | 공격·회복 대상 선택과 전투 효과 |
| `UnitPresentationSO` | 월드 프리팹, 역할색, DOTween 사망 연출 시간 |
| `UnitView` 프리팹 | Sprite, DOTween 연출, VFX 부모와 시각 계층 |

게임 규칙이나 능력치 계산을 `UnitView` 또는 DOTween 콜백에 넣지 않습니다. 반대로 Sprite와 연출 설정을 `UnitDefinitionSO`에 추가하지 않습니다.

## 6. 미리보기와 HP 표시

- 보드 교차점의 배치 미리보기는 실제 유닛 프리팹을 복제해서 사용합니다.
- 마우스를 따라다니는 배치 커서도 Body의 Sprite, 색상, 크기, 회전을 사용합니다.
- 따라서 미리보기 전용 Sprite를 별도로 만들 필요가 없습니다.
- HP 바는 공용 UI 프리팹 `Assets/02. Prefabs/02. UI/UnitHealthBar.prefab`을 보드 Canvas 아래에 복제해서 사용합니다.
- HP 바의 배경, Fill 이미지, 아군색과 적군색은 공용 UI 프리팹에서 수정합니다.
- HP 바의 위치와 크기, 체력 비율은 런타임에 보드 UI가 갱신합니다.
- 유닛 프리팹 아래에 HP 바를 추가하지 않습니다.

## 7. 완료 전 확인

- [ ] UnitRoot에 `UnitView`가 하나만 있는가?
- [ ] UnitRoot 아래에 `Body` 자식이 있는가?
- [ ] Root가 아닌 Body에 `SpriteRenderer`가 있는가?
- [ ] `Body Root`와 `Body Renderer` 참조가 올바른가?
- [ ] 보드 위 실제 유닛과 배치 미리보기의 모양과 크기가 일치하는가?
- [ ] `UnitPresentationSO.World Prefab`이 새 프리팹을 가리키는가?
- [ ] `UnitDefinitionSO`에 Action과 Presentation이 모두 연결됐는가?
- [ ] HP UI를 프리팹에 중복으로 추가하지 않았는가?
- [ ] Unity Console에 Missing Reference, 셰이더 또는 머티리얼 오류가 없는가?

## 8. 피해야 할 작업

- UnitRoot에 SpriteRenderer를 달아 외형을 구성하지 않습니다.
- 이미지 크기를 맞추기 위해 UnitRoot Scale을 변경하지 않습니다.
- 런타임에 생성되는 `StoneInner`, `RoleDot`, `FeedbackParticles`를 프리팹에 직접 추가하지 않습니다.
- 유닛 프리팹에 전투 능력치, 타깃 선택 또는 HP UI 로직을 넣지 않습니다.
- 기존 `.meta` 파일을 삭제하거나 프리팹 GUID를 불필요하게 변경하지 않습니다.

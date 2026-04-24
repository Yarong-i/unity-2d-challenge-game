# Challenge Climbing Vertical Slice / 도전형 등반 버티컬 슬라이스

## Project Overview / 프로젝트 개요

This project is a Unity 6 solo challenge climbing prototype focused on validating game feel before presentation polish.  
이 프로젝트는 Unity 6 기반의 1인용 도전형 등반 프로토타입으로, 연출보다 먼저 조작감과 플레이 감각을 검증하는 데 초점을 둡니다.

The current build milestone centers on a launch-based player movement loop in the main scene, `Assets/asd.unity`.  
현재 빌드 마일스톤은 메인 씬 `Assets/asd.unity`에서 발사형 플레이어 이동 핵심 루프를 구현하는 데 맞춰져 있습니다.

## Current Slice / 현재 슬라이스

- Launch-based player movement with `Stable`, `Charging`, and `Airborne` states  
  `Stable`, `Charging`, `Airborne` 상태를 사용하는 발사형 플레이어 이동
- Launch preparation only while grounded and moving slowly enough to count as stable  
  바닥에 붙어 있고 속도가 충분히 낮아 Stable 판정일 때만 발사 준비 가능
- Mouse-driven aiming that launches the player in the opposite direction from the cursor pull  
  마우스 당기기 방향의 반대편으로 플레이어를 발사하는 조준 방식
- Auto-launch after the maximum charge time, with launch force clamped between minimum and maximum values  
  최대 차지 시간 경과 시 자동 발사되며, 발사 힘은 최소/최대 범위로 제한
- Very light airborne steering to preserve the committed launch feel  
  발사 중심 감각을 유지하기 위한 매우 약한 공중 보정
- Existing combat, health, enemy, hazard, and clear systems kept available from the earlier prototype setup  
  이전 프로토타입에 있던 전투, 체력, 적, 함정, 클리어 시스템은 그대로 유지

## What Changed In This Update / 이번 업데이트 변경점

- Added `Assets/ChallengePlayerController.cs` as the game-facing launch movement controller  
  실제 게임용 발사 이동 컨트롤러 `Assets/ChallengePlayerController.cs` 추가
- Disabled the practice controller `PlayerMoverRB` on the `player` object in `Assets/asd.unity`  
  `Assets/asd.unity`의 `player` 오브젝트에서 연습용 `PlayerMoverRB` 비활성화
- Attached `ChallengePlayerController` to the scene player and locked `Rigidbody2D` rotation  
  씬의 플레이어에 `ChallengePlayerController`를 연결하고 `Rigidbody2D` 회전을 고정
- Updated `Codex/scene_request.json` so the same scene setup can be reapplied through the Unity bridge  
  Unity 브리지를 통해 같은 씬 구성을 다시 적용할 수 있도록 `Codex/scene_request.json` 갱신

## Controls / 조작 방법

- `Left Mouse Button Hold`: Start charging while stable  
  `마우스 왼쪽 버튼 홀드`: Stable 상태에서 차지 시작
- `Release Left Mouse Button`: Launch  
  `마우스 왼쪽 버튼 해제`: 발사
- `Move cursor away from the desired launch direction`: Pull and launch in the opposite direction  
  `가고 싶은 방향의 반대편으로 마우스를 끌기`: 반대 방향으로 발사
- `A / D` or arrow keys in the air: Very light air steering  
  `A / D` 또는 방향키를 공중에서 입력: 매우 약한 공중 보정
- `J`: Attack  
  `J`: 공격

## How To Run / 실행 방법

1. Install Unity `6000.0.57f1` through Unity Hub.  
   Unity Hub에서 Unity `6000.0.57f1`을 설치합니다.
2. Open this folder as a Unity project.  
   이 폴더를 Unity 프로젝트로 엽니다.
3. Open the primary scene `Assets/asd.unity`.  
   메인 씬 `Assets/asd.unity`를 엽니다.
4. Press Play.  
   Play를 눌러 실행합니다.
5. Let the player settle on the ground, hold the left mouse button to charge, then release to launch.  
   플레이어가 바닥에서 안정 상태가 되면 마우스 왼쪽 버튼을 홀드해 차지하고, 해제해서 발사합니다.

## What To Test / 테스트 포인트

- The player can only begin charging after regaining a stable grounded state  
  플레이어가 다시 Stable 상태가 되었을 때만 차지를 시작할 수 있는지
- Short pulls create weaker launches and long pulls create stronger launches  
  짧게 당기면 약한 발사, 길게 당기면 강한 발사가 되는지
- Holding too long triggers an automatic launch at max charge time  
  오래 홀드하면 최대 차지 시간 뒤 자동 발사가 되는지
- Air control stays intentionally weak after launch  
  발사 후 공중 제어가 의도적으로 약하게 유지되는지

## Automation Notes / 자동화 메모

Scene changes should go through `Codex/scene_request.json` and `Assets/Editor/CodexUnityBridge.cs` instead of manual serialized scene edits.  
씬 변경은 수동 직렬화 편집 대신 `Codex/scene_request.json`과 `Assets/Editor/CodexUnityBridge.cs`를 통해 처리해야 합니다.

```powershell
.\tools\unity-batch.ps1 -ApplySceneRequest -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.0.57f1\Editor\Unity.exe"
```

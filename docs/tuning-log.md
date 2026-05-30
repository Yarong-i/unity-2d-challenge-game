# Tuning Log

## 2026-04-25 - Launch Movement Checkpoint / 발사형 이동 체크포인트

- KR: 발사 방향, 발사 힘, 당김 홀드 시간은 현재 기준에서 일단 만족스럽다.
- EN: Launch direction, launch force, and charge hold time feel acceptable for this checkpoint.
- KR: 플랫폼 끝에 걸친 상태에서 `Stable` 판정이 잘 들어오지 않는 문제는 아직 남아 있다.
- EN: The `Stable` detection issue when the player is hanging on a platform edge still remains.
- KR: 다음 작업은 `Stable` 판정 개선이 우선이다.
- EN: The next task will focus on improving `Stable` detection.

## 2026-04-27 - Camera Follow And Wall Bounce Checkpoint / 카메라 추적 및 벽 튕김 체크포인트
- KR: 벽에 붙어서 떨어지는 문제는 개선됐지만 튕김이 약간 강해, `wallBounceMultiplier`, `wallBounceUpwardBoost`, `maxWallBounceSpeed`, `wallDetachSpeed` 수치를 완화했다.
- EN: Wall sticking was improved, but the bounce felt slightly strong, so `wallBounceMultiplier`, `wallBounceUpwardBoost`, `maxWallBounceSpeed`, and `wallDetachSpeed` were softened.
- KR: 플레이어가 화면 밖으로 사라지는 문제를 `CameraFollow2D` 추적 카메라로 완화했다.
- EN: Added `CameraFollow2D` to reduce cases where the player leaves the screen.
- KR: 기본 카메라 구도는 `targetViewportPosition = (0.5, 0.38)`로 잡아 플레이어가 화면 중앙보다 약간 아래에 보이게 했다.
- EN: The default camera composition uses `targetViewportPosition = (0.5, 0.38)` so the player sits slightly below center.
- KR: 프레임 찢어짐처럼 보이는 문제는 오늘 범위에서 제외하고, 추후 VSync, Game View, 그래픽 설정 쪽에서 별도로 확인한다.
- EN: The possible frame tearing issue is left for a later pass through VSync, Game View, and graphics settings.

## 2026-05-08 - Minimal Checkpoint Respawn Loop / 최소 체크포인트 리스폰 루프
- KR: 도전형 등반 게임용 최소 체크포인트/리스폰 루프를 추가했다.
- EN: Added a minimal checkpoint and respawn loop for the solo challenge climbing game.
- KR: 플레이어가 `fallYThreshold` 아래로 떨어지면 현재 리스폰 위치로 돌아가며, `R` 키로 수동 리스폰할 수 있다.
- EN: The player respawns at the current respawn point after falling below `fallYThreshold`, and can manually respawn with the `R` key.
- KR: 리스폰 시 `Rigidbody2D`의 선속도와 각속도를 초기화해 재시작 감각을 안정화했다.
- EN: Respawn now clears the `Rigidbody2D` linear and angular velocity for a stable restart feel.
- KR: `Checkpoint_Test`는 현재 씬의 `player`, `Goal`, 바닥 Collider2D 위치를 기준으로 초반 진행 경로의 안정적인 플랫폼 근처에 자동 배치했다.
- EN: `Checkpoint_Test` was placed automatically near an early stable platform based on the current scene's `player`, `Goal`, and ground Collider2D positions.
- KR: 이번 작업에서는 발사 이동, 벽 반발, 카메라 추적 설정을 변경하지 않았다.
- EN: Launch movement, wall bounce, and camera follow settings were not changed in this pass.

## 2026-05-27 - Timer, Pause, And Scene Cleanup / 타이머, 일시정지, 씬 정리
- KR: 연습용 씬 오브젝트를 정리해 현재 도전형 등반 프로토타입에 필요한 구성만 남겼다.
- EN: Cleaned up practice scene objects so the current challenge-climbing prototype keeps only the needed setup.
- KR: `player`, `Main Camera`, `GameSessionManager`, `RespawnManager`, `Checkpoint_Test`, 테스트용 플랫폼은 유지했다.
- EN: Kept `player`, `Main Camera`, `GameSessionManager`, `RespawnManager`, `Checkpoint_Test`, and the test platforms.
- KR: 불필요한 적, 위험물, 예전 리스폰 지점, 순찰 포인트, 예전 `Goal` 오브젝트는 제거했다.
- EN: Removed unused enemy, hazard, old respawn point, patrol points, and old `Goal` objects.
- KR: 타이머 UI, Esc 일시정지, 발사형 이동, 카메라 추적, 체크포인트/리스폰 기능은 유지했다.
- EN: Preserved timer UI, Esc pause, launch movement, camera follow, and checkpoint/respawn behavior.

## 2026-05-27 - Runtime Main Menu And Pause Settings / 런타임 메인 메뉴와 일시정지 설정
- KR: `Assets/asd.unity` 안에서 동작하는 런타임 메인 메뉴 오버레이를 추가했다.
- EN: Added a runtime main menu overlay that works inside `Assets/asd.unity`.
- KR: Start, Settings, Quit 기본 메뉴 흐름과 Settings placeholder를 추가했다.
- EN: Added the basic Start, Settings, and Quit menu flow with a Settings placeholder.
- KR: Start 버튼을 누르기 전에는 게임이 시작되지 않고 타이머가 `Time 00:00.00`에서 대기하도록 고쳤다.
- EN: Fixed the pre-start state so the game does not begin and the timer waits at `Time 00:00.00` until Start is pressed.
- KR: `RunTimerUI`에 명시적인 `ResetTimer()`, `StartTimer()`, `StopTimer()` 흐름을 추가했다.
- EN: Added explicit `ResetTimer()`, `StartTimer()`, and `StopTimer()` flow to `RunTimerUI`.
- KR: Esc 일시정지 메뉴에 Settings 버튼과 Settings placeholder를 추가했다.
- EN: Added a Settings button and Settings placeholder to the Esc pause menu.
- KR: 메인 메뉴와 일시정지 메뉴가 동시에 겹치지 않도록 시작 전 Esc pause를 막고 패널 상태를 분리했다.
- EN: Prevented the main menu and pause menu from overlapping by blocking Esc pause before Start and separating panel state.
- KR: Pause 중 Settings를 열어도 `Time.timeScale = 0` 상태가 유지되도록 했다.
- EN: Kept `Time.timeScale = 0` while Settings is open from the pause menu.
- KR: 기존 발사 이동, 벽 반발, 카메라 추적, 체크포인트/리스폰 스크립트는 건드리지 않았다.
- EN: Left the existing launch movement, wall bounce, camera follow, checkpoint, and respawn scripts untouched.

## 2026-05-27 - Fullscreen Settings UX / 전체화면 설정 UX
- KR: Settings 메뉴의 Fullscreen 토글 UX를 개선해 현재 상태를 더 명확하게 보이도록 했다.
- EN: Improved the Fullscreen toggle UX in Settings so the current state is easier to see.
- KR: `Screen.fullScreen`과 `Screen.fullScreenMode`를 함께 사용해 전체화면 상태를 반영한다.
- EN: Uses both `Screen.fullScreen` and `Screen.fullScreenMode` to apply fullscreen state.
- KR: Unity 에디터에서는 Game View가 실제 전체화면처럼 바뀌지 않을 수 있어 `Fullscreen: On/Off` 상태 라벨과 짧은 안내 문구를 추가했다.
- EN: Added a `Fullscreen: On/Off` state label and a short note because the Unity Editor Game View may not visibly resize.
- KR: Main Menu Settings와 Pause Settings가 같은 fullscreen 상태를 공유하도록 했다.
- EN: Main Menu Settings and Pause Settings now share the same fullscreen state.
- KR: Volume 슬라이더와 기존 Start/Settings/Quit, Esc pause 흐름은 유지했다.
- EN: Preserved the Volume slider and existing Start/Settings/Quit and Esc pause flows.
- KR: 기존 발사 이동, 벽 반발, 카메라 추적, 체크포인트/리스폰, 타이머는 건드리지 않았다.
- EN: Left launch movement, wall bounce, camera follow, checkpoint/respawn, and timer behavior untouched.

## 2026-05-28 - Run Start Respawn / 시작점 리스폰
- KR: 기본 모드에서 중간 체크포인트를 제거하고, 도전형 등반 게임의 긴장감을 위해 실패 시 시작 위치로 돌아가도록 변경했다.
- EN: Removed mid-run checkpoints from the default mode and changed failures to respawn at the start position to better fit the challenge-climbing tension.
- KR: `Checkpoint_Test` 오브젝트를 제거하고 `RespawnManager2D`를 run start position 기반 리스폰 구조로 정리했다.
- EN: Removed the `Checkpoint_Test` object and reorganized `RespawnManager2D` around a run start position respawn flow.
- KR: `R` 키 수동 리스폰과 `fallYThreshold` 자동 리스폰은 유지했다.
- EN: Kept manual respawn on the `R` key and automatic respawn below `fallYThreshold`.
- KR: 발사 이동, 벽 반발, 카메라 추적, 타이머, 메뉴/일시정지는 건드리지 않았다.
- EN: Left launch movement, wall bounce, camera follow, timer, menu, and pause behavior untouched.

## 2026-05-28 - Stage01 Blockout And Respawn Guard / Stage01 블록아웃과 리스폰 입력 차단
- KR: 첫 스테이지 블록아웃을 추가하고, 발사형 이동을 검증할 수 있도록 넓은 시작 바닥과 점진적으로 올라가는 플랫폼을 배치했다.
- EN: Added the first Stage01 blockout with a wide start floor and gradually rising platforms to validate launch-based movement.
- KR: Stage01 플랫폼이 중심만 작게 보이던 문제를 수정하고, SpriteRenderer 시각 크기가 BoxCollider2D/플랫폼 크기와 일치하도록 조정했다.
- EN: Fixed Stage01 platforms rendering only at the center and aligned SpriteRenderer visual size with BoxCollider2D/platform size.
- KR: GoalMarker가 점처럼 보이지 않도록 노란색 마커의 크기와 색상을 조정했다.
- EN: Adjusted GoalMarker size and yellow color so it no longer appears as a tiny dot.
- KR: Esc 일시정지, Main Menu, Settings 상태에서는 R 수동 리스폰과 fallYThreshold 자동 리스폰 체크를 막고, 정상 플레이 중에는 둘 다 유지했다.
- EN: Blocked R manual respawn and fallYThreshold checks during Esc pause, Main Menu, and Settings, while preserving both during active play.
- KR: 예전 연습용 루트 Square 계열 구조물은 제거하고, Stage01_Blockout, player, Main Camera, GameSessionManager, RespawnManager는 유지했다.
- EN: Removed old root Square practice structures while keeping Stage01_Blockout, player, Main Camera, GameSessionManager, and RespawnManager.
- KR: 기존 발사 이동, 벽 반발, 카메라 추적, 타이머, 메인 메뉴/일시정지, 설정 메뉴 로직은 불필요하게 건드리지 않았다.
- EN: Left existing launch movement, wall bounce, camera follow, timer, main menu/pause, and settings menu logic unchanged.
- KR: 이번 블록아웃은 최종 맵이 아니라 조작감 검증용 1차 구간이다.
- EN: This blockout is a first-pass feel-test section, not the final map.

## 2026-05-31 - Stage01 Goal And Stage Clear / Stage01 골과 스테이지 클리어
- KR: `Stage01_GoalMarker`를 실제 클리어 지점으로 연결했다.
- EN: Connected `Stage01_GoalMarker` as the real stage clear point.
- KR: 플레이어가 Goal에 닿으면 런타임 `STAGE CLEAR` UI가 표시된다.
- EN: When the player touches the Goal, a runtime `STAGE CLEAR` UI is shown.
- KR: 클리어 시 `RunTimerUI`가 정지되고 Clear Time을 표시한다.
- EN: On clear, `RunTimerUI` stops and the clear time is displayed.
- KR: Restart 버튼은 시작 위치 리스폰과 타이머 리셋/재시작을 수행한다.
- EN: The Restart button respawns at the start position and resets/restarts the timer.
- KR: Quit 버튼을 추가해 에디터에서는 Play Mode 종료, 빌드에서는 애플리케이션 종료를 처리한다.
- EN: Added a Quit button that exits Play Mode in the Editor and quits the app in builds.
- KR: 기본 모드는 체크포인트 없이 시작점 리스폰 구조를 유지한다.
- EN: The default mode keeps the start-position respawn structure without checkpoints.
- KR: 기존 발사 이동, 벽 반발, 카메라 추적, 메인 메뉴, 일시정지, 설정 메뉴는 불필요하게 건드리지 않았다.
- EN: Existing launch movement, wall bounce, camera follow, main menu, pause, and settings menu behavior were left untouched.

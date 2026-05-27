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

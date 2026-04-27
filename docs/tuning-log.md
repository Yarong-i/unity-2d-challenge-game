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

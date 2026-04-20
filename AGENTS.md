# Codex Project Rules

## Current Phase

- This project is now in the build phase, not the setup phase.
- Codex, Unity, and GitHub baseline setup is considered complete.
- Current game direction: a solo challenge climbing game.
- The first build milestone focuses on playable game feel before presentation polish:
  - Launch-based player movement.
  - Stable-state detection.
  - Basic terrain.
  - Checkpoint or start/restart structure.
  - Timer UI.
  - Esc pause.
  - Basic menu: Start, Settings, Quit.
- Movement feedback and control feel validation take priority over cinematic presentation, visual polish, and nonessential effects.

## Work Planning

- Before starting a new feature, summarize which files will be changed and why.
- Keep changes scoped to the requested feature or fix.
- Do not edit unrelated assets, settings, or scenes while implementing gameplay.

## Scene Safety

- Primary scene is `Assets/asd.unity`.
- Do not edit `Assets/Scenes/SampleScene.unity` unless the user explicitly asks for that exact scene.
- Do not edit raw `.unity`, `.prefab`, or `.meta` files by hand.
- Scene changes must go through the Unity Editor bridge.
- Use `Codex/scene_request.json` as the request file for scene object creation or scene edits.
- When Unity is open, do not run `tools/unity-batch.ps1`.
- When Unity is open, apply scene requests only through `Tools > Codex > Apply Scene Request`.
- When Unity is closed, `tools/unity-batch.ps1 -ApplySceneRequest` may be used as an exception to apply scene requests through Unity batch mode.

## Code And Assets

- Gameplay C# scripts may be edited only for gameplay changes explicitly requested by the user.
- If practice controls differ from the real game controls, prefer creating a new game controller, such as `ChallengePlayerController.cs`, instead of directly rewriting `PlayerMoverRB.cs`.
- Keep generated editor automation under `Assets/Editor/`.
- Keep Codex request/config files under `Codex/`.
- Keep local automation scripts under `tools/`.
- Do not edit `Packages/manifest.json` unless the user explicitly asks for package changes.
- Do not edit `ProjectSettings/*` unless the user explicitly asks for project settings changes.
- Before changing scene objects, prefer adding or updating bridge-supported requests instead of touching serialized Unity YAML.

## Documentation And Git

- README updates must be bilingual: Korean + English.
- Commit messages must be bilingual: Korean + English.
- PR descriptions must be bilingual: Korean + English.
- Portfolio-facing docs should explain what changed, how to run it, and what the player can do.
- Before pushing to GitHub, summarize the changed files and get user approval.

## Korean Notes

- 현재 프로젝트는 세팅 단계가 아니라 실제 제작 단계입니다.
- 주 작업 씬은 `Assets/asd.unity`입니다.
- 사용자가 명시적으로 요청하기 전까지 `Assets/Scenes/SampleScene.unity`는 수정하지 않습니다.
- `.unity`, `.prefab`, `.meta` 파일은 직접 손으로 수정하지 않습니다.
- 씬 오브젝트 생성/변경은 Unity Editor 브리지를 통해 처리합니다.
- Unity가 열려 있을 때는 `tools/unity-batch.ps1`을 실행하지 않습니다.
- Unity가 열려 있을 때 씬 요청 반영은 `Tools > Codex > Apply Scene Request`만 사용합니다.
- Unity가 닫혀 있을 때만 예외적으로 `tools/unity-batch.ps1 -ApplySceneRequest`를 사용할 수 있습니다.
- 사용자가 명시적으로 요청하지 않으면 `Packages/manifest.json`과 `ProjectSettings/*`는 수정하지 않습니다.
- GitHub push 전에는 변경 파일을 요약하고 사용자 승인을 받습니다.
- 게임플레이 C# 스크립트는 사용자가 명시적으로 요청한 게임플레이 변경에 한해 수정할 수 있습니다.
- 연습용 조작과 실제 게임 조작이 다르면 `PlayerMoverRB.cs`를 바로 수정하기보다 새 게임용 컨트롤러 생성을 우선합니다.
- 현재 1차 목표는 발사형 이동, 안정 상태 판정, 기본 지형, 체크포인트 또는 재시작 구조, 시간 표시 UI, Esc 일시정지, 기본 메뉴입니다.
- 이동 피드백과 조작감 검증은 연출보다 우선합니다.
- 새 기능 작업 전에는 어떤 파일을 바꿀지와 왜 바꾸는지 먼저 요약합니다.
- README, 커밋 메시지, PR 설명은 한국어와 영어를 함께 작성합니다.

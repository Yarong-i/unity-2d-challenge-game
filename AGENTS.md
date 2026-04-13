# Codex Project Rules

## Scene Safety

- Primary scene is `Assets/asd.unity`.
- Do not edit `Assets/Scenes/SampleScene.unity` unless the user explicitly asks for that exact scene.
- Do not edit raw `.unity`, `.prefab`, or `.meta` files by hand.
- Scene changes must go through a Unity Editor bridge.
- Use `Codex/scene_request.json` as the request file for scene object creation or scene edits.
- Use `tools/unity-batch.ps1 -ApplySceneRequest` to apply scene requests through Unity.

## Code And Assets

- Gameplay C# scripts can be edited directly only when the user asks for gameplay/code changes.
- Keep generated editor automation under `Assets/Editor/`.
- Keep Codex request/config files under `Codex/`.
- Keep local automation scripts under `tools/`.
- Before changing scene objects, prefer adding or updating bridge-supported requests instead of touching serialized Unity YAML.

## Documentation And Git

- README updates must be bilingual: Korean + English.
- Commit messages must be bilingual: Korean + English.
- PR descriptions must be bilingual: Korean + English.
- Portfolio-facing docs should explain what changed, how to run it, and what the player can do.

## Korean Notes

- 주 작업 씬은 `Assets/asd.unity`입니다.
- 사용자가 명시적으로 요청하기 전까지 `Assets/Scenes/SampleScene.unity`는 수정하지 않습니다.
- `.unity`, `.prefab`, `.meta` 파일은 직접 손으로 수정하지 않습니다.
- 씬 오브젝트 생성/변경은 Unity Editor 브리지를 통해 처리합니다.
- README, 커밋 메시지, PR 설명은 한국어와 영어를 함께 작성합니다.

# 2D Platformer Practice / 2D 플랫포머 연습

## 프로젝트 소개 / Project Overview

이 프로젝트는 Unity 6 기반의 2D 플랫포머 프로토타입입니다. 플레이어 이동, 점프, 대시, 공격, 적, 위험 지대, 클리어 지점을 작은 단위로 구현하며 포트폴리오용으로 정리하고 있습니다.

This project is a Unity 6 based 2D platformer prototype. It focuses on building portfolio-ready gameplay pieces such as player movement, jumping, dashing, attacking, enemies, hazards, and a clear goal.

## 현재 구현 기능 / Current Features

- 플레이어 좌우 이동, 가속/감속 감각 조정 / Player horizontal movement with acceleration and deceleration tuning
- 점프, 코요테 타임, 점프 버퍼, 짧은 점프 컷 / Jumping with coyote time, jump buffering, and short-hop jump cut
- 지상 및 공중 대시 / Ground and air dash
- 플레이어 공격 입력과 히트박스 판정 / Player attack input with hitbox detection
- 적 순찰, 적 체력, 피격 피드백, 사망 처리 / Enemy patrol, enemy health, hit feedback, and death handling
- 적 접촉 피해와 위험 지대 피해 / Contact damage and hazard damage zones
- 플레이어 HP, 피격 무적, 넉백, 리스폰 / Player HP, invincibility frames, knockback, and respawn
- 스테이지 클리어 트리거 / Stage clear trigger
- HP UI와 플레이어 애니메이션 연동 / HP UI and player animation integration
- Codex용 Unity Editor 브리지 초안 / Draft Unity Editor bridge for Codex automation

## 앞으로 구현할 기능 / Planned Features

- 클리어 UI와 다음 스테이지 흐름 / Clear UI and next-stage flow
- 적 종류 추가와 공격 패턴 확장 / More enemy types and attack patterns
- 체크포인트와 낙사 처리 / Checkpoints and fall-death handling
- 사운드, 이펙트, 카메라 연출 / Sound, effects, and camera polish
- GitHub 포트폴리오용 스크린샷과 플레이 영상 / Screenshots and gameplay video for GitHub portfolio
- Unity Editor 브리지 명령 확장 / Expanded Unity Editor bridge commands

## 사용 기술 / Tech Stack

- Unity `6000.0.57f1`
- C#
- Unity 2D Physics
- Universal Render Pipeline 2D
- Unity Input Manager style input
- TextMesh Pro / UGUI
- PowerShell automation

## 실행 방법 / How To Run

1. Unity Hub에서 Unity `6000.0.57f1` 또는 호환되는 Unity 6 버전을 설치합니다.  
   Install Unity `6000.0.57f1` or a compatible Unity 6 version through Unity Hub.

2. 이 폴더를 Unity 프로젝트로 엽니다.  
   Open this folder as a Unity project.

3. 주 작업 씬인 `Assets/asd.unity`를 엽니다.  
   Open the primary working scene, `Assets/asd.unity`.

4. Play 버튼을 눌러 실행합니다.  
   Press Play to run the project.

5. 기본 조작: 좌우 이동 `A/D` 또는 방향키, 점프 `Space`, 대시 `Left Shift`, 공격 `J`.  
   Default controls: move with `A/D` or arrow keys, jump with `Space`, dash with `Left Shift`, attack with `J`.

## 자동화 메모 / Automation Notes

씬 오브젝트 생성이나 씬 변경은 Unity 직렬화 파일을 직접 수정하지 않고 `Codex/scene_request.json`과 `Assets/Editor/CodexUnityBridge.cs`를 통해 처리합니다.

Scene object creation and scene changes should go through `Codex/scene_request.json` and `Assets/Editor/CodexUnityBridge.cs`, without manually editing Unity serialized files.

```powershell
.\tools\unity-batch.ps1 -ApplySceneRequest
```

# 5. 철문 화면 전환 후 게임 시작

**같은 씬에서 초기화하는 버전:** [Additive 튜토리얼](../../ScreenTransition/TUTORIAL_ADDITIVE.md). `Connect Grid Demo In-Place Transition` 메뉴로 현재 씬을 유지하는 모드를 연결할 수 있습니다.

[공용 API 적용 튜토리얼](../../ScreenTransition/TUTORIAL.md)

Play를 종료하고 기존 메뉴 씬 `Assets/GridSystem/Scene/m.unity`를 연 다음 `Tools > Screen Transition > Connect Grid Demo Start Transition`을 실행합니다.

도구는 메뉴 씬을 게임 씬으로 복사하고 GameManager의 목적지를 연결하며, 공용 철문 프리팹과 Renderer Feature를 설치합니다. 전역 Build Scene List에 두 씬을 등록합니다. 활성 Build Profile이 별도 Scene List를 사용한다면 해당 목록도 확인하세요.

StartButton의 기존 `GameManager.GameLoop()` 연결은 유지합니다. 문 닫힘 → 게임 씬 로드 → 문 열림 완료 후 새 GameManager가 Playing 상태로 바뀝니다. 타이머·플레이어 입력·몬스터·HUD도 그때 시작됩니다.

직접 설정할 경우 GameManager의 Use Scene Transition을 켜고 Gameplay Scene에 목적지 경로를 입력합니다. 비워 두면 현재 씬을 재로드하고 게임을 시작합니다. 기존 즉시 시작 실습은 Use Scene Transition을 끄면 사용할 수 있습니다.

코드 재사용, 일반 버튼 연결, 도착 이벤트, UI/입력 설정 및 확인 항목은 위 공용 튜토리얼에 정리되어 있습니다.

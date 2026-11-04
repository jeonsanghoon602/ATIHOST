# PR #16 Review

- PR: `feat: add Equipment Profile management (HOST-004)`
- Reviewed commit: `cccce0e6cd0659b7100bce655b2d875b2c77bc80`
- Review date: 2026-11-04
- Verdict: 수정 후 재검토

## 변경 요약

JSON Equipment Profile 모델, 검증, 저장소와 관련 단위 테스트를 추가한다. 계층 구조는 기존 원칙에 맞지만, 병합 전에 아래 항목을 수정해야 한다.

## Must fix

### 1. Device ID 범위 불일치

`HostSimulator.Domain/EquipmentProfile.cs:79`는 Device ID를 0~32767로 제한한다. 기존 `ConnectionParameters`와 Secs4Net 2.4.4는 `ushort` 범위인 0~65535를 지원하므로 32768~65535의 정상 설정이 Profile에서 거부된다.

최대값을 65535로 통일하고 65535 경계 테스트를 추가한다.

### 2. 필수 접속 설정 누락을 검출하지 못함

`HostSimulator.Infrastructure/Profile/ProfileDto.cs:16`의 `isActive`와 `deviceId`가 JSON에 없으면 각각 `false`, `0`으로 조용히 대체된다. 이름, IP, Port만 포함된 JSON이 검증을 통과하여 잘못된 연결 역할이나 Device ID를 사용할 수 있다.

필수 JSON 속성으로 표시하거나 nullable DTO 값으로 누락 여부를 검증하고 테스트한다.

### 3. Reconnect Interval과 T5 관계 검증 누락

`HostSimulator.Domain/EquipmentProfile.cs:88`은 각 값의 개별 범위만 검사한다. `docs/state-transition.md`는 `Reconnect Interval >= T5`를 요구하므로 `T5=10`, `ReconnectIntervalSeconds=1` 같은 설정을 거부해야 한다.

두 값의 관계를 검증하고 실패 테스트를 추가한다.

### 4. Timer 상한값의 근거가 없음

`HostSimulator.Domain/EquipmentProfile.cs:82`의 T3/T8 120초, T5/T6/T7 240초 상한은 Issue나 현재 설비 문서에 정의되어 있지 않다. 저장소 지침은 미확정 Timer를 추측하지 않도록 요구한다.

Interface Specification 또는 승인된 Issue 요구사항에서 범위를 확정한 뒤 적용한다.

## Should fix

### 5. Profile 파일 저장이 원자적이지 않음

`HostSimulator.Infrastructure/Profile/JsonProfileRepository.cs:70`은 원본 파일을 직접 덮어쓴다. 쓰기 중 프로세스 종료나 디스크 오류가 발생하면 기존 Profile 파일까지 손상될 수 있다.

같은 디렉터리의 임시 파일에 쓴 뒤 원본을 교체한다.

## 검증 결과

- Debug x64 빌드: 경고 0, 오류 0
- Debug 테스트: 75개 통과
- Release x64 빌드: 경고 0, 오류 0
- Release 테스트: 75개 통과
- Equipment Simulator 및 실제 앱 통합: 미검증

## 결론

1~4를 수정한 뒤 재검토가 필요하다. 5도 Profile 데이터 손실 방지를 위해 이번 PR에서 함께 처리하는 것이 안전하다.

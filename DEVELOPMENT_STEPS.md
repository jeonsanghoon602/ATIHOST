# ATIHOST 개발 단계

## 1. 목적

이 문서는 .NET Framework 4.7.2 기반 반도체 설비 Host Simulator를 작은 작업 단위로 개발하기 위한 실행 계획이다.

개발은 4개 마일스톤과 11개 작업으로 진행한다. 각 작업은 하나의 GitHub Issue로 관리하며, 완료 조건을 충족하고 검증을 통과한 뒤 다음 단계로 이동한다.

```text
M1 기반 구축
→ M2 기본 통신
→ M3 자동 Scenario
→ M4 안정화 및 배포
```

## 2. 공통 기술 기준

- Language: C#
- Runtime: .NET Framework 4.7.2
- UI: WPF
- Pattern: MVVM
- Platform Target: 대상 SDK와 배포 PC에 맞춰 x86 또는 x64 명시
- Protocol SDK: Secs4Net 2.4.4 버전 고정 또는 4.7.2 호환 상용 SDK
- Equipment Profile: JSON
- Scenario: YAML 또는 JSON
- Test: xUnit
- Source Encoding: 새 C# 프로젝트는 UTF-8
- BCB 연동 소스: `.cpp`, `.h`, `.dfm`은 CP949/Windows-949 유지

최신 Secs4Net 3.x는 .NET 8/10 대상이므로 이 프로젝트에서 직접 사용하지 않는다.

## 3. 전체 의존 관계

```text
HOST-001 요구사항 및 설비 사양
    ↓
HOST-002 개발 환경 및 Solution
    ↓
HOST-003 통신 SDK PoC
    ↓
HOST-004 Equipment Profile
    ↓
HOST-005 HSMS 상태 머신
    ↓
HOST-006 SECS-II 메시지 계층
    ↓
HOST-007 GEM 기본 상태
    ├──────────────┐
    ↓              ↓
HOST-008 메시지    HOST-010 UI 초안
카탈로그           │
    ↓              │
HOST-009 Scenario Runner
    ↓              │
HOST-010 UI 완성 ◀─┘
    ↓
HOST-011 안정화 및 배포
```

## 4. M1 기반 구축

### HOST-001 요구사항 및 설비 사양 정리

상태: `Backlog`

목표:

- 개발 범위와 대상 설비의 통신 조건을 확정한다.

작업:

- Host Simulator의 Active/Passive 역할 확인
- Equipment IP와 Port 확인
- Device ID 확인
- 대상 Windows 버전 확인
- x86/x64 확인
- 필수 Stream/Function 선정
- CEID, RPTID, VID, ALID, RCMD 목록 작성
- Host Initiated/Equipment Initiated 구분
- Primary/Secondary 응답 관계 작성
- 정상, 오류 및 Timeout Scenario 작성
- MVP 제외 범위 확정

산출물:

- 요구사항 문서
- 메시지 카탈로그 초안
- 상태 전이 초안
- 첫 번째 시험 Scenario
- 미확정 항목 목록

완료 조건:

- [ ] Interface Specification을 확보했다.
- [ ] Host와 Equipment의 역할이 확정됐다.
- [ ] 첫 번째 시험 Scenario가 선정됐다.
- [ ] 필수 메시지 목록이 작성됐다.
- [ ] 미확정 항목과 담당자가 표시됐다.
- [ ] 구현을 시작해도 되는 범위가 승인됐다.

### HOST-002 개발 환경 및 Solution 골격

선행조건: `HOST-001`

목표:

- 모든 프로젝트가 빌드되고 테스트를 실행할 수 있는 기본 구조를 만든다.

프로젝트:

```text
HostSimulator.sln
├─ HostSimulator.App
├─ HostSimulator.Application
├─ HostSimulator.Protocol
├─ HostSimulator.Domain
├─ HostSimulator.Infrastructure
└─ HostSimulator.Tests
```

작업:

- WPF Solution 생성
- 모든 프로젝트를 .NET Framework 4.7.2로 설정
- x86 또는 x64 Platform Target 설정
- 프로젝트 참조 방향 구성
- 기본 설정 로딩 구성
- 기본 로그 구성
- xUnit 테스트 프로젝트 구성

참조 원칙:

```text
App → Application → Domain
Protocol → Domain
Infrastructure → Application/Domain의 인터페이스 구현
Tests → 시험 대상 프로젝트
```

완료 조건:

- [ ] 모든 프로젝트의 Target Framework가 4.7.2다.
- [ ] Debug 빌드가 성공한다.
- [ ] Release 빌드가 성공한다.
- [ ] 기본 자동 테스트가 실행된다.
- [ ] UI가 Protocol 구현을 직접 호출하지 않는다.
- [ ] 빌드 방법이 README에 기록됐다.

### HOST-003 통신 SDK 기술 검증

선행조건: `HOST-002`

목표:

- 선택한 SDK가 .NET Framework 4.7.2와 대상 PC에서 안정적으로 동작하는지 확인한다.

작업:

- Secs4Net 2.4.4 또는 상용 SDK 설치
- 패키지 버전 고정
- 최소 연결 프로그램 작성
- x86/x64 호환성 확인
- 반복 연결 및 해제
- 예외, 재접속 및 메모리 동작 확인
- SDK 라이선스와 배포 조건 확인

완료 조건:

- [ ] .NET Framework 4.7.2에서 빌드 및 실행된다.
- [ ] Equipment Simulator와 TCP 연결된다.
- [ ] 최소 100회 연결/해제를 수행해 비정상 종료가 없다.
- [ ] x86/x64 조건이 확정됐다.
- [ ] SDK 버전이 자동으로 변경되지 않는다.
- [ ] SDK의 제약과 교체 조건이 기록됐다.

판정:

- 성공: M2로 이동한다.
- 실패: 제품 기능을 구현하기 전에 SDK를 교체하고 HOST-003을 다시 수행한다.

## 5. M2 기본 통신

### HOST-004 Equipment Profile 관리

선행조건: `HOST-003`

목표:

- 설비별 접속 정보를 코드 수정 없이 관리한다.

설정 항목:

```text
Profile Name
IP Address
Port
Active/Passive
Device ID
T3/T5/T6/T7/T8
Auto Reconnect
Reconnect Interval
```

완료 조건:

- [ ] JSON Profile을 읽고 저장할 수 있다.
- [ ] 필수 설정 누락을 시작 전에 검출한다.
- [ ] IP, Port 및 Timer 범위를 검증한다.
- [ ] Profile별 설정이 분리된다.
- [ ] 민감한 정보가 로그에 기록되지 않는다.

### HOST-005 HSMS 상태 머신

선행조건: `HOST-004`

목표:

- 연결, Select, Linktest, 연결 단절 및 재접속을 안정적으로 처리한다.

상태:

```text
Disconnected
→ Connecting
→ Connected
→ Selected
→ Disconnecting
→ Disconnected
```

작업:

- Connect/Disconnect
- Select/Deselect
- Linktest
- Separate
- T5/T6/T7/T8 처리
- 연결 단절 감지
- 재접속
- 상태 변경 이벤트

완료 조건:

- [ ] Connect와 Select가 성공한다.
- [ ] Linktest 요청과 응답을 확인할 수 있다.
- [ ] Select 실패와 Timeout을 구분한다.
- [ ] 연결 단절을 감지한다.
- [ ] 반복 재접속이 가능하다.
- [ ] 통신 오류로 프로그램이 종료되지 않는다.
- [ ] 모든 상태 변경 이유가 로그에 기록된다.

### HOST-006 SECS-II 메시지 계층

선행조건: `HOST-005`

목표:

- Primary/Secondary 메시지를 안전하게 송수신하고 연결한다.

작업:

- 메시지 생성
- Encode/Decode
- W-Bit
- System Byte 생성 및 추적
- Primary/Secondary 연결
- T3 Timeout
- SxF0와 S9Fx 처리
- 예상하지 않은 메시지 처리
- Raw Data 기록

완료 조건:

- [ ] Primary Message를 전송할 수 있다.
- [ ] Secondary Message를 요청과 연결할 수 있다.
- [ ] System Byte가 정확히 처리된다.
- [ ] T3 Timeout을 검출한다.
- [ ] 잘못된 Data Type을 안전하게 처리한다.
- [ ] 예상하지 않은 메시지를 별도 기록한다.
- [ ] 송수신 Raw Data와 해석 결과가 일치한다.

### HOST-007 GEM 기본 상태

선행조건: `HOST-006`

목표:

- Communication과 Online/Offline 상태를 관리한다.

작업:

- Establish Communication
- Online Request/Response
- Offline 처리
- 상태 전이 검증
- 상태별 명령 허용 정책
- 상태 변경 로그

완료 조건:

- [ ] Communication 상태를 확립할 수 있다.
- [ ] Online/Offline 상태를 구분한다.
- [ ] 잘못된 상태의 명령을 차단한다.
- [ ] 연결 해제 후 GEM 상태가 초기화된다.
- [ ] 재접속 후 Communication을 다시 확립할 수 있다.

M2 통합 Gate:

```text
Connect
→ Select
→ Establish Communication
→ Online
→ 단일 SECS-II 메시지 왕복
→ Disconnect
```

- [ ] 전체 흐름을 연속 20회 실행해 성공한다.
- [ ] 실패 시 어느 단계에서 실패했는지 로그로 식별할 수 있다.

## 6. M3 자동 Scenario

### HOST-008 설비 메시지 카탈로그

선행조건: `HOST-001`, `HOST-006`

목표:

- 설비별 메시지 정의와 자동 응답 규칙을 코드에서 분리한다.

우선 대상:

- S1 Communication 및 상태
- S2F41/S2F42 Remote Command
- S5F1/S5F2 Alarm Report
- S6F11/S6F12 Event Report
- 대상 설비에 필요한 Recipe 메시지

완료 조건:

- [ ] 메시지 구조가 Interface Specification과 일치한다.
- [ ] CEID와 Report Data를 해석할 수 있다.
- [ ] RCMD와 Parameter를 검증할 수 있다.
- [ ] 자동 응답 여부를 설정할 수 있다.
- [ ] 알 수 없는 메시지를 안전하게 기록한다.
- [ ] 메시지별 정상 및 오류 테스트가 존재한다.

### HOST-009 Scenario Runner

선행조건: `HOST-005`, `HOST-006`, `HOST-008`

목표:

- 수동 시험을 재현 가능한 자동 시험으로 전환한다.

초기 명령:

```text
connect
disconnect
send
wait
reply
assert
delay
```

대표 Scenario:

```text
Connect
→ Establish Communication
→ Online
→ Send S2F41
→ Wait S2F42 within T3
→ Assert HCACK
→ Wait S6F11 where CEID == 1320
→ Reply S6F12
→ PASS/FAIL
```

완료 조건:

- [ ] YAML 또는 JSON Scenario를 읽는다.
- [ ] 각 단계의 Timeout을 처리한다.
- [ ] 메시지 Field를 Assert할 수 있다.
- [ ] 실행 중 안전하게 취소할 수 있다.
- [ ] 실패한 단계와 원인을 표시한다.
- [ ] 단계별 시작, 종료 및 소요 시간을 기록한다.
- [ ] 전체 결과를 파일로 저장할 수 있다.

### HOST-010 WPF 사용자 화면

선행조건: UI 초안은 `HOST-002` 이후 가능, 기능 완성은 `HOST-009` 이후

목표:

- 사용자가 통신과 Scenario를 편리하게 조작하고 결과를 확인할 수 있게 한다.

화면:

- Equipment Profile
- 연결 및 GEM 상태
- 메시지 송수신 모니터
- 수동 메시지 전송
- Scenario 선택 및 실행
- 단계별 결과
- 로그 검색 및 저장

완료 조건:

- [ ] UI가 통신 스레드를 차단하지 않는다.
- [ ] 상태가 실시간으로 갱신된다.
- [ ] Connect/Disconnect를 조작할 수 있다.
- [ ] 수동 메시지를 전송할 수 있다.
- [ ] Scenario 실행과 취소가 가능하다.
- [ ] 대량 로그에서도 화면이 멈추지 않는다.
- [ ] ViewModel에 SDK 세부 구현이 들어가지 않는다.

## 7. M4 안정화 및 배포

### HOST-011 비정상 시험, 보고서 및 배포

선행조건: `HOST-009`, `HOST-010`

목표:

- 장시간 현장 시험과 다른 PC 배포가 가능한 상태로 만든다.

비정상 시험:

- Secondary Message 미응답
- T3 Timeout
- 잘못된 System Byte
- 예상하지 않은 Stream/Function
- 잘못된 Data Type
- 중복 메시지
- 지연 응답
- TCP 강제 단절
- 반복 재접속
- Scenario 실행 중 종료
- 로그 저장 공간 부족
- 장시간 통신

배포 작업:

- .NET Framework 4.7.2 설치 여부 검사
- x86/x64 확인
- 기본 설정 제공
- 로그 보관 기간과 최대 크기 설정
- 실행 파일 버전 표시
- 설치 및 실행 설명서
- 실제 설비 시험 체크리스트
- 알려진 제약사항 문서화

완료 조건:

- [ ] 자동 회귀 테스트가 통과한다.
- [ ] 장시간 통신 시험 기준을 통과한다.
- [ ] 재접속 후 상태를 복구한다.
- [ ] 로그 파일 크기와 보관 기간이 제한된다.
- [ ] Equipment Simulator 시험 결과가 저장된다.
- [ ] 실제 설비 시험 결과가 기록된다.
- [ ] Release 패키지를 깨끗한 시험 PC에서 실행할 수 있다.
- [ ] 미해결 문제와 잔여 위험이 승인됐다.

## 8. 병렬 작업 기준

다음 단계는 반드시 순차 진행한다.

```text
HOST-001 → HOST-002 → HOST-003
HOST-004 → HOST-005 → HOST-006 → HOST-007
```

SDK와 통신 인터페이스가 안정된 후 다음 작업은 일부 병렬화할 수 있다.

- 통신 에이전트: HOST-004~007
- 사양 에이전트: HOST-008
- UI 에이전트: HOST-010 화면 초안
- 검증 에이전트: 각 단계 완료 조건 검토

같은 파일, 공용 인터페이스 또는 동일한 상태 머신을 두 에이전트가 동시에 수정하지 않는다.

## 9. Issue 운영 규칙

각 HOST 작업은 GitHub Issue 하나로 관리한다.

권장 상태:

```text
Backlog
→ Analysis
→ Ready
→ In Progress
→ Review
→ Equipment Test
→ Done
```

Issue에는 다음 내용을 포함한다.

- 목적
- 현재 동작
- 기대 동작
- 적용 범위
- 제외 범위
- 입력과 예상 결과
- 기술적 제약
- 완료 조건
- 에이전트 제출물

작성 양식은 [에이전트 작업 요청서 템플릿](./AGENT_WORK_REQUEST_TEMPLATE.md)을 사용한다.

## 10. 에이전트 작업 원칙

1. 작업 하나당 구현 에이전트 한 명을 기본으로 한다.
2. 구현 에이전트는 별도 branch/worktree를 사용한다.
3. 구현 에이전트와 최종 검증 에이전트를 분리한다.
4. 완료 보고에는 변경 파일, 빌드, 테스트, 미확인 사항 및 잔여 위험을 포함한다.
5. 완료 조건을 충족하지 못하면 Done으로 이동하지 않는다.
6. 병렬 구현 결과는 한 번에 합치지 않고 하나씩 통합·재검증한다.
7. 실제 설비 제어 시험은 사람의 승인 후 수행한다.

## 11. 현재 다음 작업

첫 번째 실행 대상은 `HOST-001 요구사항 및 설비 사양 정리`다.

HOST-001이 완료되면 다음 순서로 진행한다.

```text
HOST-002 Solution 골격
→ HOST-003 통신 SDK PoC
→ M1 완료 검토
```


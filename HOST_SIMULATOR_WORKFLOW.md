# Semiconductor Equipment Host Simulator 개발 워크플로우

## 1. 문서 목적

이 문서는 반도체 설비의 HSMS/SECS-II/GEM 통신을 시험하기 위한 Host Simulator의 요구사항, 기술 구성, 개발 단계, 멀티에이전트 협업 방식 및 완료 조건을 정의한다.

Host Simulator가 Host 역할로 실제 Equipment 또는 Equipment Simulator에 접속하는 것을 전제로 한다. 정확한 Stream/Function, CEID, RPTID, VID, ALID, RCMD 및 Data Item 구조는 SEMI 표준과 대상 설비의 Interface Specification을 우선한다.

## 2. 권장 기술 구성

- 언어: C#
- 런타임: .NET Framework 4.7.2
- UI: WPF
- 설계 패턴: MVVM
- HSMS/SECS/GEM: Secs4Net 2.4.4 버전 고정 또는 .NET Framework 4.7.2 호환 상용 SDK
- Equipment Profile: JSON
- 시험 Scenario: YAML 또는 JSON
- 로그: 구조화 파일, 필요 시 SQLite
- 테스트: xUnit

새 Host Simulator는 기존 C++Builder 설비 프로그램과 분리된 독립 실행 프로그램으로 개발하는 것을 기본 방향으로 한다.

```text
기존 BCB Equipment 프로그램
          ⇅ HSMS
새로운 C# Host Simulator
```

기존 BCB `.cpp`, `.h`, `.dfm`을 수정하는 경우 CP949/Windows-949 인코딩을 유지한다. 새 C# 프로젝트와 Markdown 문서는 UTF-8로 관리한다.

최신 Secs4Net 3.x는 .NET 8/10 대상이므로 .NET Framework 4.7.2 프로젝트에서 직접 사용할 수 없다. Secs4Net을 선택하면 `netstandard2.0`을 제공하는 2.4.4를 버전 고정하여 기술 검증한다. NuGet 자동 업데이트는 사용하지 않으며, 장시간 통신, 재접속, 메모리 사용량과 배포 PC 호환성을 별도로 검증한다.

.NET Framework 4.7.2는 설치된 Windows 운영체제의 구성 요소 수명주기를 따른다. 배포 대상 OS와 4.7.2 Developer Pack/Runtime 설치 여부를 사전에 확인한다.

## 3. MVP 목표

첫 번째 버전은 다음 기능까지 구현한다.

1. Equipment Profile 등록 및 선택
2. HSMS TCP 연결과 Select 처리
3. Communication 및 Online 상태 관리
4. SECS-II 메시지 수동 송수신
5. Primary Message 수신과 Secondary Message 자동 응답
6. 자동 시험 Scenario 실행
7. 단계별 Pass/Fail 판정
8. Raw Data와 해석된 메시지 로그 저장
9. Timeout, 연결 해제 및 재접속 시험

초기 MVP에서 제외하는 항목은 다음과 같다.

- 전체 MES 기능
- 생산 데이터베이스 연동
- 모든 GEM300 기능
- 실제 생산 제어
- Recipe 파일 자동 변경
- 복잡한 범용 스크립트 언어

## 4. 대표 운용 흐름

```text
사용자
  │ Equipment Profile 선택 및 Scenario 실행
  ▼
Host Simulator
  │ HSMS TCP Connect
  ▼
Equipment
  │ Select Request / Response
  │ Establish Communication
  │ Online Request / Response
  │ Remote Command 및 Ack
  │ Event Report 및 Ack
  │ Alarm Report 및 Ack
  ▼
Host Simulator
  │ 메시지 검증, Timeout 검사, Pass/Fail 판정
  ▼
사용자
  └─ 결과와 송수신 로그 확인
```

대표 메시지 흐름 예시는 다음과 같다. 실제 방향과 Data Item은 설비 사양서를 따른다.

```text
1. HSMS Connect
2. Select Request / Select Response
3. Establish Communication
4. Online Request / Online Response
5. Host → Equipment: S2F41 Remote Command
6. Equipment → Host: S2F42 Command Ack
7. Equipment → Host: S6F11 Event Report
8. Host → Equipment: S6F12 Event Ack
9. Equipment → Host: S5F1 Alarm Report
10. Host → Equipment: S5F2 Alarm Ack
```

## 5. 요구사항

### 5.1 목적

실제 MES에 연결하기 전에 설비의 HSMS/SECS/GEM 통신과 주요 운전 Scenario를 개발 PC에서 반복 검증할 수 있어야 한다.

### 5.2 기대 동작

1. Equipment IP, Port, Active/Passive, Device ID 및 통신 Timer를 설정할 수 있다.
2. Disconnected, Connected, Selected, Communicating, Online 상태를 표시한다.
3. SECS-II Primary Message를 선택하거나 작성하여 전송할 수 있다.
4. 수신한 Primary Message에 정의된 Secondary Message를 자동 응답할 수 있다.
5. Send, Wait, Reply, Assert, Delay 단계로 구성된 Scenario를 실행할 수 있다.
6. 각 단계의 시작 시간, 종료 시간, 소요 시간 및 성공 여부를 기록한다.
7. 송수신 방향, Stream/Function, W-Bit, System Byte, Body Data 및 Raw Data를 기록한다.
8. 설정된 시간 안에 기대 메시지가 도착하지 않으면 해당 단계를 실패 처리한다.
9. 연결이 끊어지면 상태를 갱신하고 수동 또는 자동 재접속할 수 있다.

### 5.3 설비별 정의 항목

구현 전에 다음 목록을 작성한다.

- 지원할 Stream/Function 목록
- CEID, RPTID, VID 목록
- ALID 및 Alarm 목록
- RCMD와 Parameter 목록
- Host Initiated / Equipment Initiated 구분
- Primary/Secondary 응답 관계
- 각 메시지의 W-Bit 조건
- Timeout 및 재시도 정책
- Online/Offline 상태 전이 조건
- 설비별 예외 동작

## 6. 대표 Scenario

LoadReq CEID가 `1320`인 설비의 예시는 다음과 같다.

```yaml
name: Load Request Test
equipmentProfile: Aspen3F

steps:
  - action: connect
  - action: establishCommunication
  - action: requestOnline
  - action: wait
    message: S6F11
    timeoutMs: 10000
    where:
      CEID: 1320
  - action: reply
    message: S6F12
  - action: assert
    path: CEID
    equals: 1320
```

초기 Scenario Runner는 다음 명령만 지원한다.

- `connect`
- `disconnect`
- `send`
- `wait`
- `reply`
- `assert`
- `delay`

조건 분기, 반복문 및 사용자 정의 함수는 실제 요구가 확인된 후 추가한다.

## 7. 권장 프로그램 구조

```text
HostSimulator.sln
├─ HostSimulator.App
│  ├─ WPF 화면
│  ├─ ViewModel
│  └─ 사용자 명령
├─ HostSimulator.Application
│  ├─ 연결 관리
│  ├─ Scenario 실행
│  ├─ Pass/Fail 판정
│  └─ Equipment Profile 관리
├─ HostSimulator.Protocol
│  ├─ HSMS 연결
│  ├─ SECS-II Encode/Decode
│  ├─ Primary/Secondary 관리
│  ├─ System Byte 관리
│  └─ Timeout 관리
├─ HostSimulator.Domain
│  ├─ Equipment 상태
│  ├─ GEM 상태
│  ├─ Message Definition
│  └─ Scenario Model
├─ HostSimulator.Infrastructure
│  ├─ 로그 저장
│  ├─ JSON/YAML 설정
│  ├─ SQLite 선택 적용
│  └─ 결과 보고서
└─ HostSimulator.Tests
   ├─ 메시지 Encode/Decode 테스트
   ├─ Scenario 테스트
   ├─ Timeout 테스트
   └─ 가상 Equipment 테스트
```

UI에서 TCP 또는 SECS/GEM SDK를 직접 호출하지 않는다.

```text
WPF UI
→ Application Service
→ Scenario Runner
→ Protocol Adapter
→ Secs4Net 2.4.4 또는 4.7.2 호환 상용 SDK
→ Equipment
```

통신 SDK는 내부 인터페이스 뒤에 감싸 UI와 Scenario 계층에 SDK 전용 타입이 퍼지지 않도록 한다.

모든 프로젝트의 Target Framework는 `.NET Framework 4.7.2`로 통일하고, 사용하는 통신 SDK와 배포 PC 환경에 맞춰 x86/x64 Platform Target을 명시한다.

## 8. 멀티에이전트 역할

### 8.1 사람

- 목적과 우선순위 결정
- 대상 설비와 사양서 제공
- 실제 장비 시험 승인
- 주요 설계 결정 및 최종 배포 승인

### 8.2 리드/분석 에이전트

- 요구사항과 제외 범위 정리
- Interface Specification 분석
- 메시지 카탈로그 작성
- 상태 전이와 자동 응답 규칙 정의
- 작업을 모듈 단위로 분해
- 에이전트별 소유 영역 지정
- 통합 순서 결정

### 8.3 통신 구현 에이전트

- TCP 및 HSMS 연결
- Select/Deselect 및 Linktest
- SECS-II Encode/Decode
- System Byte 및 Timeout 처리
- Primary/Secondary 상관관계 관리

화면 디자인, Scenario 판정 정책 및 설비별 CEID 정의는 수정하지 않는다.

### 8.4 Scenario 구현 에이전트

- Send, Wait, Reply, Assert, Delay
- 단계별 Timeout
- Pass/Fail 판정
- 실행 취소
- 결과 보고

통신 계층의 내부 구현을 직접 수정하지 않고 공개 인터페이스만 사용한다.

### 8.5 UI 구현 에이전트

- Equipment Profile 화면
- 연결 및 GEM 상태 표시
- 메시지 송수신 모니터
- 수동 메시지 전송
- Scenario 선택 및 실행
- 결과와 로그 표시

### 8.6 검증 에이전트

검증 에이전트는 원칙적으로 코드를 직접 수정하지 않고 다음 항목을 보고한다.

- 요구사항 누락
- 상태 전이 오류
- Primary/Secondary 응답 오류
- W-Bit 및 System Byte 불일치
- Timeout 누락
- 중복 메시지 처리 문제
- 연결 해제 및 재접속 문제
- 메모리 및 스레드 안전성
- 로그 누락과 기존 기능 회귀
- BCB 파일 수정 시 CP949 손상 여부

## 9. 브랜치와 작업 공간

각 구현 에이전트는 별도 Git branch와 worktree를 사용한다.

```text
main
├─ codex/host-hsms-protocol
├─ codex/host-scenario-runner
├─ codex/host-ui
└─ codex/host-tests
```

두 에이전트가 같은 파일이나 같은 함수를 동시에 수정하지 않도록 한다. 공용 인터페이스 변경은 리드 에이전트가 먼저 확정한다.

소스뿐 아니라 다음 자원도 에이전트별로 분리한다.

- 빌드 출력 폴더
- 임시 파일
- TCP Port
- 로그 파일
- 테스트 설정
- Equipment Simulator 인스턴스

## 10. 단계별 개발 계획

### 단계 0: 사양 확정

- Host/Equipment 역할 확정
- Active/Passive 설정 확정
- 메시지 카탈로그 작성
- 상태 전이 작성
- 첫 번째 자동 Scenario 선정
- 성공 및 실패 조건 작성

이 단계에서는 제품 코드를 수정하지 않는다.

### 단계 1: 최소 HSMS 연결

```text
Connect → Select → Linktest → Disconnect
```

완료 조건:

- 반복 연결 및 해제가 가능하다.
- 연결 상태가 정확히 표시된다.
- Select 실패와 Timeout이 구분된다.
- 로그에서 제어 메시지를 확인할 수 있다.

### 단계 2: 단일 SECS-II 왕복

```text
Primary 전송 → Secondary 수신 → System Byte 비교 → 내용 검사
```

완료 조건:

- 정상 응답을 요청 메시지와 연결할 수 있다.
- T3 Timeout을 검출한다.
- 예상하지 않은 응답을 별도로 기록한다.

### 단계 3: GEM 기본 흐름

- Establish Communication
- Online/Offline
- Event Report
- Alarm Report
- Remote Command

### 단계 4: Scenario Runner

```text
Send S2F41
→ Wait S2F42 within T3
→ Assert HCACK
→ Wait S6F11 where CEID == 1320
→ Reply S6F12
→ PASS/FAIL
```

### 단계 5: 비정상 시험

- Secondary Message 미응답
- 잘못된 System Byte
- 예상하지 않은 Stream/Function
- TCP 연결 중단
- 중복 메시지와 지연 응답
- 잘못된 SECS-II Data Type
- Online 상태가 아닌 상태에서 명령 전송
- Scenario 실행 중 사용자 취소

### 단계 6: 순차 통합

```text
HSMS
→ SECS-II
→ GEM 상태
→ Scenario Runner
→ UI
→ 전체 검증
```

각 브랜치를 하나씩 통합하고 매번 전체 빌드와 회귀 테스트를 수행한다.

## 11. 완료 조건

- [ ] 지정한 IP와 Port로 HSMS 연결이 성공한다.
- [ ] Active/Passive 설정이 적용된다.
- [ ] Select 및 Communication 상태를 확인할 수 있다.
- [ ] Online/Offline 상태 전이가 사양과 일치한다.
- [ ] 수동으로 SECS-II Primary Message를 전송할 수 있다.
- [ ] Secondary Message를 수신하고 요청과 연결할 수 있다.
- [ ] 정의된 Primary Message에 자동 응답할 수 있다.
- [ ] S2F41 전송 후 S2F42의 HCACK을 검사할 수 있다.
- [ ] S6F11 수신 후 S6F12를 자동 전송할 수 있다.
- [ ] 지정 CEID와 Report Data를 검사할 수 있다.
- [ ] 설정된 Timeout 발생 시 해당 단계가 실패 처리된다.
- [ ] 연결 단절 후 상태가 갱신되고 재접속할 수 있다.
- [ ] 송수신 방향, 시간, Stream/Function, W-Bit, System Byte 및 Body가 기록된다.
- [ ] Scenario별 Pass/Fail 결과와 실패 원인을 저장할 수 있다.
- [ ] 자동 테스트와 Release 빌드가 성공한다.
- [ ] 기존 BCB 소스를 수정했다면 CP949 인코딩이 유지된다.
- [ ] 검증하지 못한 사항과 잔여 위험이 보고된다.

## 12. 작업 요청 및 완료 보고

각 개발 작업은 목적, 현재 동작, 기대 동작, 적용 범위, 제외 범위, 입력과 예상 결과, 기술적 제약 및 완료 조건을 먼저 작성한 후 시작한다.

복사해서 사용할 수 있는 작업 요청서와 완료 보고 양식은 [에이전트 작업 요청서 템플릿](./AGENT_WORK_REQUEST_TEMPLATE.md)을 사용한다.

각 에이전트는 다음 내용을 제출한다.

1. 변경한 파일 목록
2. 변경 목적과 주요 설계 결정
3. 실행한 빌드 및 테스트
4. 테스트 결과
5. 확인하지 못한 사항
6. 남아 있는 위험
7. 다른 작업과의 의존성
8. 통합 시 주의사항
9. 커밋 해시

단순한 `완료했습니다` 보고만으로는 통합하지 않는다.

## 13. 핵심 운영 원칙

1. 화면보다 통신 상태 머신을 먼저 안정화한다.
2. 수동 시험보다 재현 가능한 자동 Scenario를 우선한다.
3. 파일이 아니라 모듈과 책임 단위로 작업을 분리한다.
4. 구현자와 검증자를 분리한다.
5. 병렬 구현 후 통합은 순차적으로 수행한다.
6. 실제 설비 시험 전 Equipment Simulator와 자동 테스트로 검증한다.
7. 사양서에 없는 동작을 에이전트가 임의로 결정하지 않게 한다.
8. 모든 성공 판정에는 로그 또는 테스트 결과와 같은 증거를 남긴다.

## 14. 참고 링크

- .NET Framework 4.7.2 및 OS 호환성: https://learn.microsoft.com/dotnet/framework/install/versions-and-dependencies
- WPF 문서: https://learn.microsoft.com/dotnet/desktop/wpf/
- Secs4Net: https://github.com/mkjeff/secs4net
- Secs4Net 2.4.4: https://www.nuget.org/packages/Secs4Net/2.4.4


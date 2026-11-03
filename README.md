# ATIHOST

반도체 설비의 HSMS/SECS-II/GEM 통신을 개발 PC에서 검증하기 위한 Host Simulator 프로젝트이다.

실제 MES에 연결하기 전에 Equipment 또는 Equipment Simulator를 대상으로 통신 연결, 메시지 송수신, 자동 응답, Scenario 실행 및 Pass/Fail 판정을 반복 수행하는 것을 목표로 한다.

## 주요 목표

- Equipment Profile별 HSMS 연결 설정
- HSMS Connect, Select, Linktest 및 재접속
- SECS-II Primary/Secondary 메시지 송수신
- Communication 및 Online/Offline 상태 관리
- S2F41/S2F42 Remote Command 검증
- S6F11/S6F12 Event Report 검증
- S5F1/S5F2 Alarm Report 검증
- 자동 Scenario 실행 및 단계별 Pass/Fail 판정
- Raw Data와 해석된 메시지 로그 저장
- Timeout, 연결 중단 및 비정상 메시지 시험

정확한 Stream/Function, CEID, RPTID, VID, ALID, RCMD 및 Data Item 구조는 SEMI 표준과 대상 설비의 Interface Specification을 우선한다.

## 권장 기술 스택

- Language: C#
- Runtime: .NET Framework 4.7.2
- UI: WPF
- Pattern: MVVM
- HSMS/SECS/GEM: Secs4Net 2.4.4 버전 고정 또는 .NET Framework 4.7.2 호환 상용 SDK
- Equipment Profile: JSON
- Test Scenario: YAML 또는 JSON
- Test: xUnit
- Log: 구조화 파일, 필요 시 SQLite

새 Host Simulator는 기존 C++Builder 설비 프로그램과 분리된 독립 프로그램으로 개발하는 것을 기본 방향으로 한다.

최신 Secs4Net 3.x는 .NET 8/10 대상이므로 이 프로젝트에서 직접 사용할 수 없다. Secs4Net을 선택하면 `netstandard2.0`을 제공하는 2.4.4를 고정하여 호환성과 장시간 통신 안정성을 검증한다. 신규 버전으로 자동 업데이트하지 않는다.

```text
기존 BCB Equipment 프로그램
          ⇅ HSMS
새로운 C# Host Simulator
```

## MVP 범위

첫 번째 버전은 다음 수직 흐름을 완성한다.

```text
HSMS Connect
→ Select
→ Establish Communication
→ Online
→ S2F41 전송 및 S2F42 확인
→ S6F11 수신 및 S6F12 응답
→ 로그 저장
→ Scenario Pass/Fail 판정
```

초기 버전에는 전체 MES, 생산 DB, 모든 GEM300 기능, 실제 생산 제어 및 복잡한 범용 스크립트 언어를 포함하지 않는다.

## 권장 프로젝트 구조

```text
HostSimulator.sln
├─ HostSimulator.App              WPF UI와 ViewModel
├─ HostSimulator.Application      연결 및 Scenario 실행
├─ HostSimulator.Protocol         HSMS/SECS-II 통신
├─ HostSimulator.Domain           상태와 메시지 모델
├─ HostSimulator.Infrastructure   설정, 로그 및 보고서
└─ HostSimulator.Tests            자동 테스트
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

## 개발 워크플로우

1. 설비 Interface Specification을 분석한다.
2. 요구사항, 제외 범위 및 완료 조건을 확정한다.
3. 메시지 카탈로그와 상태 전이를 작성한다.
4. 작업을 통신, Scenario, UI 및 검증 영역으로 분리한다.
5. 각 구현 작업은 별도 branch와 worktree에서 수행한다.
6. 구현 에이전트는 변경 파일과 테스트 결과를 제출한다.
7. 별도의 검증 에이전트 또는 사람이 변경 내용을 검토한다.
8. HSMS, SECS-II, GEM 상태, Scenario Runner, UI 순서로 통합한다.
9. Equipment Simulator에서 자동 시험한다.
10. 승인 후 실제 설비에서 제한된 조건으로 최종 검증한다.

권장 작업 상태는 다음과 같다.

```text
Backlog
→ Analysis
→ Ready
→ In Progress
→ Review
→ Equipment Test
→ Done
```

## 기본 개발 규칙

- 작업 하나당 요구사항과 완료 조건을 하나씩 작성한다.
- 같은 파일이나 같은 함수를 여러 에이전트가 동시에 수정하지 않는다.
- 구현자와 최종 검증자를 분리한다.
- 사양서에 없는 동작을 임의로 결정하지 않는다.
- 성공 판정에는 빌드, 테스트 또는 통신 로그와 같은 증거를 남긴다.
- 통신 SDK는 Adapter 뒤에 격리한다.
- 모든 프로젝트의 Target Framework를 `.NET Framework 4.7.2`로 통일한다.
- NuGet 패키지의 .NET Framework 4.7.2 호환성을 설치 전에 확인한다.
- 통신 SDK와 대상 PC에 맞춰 x86/x64 Platform Target을 명시한다.
- 실제 장비 시험 전 Equipment Simulator로 검증한다.
- 실제 생산 제어 기능은 별도 승인 없이 실행하지 않는다.
- 기존 BCB `.cpp`, `.h`, `.dfm`을 수정할 때는 CP949/Windows-949 인코딩을 유지한다.

## 문서

- [Host Simulator 개발 워크플로우](./HOST_SIMULATOR_WORKFLOW.md)
- [단계별 개발 계획](./DEVELOPMENT_STEPS.md)
- [에이전트 작업 요청서 템플릿](./AGENT_WORK_REQUEST_TEMPLATE.md)
- [GitHub Issue 및 Pull Request 템플릿](./ISSUE_PR_TEMPLATES.md)

## 작업 시작 체크리스트

- [ ] 대상 설비와 Host/Equipment 역할이 확정되었다.
- [ ] Interface Specification을 확보했다.
- [ ] Active/Passive, IP, Port 및 Device ID를 확인했다.
- [ ] 필수 Stream/Function과 메시지 구조를 정리했다.
- [ ] 첫 번째 자동 Scenario를 선정했다.
- [ ] 정상, 오류 및 Timeout 완료 조건을 작성했다.
- [ ] 실제 설비 없이 실행할 Equipment Simulator 환경을 준비했다.
- [ ] 로그에 민감한 생산 정보가 포함되지 않는지 확인했다.

## 빌드 및 테스트

### 사전 조건

- .NET SDK 6.0 이상 (SDK-style csproj로 net472 대상 빌드)
- .NET Framework 4.7.2 Developer Pack 또는 Targeting Pack (Visual Studio Build Tools에 포함되거나 별도 설치)

### NuGet 패키지 복원

```bash
dotnet restore HostSimulator.sln
```

### Debug 빌드

```bash
dotnet build HostSimulator.sln -c Debug -p:Platform=x64 --no-restore -m:1
```

### Release 빌드

```bash
dotnet build HostSimulator.sln -c Release -p:Platform=x64 --no-restore -m:1
```

### 테스트 실행

```bash
dotnet test HostSimulator.Tests/HostSimulator.Tests.csproj -c Debug -p:Platform=x64 --no-build
dotnet test HostSimulator.Tests/HostSimulator.Tests.csproj -c Release -p:Platform=x64 --no-build
```

### 프로젝트 참조 구조

```text
App → Application → Domain
App → Infrastructure → Application, Domain
Protocol → Domain
Tests → Application, Domain, Infrastructure, Protocol
```

App은 Protocol을 직접 참조하지 않는다. 모든 프로젝트의 Platform Target은 x64이다.

## 참고

- [.NET Framework 4.7.2 버전 및 OS 호환성](https://learn.microsoft.com/dotnet/framework/install/versions-and-dependencies)
- [WPF 문서](https://learn.microsoft.com/dotnet/desktop/wpf/)
- [Secs4Net](https://github.com/mkjeff/secs4net)
- [Secs4Net 2.4.4 NuGet](https://www.nuget.org/packages/Secs4Net/2.4.4)


# GitHub Issue 및 Pull Request 템플릿

## 1. 목적

이 문서는 ATIHOST 개발 작업의 GitHub Issue와 Pull Request를 일관된 형식으로 작성하기 위한 기준과 복사 템플릿을 제공한다.

- Issue는 `왜 필요한지`, `무엇을 할지`, `어디까지 하면 완료인지`를 정의한다.
- Pull Request는 `무엇을 변경했는지`, `어떻게 검증했는지`, `무엇이 아직 위험한지`를 보고한다.
- `HOST-001`부터 `HOST-011`까지 작업 하나당 Issue 하나를 기본으로 한다.
- Issue 하나당 PR 하나를 기본으로 한다.
- 서로 독립된 개발 단계를 하나의 큰 PR로 합치지 않는다.

## 2. 이름 규칙

### Issue 제목

```text
[HOST-번호] 작업 내용을 한 문장으로 작성
```

예시:

```text
[HOST-001] 요구사항 및 설비 사양 정리
[HOST-005] HSMS 상태 머신 구현
[HOST-009] Scenario Runner 구현
```

### Pull Request 제목

```text
[HOST-번호] 실제 변경 내용을 한 문장으로 작성
```

예시:

```text
[HOST-001] 설비 통신 요구사항 문서화
[HOST-005] HSMS 연결 및 재접속 상태 머신 구현
[HOST-009] YAML 기반 Scenario Runner 구현
```

### Branch 이름

```text
codex/host-번호-짧은-작업명
```

예시:

```text
codex/host-001-requirements
codex/host-005-hsms-state
codex/host-009-scenario-runner
```

## 3. 작업 흐름

```text
Issue 생성
→ 요구사항과 완료 조건 검토
→ Ready 전환
→ Branch/Worktree 생성
→ 구현 및 테스트
→ Pull Request 생성
→ 독립 리뷰
→ Equipment Simulator 시험
→ Merge
→ Issue Done
```

PR 본문에는 연결된 Issue를 다음과 같이 작성한다.

```text
Closes #123
```

PR이 기본 브랜치에 병합되면 연결된 Issue가 자동으로 닫힌다.

## 4. 범용 Issue 템플릿

아래 내용을 복사하여 새로운 HOST 작업에 사용한다.

```text
# [HOST-XXX] 작업 제목

## 목적

이 작업이 필요한 업무상의 이유를 작성한다.

## 배경

현재 상황, 문제, 관련 사양 및 선행 작업을 작성한다.

## 현재 동작

현재 프로그램에서 관찰되는 동작이나 누락된 기능을 작성한다.

## 기대 동작

변경 후 사용자가 확인할 수 있는 동작을 작성한다.

## 작업 범위

- [ ] 작업 항목 1
- [ ] 작업 항목 2
- [ ] 작업 항목 3

## 제외 범위

- 이번 Issue에서 변경하지 않을 내용 1
- 이번 Issue에서 변경하지 않을 내용 2

## 입력과 예상 결과

### 정상 입력

- 입력:
- 예상 결과:

### 오류 입력

- 입력:
- 예상 오류 처리:

## 기술적 제약

- Target Framework: .NET Framework 4.7.2
- UI: WPF/MVVM
- Platform Target: x86 또는 x64 명시
- Protocol SDK: Secs4Net 2.4.4 또는 승인된 4.7.2 호환 SDK
- 통신 SDK는 Protocol Adapter 뒤에서만 사용
- Interface Specification을 우선
- 기존 BCB 파일 수정 시 CP949/Windows-949 유지

## 선행조건 및 의존성

- 선행 Issue:
- 차단 중인 항목:
- 관련 모듈:

## 산출물

- 코드 또는 문서
- 자동 테스트
- 시험 로그 또는 결과 보고서

## 완료 조건

- [ ] 기대 동작을 확인했다.
- [ ] 정상 입력 테스트가 통과한다.
- [ ] 오류 입력이 안전하게 처리된다.
- [ ] Timeout 또는 연결 오류가 처리된다.
- [ ] 기존 기능의 결과가 변경되지 않는다.
- [ ] Debug 빌드가 성공한다.
- [ ] Release 빌드가 성공한다.
- [ ] 관련 자동 테스트가 통과한다.
- [ ] 변경 파일과 테스트 결과가 보고된다.
- [ ] 미확인 사항과 잔여 위험이 기록된다.

## 참고 자료

- Interface Specification:
- 관련 문서:
- 관련 Issue/PR:

## 에이전트 제출물

- 변경한 파일 목록
- 변경 이유와 주요 설계 결정
- 빌드 및 테스트 결과
- 확인하지 못한 사항
- 남아 있는 위험
- 통합 시 주의사항
- 커밋 해시
```

## 5. HOST-001 Issue 작성 예시

```text
# [HOST-001] 요구사항 및 설비 사양 정리

## 목적

반도체 설비 Host Simulator 개발을 시작하기 전에 대상 설비의
HSMS/SECS-II/GEM 통신 조건, 필수 메시지 및 첫 번째 시험 Scenario를 확정한다.

이 Issue에서는 제품 코드를 구현하지 않고 이후 작업의 기준이 될 요구사항과
설비 사양을 문서화한다.

## 배경

Host Simulator는 다음 기술 기준으로 개발할 예정이다.

- C#
- .NET Framework 4.7.2
- WPF/MVVM
- Secs4Net 2.4.4 또는 4.7.2 호환 상용 SDK
- JSON Equipment Profile
- YAML 또는 JSON Scenario

현재 대상 설비의 접속 조건과 필수 메시지가 완전히 정리되지 않았다.

## 작업 범위

- [ ] 대상 설비와 Interface Specification 확인
- [ ] Host와 Equipment의 Active/Passive 역할 확인
- [ ] Equipment IP와 Port 확인
- [ ] Device ID 확인
- [ ] 대상 Windows 버전 확인
- [ ] x86/x64 Platform Target 확인
- [ ] 필수 Stream/Function 목록 작성
- [ ] CEID, RPTID, VID 목록 작성
- [ ] ALID와 Alarm 목록 작성
- [ ] RCMD와 Parameter 목록 작성
- [ ] Primary/Secondary 응답 관계 작성
- [ ] Online/Offline 상태 전이 작성
- [ ] Timeout 및 재시도 조건 작성
- [ ] 첫 번째 자동 시험 Scenario 선정
- [ ] 미확정 항목과 확인 담당자 기록

## 첫 번째 Scenario 후보

HSMS Connect
→ Select
→ Establish Communication
→ Online
→ S2F41 전송
→ S2F42 HCACK 확인
→ S6F11 CEID=1320 수신
→ S6F12 응답
→ PASS/FAIL

메시지 방향과 Data Item 구조는 대상 설비의 Interface Specification을 기준으로 수정한다.

## 제외 범위

- 실제 프로그램 구현
- WPF 화면 개발
- 통신 SDK 설치
- 실제 생산 설비 제어
- 생산 데이터베이스 연결
- Interface Specification에 없는 동작 추정

## 산출물

- 요구사항 문서
- 메시지 카탈로그 초안
- 상태 전이 초안
- 첫 번째 시험 Scenario
- 미확정 항목 목록

## 완료 조건

- [ ] Host와 Equipment의 역할이 명확하게 정의됐다.
- [ ] IP, Port, Device ID가 확인됐다.
- [ ] 대상 OS와 x86/x64 조건이 확인됐다.
- [ ] 첫 번째 개발 대상 메시지가 선정됐다.
- [ ] 메시지 방향과 응답 관계가 작성됐다.
- [ ] 첫 번째 Scenario의 성공과 실패 조건이 작성됐다.
- [ ] 미확정 항목이 별도 목록으로 관리된다.
- [ ] 구현자가 추가 추정 없이 HOST-002와 HOST-003을 시작할 수 있다.
- [ ] 사람의 검토와 승인을 받았다.

## 참고 문서

- README.md
- HOST_SIMULATOR_WORKFLOW.md
- DEVELOPMENT_STEPS.md
- AGENT_WORK_REQUEST_TEMPLATE.md

## 에이전트 제출물

- 작성하거나 변경한 파일
- 확인한 설비 사양
- 확정된 내용
- 확인하지 못한 내용
- 서로 충돌하거나 해석이 필요한 사양
- 후속 작업에서 주의할 위험
```

## 6. Pull Request 템플릿

아래 내용을 복사하여 모든 Pull Request에 사용한다.

```text
# [HOST-XXX] 변경 내용을 한 문장으로 작성

## 연결된 Issue

<!-- 필수: 병합 시 Issue가 자동으로 닫히도록 실제 번호를 입력한다. 예: Closes #123 -->
Closes #이슈번호

## 변경 목적

이 변경이 필요한 이유를 작성한다.

## 주요 변경 사항

- 변경 사항 1
- 변경 사항 2
- 변경 사항 3

## 변경 파일

- `파일 경로`: 변경 이유
- `파일 경로`: 변경 이유

## 제외한 내용

- 이번 PR에서 구현하지 않은 내용 1
- 이번 PR에서 구현하지 않은 내용 2

## 검증 환경

- OS:
- Visual Studio:
- .NET Framework: 4.7.2
- Platform Target: x86 또는 x64
- Protocol SDK:
- Equipment Simulator:

## 빌드 결과

- [ ] Debug 빌드 성공
- [ ] Release 빌드 성공
- [ ] 모든 프로젝트가 .NET Framework 4.7.2를 대상으로 한다.
- [ ] x86/x64 Platform Target이 요구사항과 일치한다.

실행한 빌드와 결과:

## 테스트 결과

- [ ] 자동 테스트 통과
- [ ] 정상 Scenario 통과
- [ ] 오류 Scenario 통과
- [ ] Timeout Scenario 통과
- [ ] 재접속 시험 통과

실행한 테스트:

테스트 결과 및 로그:

## 통신 검증

통신 변경이 없는 경우 `해당 없음`으로 작성한다.

- [ ] Stream/Function이 사양과 일치한다.
- [ ] W-Bit이 사양과 일치한다.
- [ ] System Byte가 요청과 응답에서 일치한다.
- [ ] Primary/Secondary 관계가 정확하다.
- [ ] Timeout이 처리된다.
- [ ] 예상하지 않은 메시지가 안전하게 처리된다.
- [ ] Raw Data와 해석 결과가 로그에 기록된다.

## 호환성 확인

- [ ] Secs4Net을 사용한다면 2.4.4로 고정됐다.
- [ ] 최신 Secs4Net 3.x로 자동 업데이트되지 않는다.
- [ ] 기존 기능의 결과가 변경되지 않았다.
- [ ] BCB 파일을 수정했다면 CP949 인코딩을 유지했다.

## 리뷰어가 집중해서 볼 부분

- 확인이 필요한 설계 판단:
- 오류 가능성이 높은 로직:
- 설비 사양 해석이 필요한 부분:
- 성능 또는 스레드 관련 부분:

## 확인하지 못한 사항

- 실제로 확인하지 못한 조건:

없으면 `없음`으로 작성한다.

## 잔여 위험

- 알려진 위험:
- 후속 Issue에서 처리할 내용:
- 배포 또는 실제 설비 시험 시 주의사항:

## 최종 체크리스트

- [ ] 연결된 Issue를 `Closes #이슈번호`로 명시했다.
- [ ] 연결된 Issue의 완료 조건을 모두 확인했다.
- [ ] 작업 범위 밖의 불필요한 변경이 없다.
- [ ] 임시 코드와 디버그 출력이 없다.
- [ ] 관련 문서를 업데이트했다.
- [ ] 변경 파일과 테스트 결과가 보고됐다.
- [ ] 미확인 사항과 잔여 위험을 작성했다.
```

## 7. 리뷰 기준

리뷰어는 코드가 많거나 복잡해 보이는지를 기준으로 승인하지 않는다. 다음 항목을 기준으로 검토한다.

1. Issue의 요구사항과 완료 조건을 충족했는가?
2. Interface Specification과 메시지 구조가 일치하는가?
3. 오류, Timeout 및 재접속을 안전하게 처리하는가?
4. 검증 결과와 로그가 충분한가?
5. 작업 범위를 불필요하게 확대하지 않았는가?
6. 미확인 사항과 잔여 위험을 명확하게 공개했는가?
7. 다음 작업자가 추가 추정 없이 이어서 작업할 수 있는가?

## 8. 병합 기준

다음 조건을 모두 만족한 PR만 병합한다.

- 연결된 Issue가 존재한다.
- 완료 조건이 확인됐다.
- 빌드와 관련 테스트가 통과했다.
- 독립적인 리뷰를 받았다.
- 검토 의견이 해결됐다.
- 미확인 사항과 잔여 위험이 승인됐다.
- 실제 설비 시험이 필요한 변경은 사람의 승인을 받았다.

## 9. Claude가 코드 구현을 담당하는 경우

Claude는 구현자로 사용하고, 최종 검증과 병합 판단은 Codex 또는 사람이 담당한다.

```text
사람/리드
Issue와 완료 조건 확정
    ↓
Claude
별도 branch/worktree에서 구현 및 테스트
    ↓
Claude
변경 파일, 테스트 결과, 미확인 사항과 위험 보고
    ↓
사람/리드
Claude 보고를 Codex 세션에 전달
    ↓
Codex 또는 사람
Issue 기준 독립 리뷰 및 재검증
    ↓
문제 있음 → 사람이 Claude 세션에 구체적인 수정 요청 전달
문제 없음 → 지정된 담당자가 Commit/Push/PR 생성
    ↓
사람 승인 후 Merge
```

### Claude 작업 요청

Claude에게는 GitHub Issue 전체와 다음 지침을 함께 전달한다.

```text
이 Issue의 적용 범위 안에서만 구현한다.
먼저 관련 코드를 조사하고 예상 변경 파일과 구현 계획을 보고한다.
별도 branch/worktree에서 작업한다.
요구사항에 없는 리팩터링이나 기능 추가를 하지 않는다.
모든 프로젝트는 .NET Framework 4.7.2를 유지한다.
Secs4Net을 사용한다면 2.4.4를 유지한다.
기존 BCB 파일을 수정한다면 CP949 인코딩을 유지한다.
완료 후 변경 파일, 빌드, 테스트, 미확인 사항과 잔여 위험을 보고한다.
```

### 역할 구분

- Claude: 코드 구현, 관련 테스트 작성, 변경 결과 보고
- Codex: 요구사항 대비 diff 리뷰, 회귀 위험과 누락 검사, 테스트 재실행
- 사람: 세션 간 요청과 검토 의견 전달, Commit/Push/PR 담당 지정, 사양 해석, 실제 설비 시험 승인, 최종 병합 결정
- 지정된 담당자: Commit/Push/PR 생성 또는 갱신

Claude가 작성한 코드를 동일한 Claude 세션이 최종 승인하지 않는다. 검토자는 발견 사항을 먼저 보고하고, 사람은 해당 내용을 Claude 세션에 전달하여 수정 이력을 유지한다. 자동 A2A 전달은 사용하지 않는다.

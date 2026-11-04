# HOST-001 요구사항 문서

## 1. 목적

반도체 설비(ATI)의 HSMS/SECS-II/GEM 통신을 개발 PC에서 검증하기 위한 Host Simulator의 개발 범위, 대상 설비 조건, 접속 정보 및 제외 범위를 확정한다.

## 2. 대상 설비

| 항목 | 값 | 비고 |
|------|------|------|
| 설비 명칭 | TBD | Interface Specification 확보 후 확정 |
| 설비 종류 | 반도체 제조 설비 | |
| 통신 표준 | SEMI E37 (HSMS), SEMI E5 (SECS-II), SEMI E30 (GEM) | |

## 3. Host/Equipment 역할

| 항목 | 값 | 비고 |
|------|------|------|
| Host 역할 | TBD | Interface Specification 확보 후 확정 |
| Equipment 역할 | TBD | Interface Specification 확보 후 확정 |
| 연결 주체 | TBD | Host/Equipment 역할에 따라 결정 |

> **참고**: 일반적으로 Host=Active(TCP Client), Equipment=Passive(TCP Server)이나 설비에 따라 반대일 수 있다. Interface Specification 확보 후 확정한다.

## 4. 접속 정보

| 항목 | 값 | 비고 |
|------|------|------|
| Equipment IP | TBD | 설비 네트워크 담당자 확인 필요 |
| Equipment Port | TBD | 일반적으로 5000번대 |
| Device ID | TBD | 설비 사양서 확인 후 확정 |
| Session ID | TBD | 설비 사양서 확인 후 확정 |

개발 및 시험 단계에서는 Equipment Simulator를 사용한다. Simulator 선정 후 접속 정보를 확정한다.

HOST-003 로컬 시험은 `Root_ROE2.exe`를 사용하며 IP=`127.0.0.1`, Port=`6000`, Device ID=`1`, Host=`Active`, Equipment Simulator=`Passive`로 확정했다. 이 값은 로컬 Simulator 전용이며 실제 설비 접속 정보는 아니다.

## 5. 대상 환경

| 항목 | 값 | 비고 |
|------|------|------|
| 운영체제 | TBD | 대상 PC 확인 후 확정 |
| 플랫폼 | x86 또는 x64 | 통신 SDK와 대상 PC에 맞춰 확정. TBD |
| 런타임 | .NET Framework 4.7.2 | |
| UI | WPF / MVVM | |

## 6. 개발 범위 (MVP)

첫 번째 버전은 다음 수직 흐름을 완성한다.

```text
HSMS Connect
→ Select
→ Establish Communication (S1F13/S1F14)
→ Online (S1F17/S1F18)
→ S2F41 전송 및 S2F42 확인 (Remote Command)
→ S6F11 수신 및 S6F12 응답 (Event Report)
→ 로그 저장
→ Scenario Pass/Fail 판정
```

### 포함 기능

- Equipment Profile별 HSMS 연결 설정 (JSON)
- HSMS Connect, Select, Linktest 및 재접속
- SECS-II Primary/Secondary 메시지 송수신
- Communication 및 Online/Offline 상태 관리
- S2F41/S2F42 Remote Command 검증
- S6F11/S6F12 Event Report 검증
- S5F1/S5F2 Alarm Report 검증
- 자동 Scenario 실행 및 단계별 Pass/Fail 판정
- Raw Data와 해석된 메시지 로그 저장
- Timeout, 연결 중단 및 비정상 메시지 시험

## 7. 제외 범위

- 전체 MES 기능
- 생산 DB 연결
- 모든 GEM300 기능
- 실제 생산 제어
- 복잡한 범용 스크립트 언어
- 기존 BCB Equipment 프로그램 변경
- 사양서에 없는 메시지 구조 추정

## 8. 기술 제약

- 모든 프로젝트의 Target Framework를 .NET Framework 4.7.2로 통일한다.
- Secs4Net 사용 시 2.4.4 버전으로 고정한다. 3.x로 업그레이드하지 않는다.
- 통신 SDK는 Protocol Adapter 뒤에 격리한다. UI 코드에서 직접 호출하지 않는다.
- 새 C# 및 문서 파일은 UTF-8을 사용한다.
- 기존 BCB `.cpp`, `.h`, `.dfm` 파일은 CP949/Windows-949 인코딩을 유지한다.
- 실제 장비 시험 전 Equipment Simulator로 검증한다.
- 실제 생산 제어는 별도 승인 없이 실행하지 않는다.

## 9. Timeout 기본값

| Timer | 설명 | 기본값 | 비고 |
|-------|------|--------|------|
| T3 | Reply Timeout | 45초 | 설비 사양서 확인 후 조정 |
| T5 | Connect Separation Timeout | 10초 | 연결 실패 후 다음 시도까지의 대기 시간. TCP Connect Timeout과 구분 |
| T6 | Control Transaction Timeout | 5초 | |
| T7 | Not Selected Timeout | 10초 | |
| T8 | Network Intercharacter Timeout | 5초 | |

> **TBD**: 설비별 Timeout 값은 Interface Specification 확보 후 확정한다.

## 10. 관련 문서

- [메시지 카탈로그 초안](./message-catalog.md)
- [상태 전이 초안](./state-transition.md)
- [첫 번째 시험 Scenario](./test-scenario-001.md)
- [미확정 항목 목록](./open-items.md)

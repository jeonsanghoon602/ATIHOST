# HOST-001 미확정 항목 목록

## 1. 개요

이 문서는 HOST-001 요구사항 및 설비 사양 정리 과정에서 확정하지 못한 항목을 관리한다. 각 항목은 확인 담당자와 예상 확인 시점을 기록하여 후속 작업(HOST-002, HOST-003)이 추가 추정 없이 시작할 수 있도록 한다.

## 2. 미확정 항목

### 2.1 설비 사양 관련

| ID | 항목 | 현재 상태 | 확인 담당자 | 예상 확인 시점 | 영향 범위 |
|----|------|-----------|------------|----------------|-----------|
| OI-001 | Interface Specification 확보 | 미확보 | 설비 담당자 / PM | HOST-002 시작 전 | 전체 메시지 카탈로그, CEID/VID/ALID/RCMD 확정 |
| OI-002 | Host/Equipment Active/Passive 역할 | 미확정 | 설비 담당자 | Interface Specification 확보 시 | HSMS 연결 방향, Equipment Profile 설정 |
| OI-003 | Equipment IP, Port | 미확정 | 네트워크/설비 담당자 | 실제 설비 시험 전 | Equipment Profile 설정 |
| OI-004 | Device ID / Session ID | 미확정 | 설비 담당자 | Interface Specification 확보 시 | HSMS Select, 메시지 교환 |

### 2.2 메시지 관련

| ID | 항목 | 현재 상태 | 확인 담당자 | 예상 확인 시점 | 영향 범위 |
|----|------|-----------|------------|----------------|-----------|
| OI-005 | CEID 목록 및 의미 | TBD | 설비 담당자 | Interface Specification 확보 시 | S6F11 처리, Event Report 해석, 시험 Scenario |
| OI-006 | RPTID 및 VID 연결 관계 | TBD | 설비 담당자 | Interface Specification 확보 시 | Report Data 해석, 메시지 카탈로그 |
| OI-007 | VID Data Type | TBD | 설비 담당자 | Interface Specification 확보 시 | 메시지 Encode/Decode |
| OI-008 | ALID 목록 및 Alarm 조건 | TBD | 설비 담당자 | Interface Specification 확보 시 | S5F1/S5F2 처리 |
| OI-009 | RCMD 및 Parameter 목록 | TBD | 설비 담당자 | Interface Specification 확보 시 | S2F41/S2F42 처리, Remote Command 검증 |
| OI-010 | 설비별 Timeout 값 (T3/T5/T6/T7/T8) | SEMI 표준 기본값 사용 중 | 설비 담당자 | Interface Specification 확보 시 | Timeout 처리, 재접속 정책 |

### 2.3 환경 관련

| ID | 항목 | 현재 상태 | 확인 담당자 | 예상 확인 시점 | 영향 범위 |
|----|------|-----------|------------|----------------|-----------|
| OI-011 | 대상 PC Windows 버전 | 미확정 | 배포 담당자 | 배포 계획 수립 시 | .NET Framework 호환성 |
| OI-012 | x86/x64 Platform Target | 미확정 | 통신 SDK/배포 담당자 | HOST-003 SDK PoC 시 | 빌드 설정, 배포 패키지 |
| OI-013 | Equipment Simulator 선정 | 미선정 | 개발팀 | HOST-003 시작 전 | SDK PoC, 자동 시험 환경 |

### 2.4 개발 관련

| ID | 항목 | 현재 상태 | 확인 담당자 | 예상 확인 시점 | 영향 범위 |
|----|------|-----------|------------|----------------|-----------|
| OI-014 | Secs4Net 2.4.4 vs 상용 SDK 결정 | Secs4Net 2.4.4 기본 | 개발팀 / PM | HOST-003 SDK PoC 시 | Protocol 계층 전체 |
| OI-015 | Scenario 파일 형식 (YAML/JSON) | 미확정 | 개발팀 | HOST-009 시작 전 | Scenario Runner |

## 3. 해결 프로세스

1. 미확정 항목을 발견하면 이 문서에 추가한다.
2. 확인 담당자에게 확인을 요청한다.
3. 확인이 완료되면 상태를 "확정"으로 변경하고 확정 값을 기록한다.
4. 관련 문서(요구사항, 메시지 카탈로그, 상태 전이, 시험 Scenario)를 업데이트한다.

## 4. 확정 이력

| 날짜 | ID | 확정 내용 | 확인자 |
|------|------|-----------|--------|
| — | — | (아직 확정된 항목 없음) | — |

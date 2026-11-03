# HOST-001 첫 번째 시험 Scenario

## 1. 개요

이 문서는 Host Simulator MVP의 첫 번째 자동 시험 Scenario를 정의한다. 정상 흐름, 오류 흐름, Timeout 흐름 3가지 케이스를 포함한다.

대상: Equipment Simulator 환경에서 HSMS 연결부터 S2F41/S2F42, S6F11/S6F12까지의 수직 흐름을 검증한다.

## 2. 전제 조건

| 항목 | 값 |
|------|------|
| 시험 환경 | Equipment Simulator (127.0.0.1:5000) |
| Host 역할 | Active |
| Equipment 역할 | Passive |
| Device ID | 0 |
| T3 | 45초 |
| T5 | 10초 |
| T6 | 5초 |
| T7 | 10초 |

## 3. Scenario A — 정상 흐름

### 목적

HSMS 연결부터 Remote Command 전송, Event Report 수신까지 전체 정상 흐름이 동작하는지 확인한다.

### 단계

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1 | TCP Connect | H → E | TCP 연결 성공 | T5 |
| 2 | Select.req | H → E | Select.rsp 수신 (Status=0) | T6 |
| 3 | S1F13 Establish Communication | H → E | S1F14 수신, COMMACK=0 | T3 |
| 4 | S1F17 Request ON-LINE | H → E | S1F18 수신, ONLACK=0 | T3 |
| 5 | S2F41 Host Command Send | H → E | S2F42 수신, HCACK=0 | T3 |
| 6 | S6F11 대기 (Event Report) | E → H | S6F11 수신, CEID 확인 | T3 |
| 7 | S6F12 응답 전송 | H → E | ACKC6=0 전송 완료 | — |
| 8 | 로그 확인 | — | 전체 송수신 Raw Data 기록 완료 | — |

### 판정 기준

- **PASS**: 모든 단계가 예상 결과와 일치하고 Timeout 없이 완료
- **FAIL**: 어느 단계에서든 예상 결과와 불일치하거나 Timeout 발생

### RCMD 및 CEID

> **TBD**: 구체적 RCMD 이름, Parameter, CEID 값은 Interface Specification 확보 후 확정한다. 시험 시 Equipment Simulator의 기본 RCMD와 CEID를 사용한다.

## 4. Scenario B — 오류 흐름

### 목적

비정상 응답을 수신했을 때 Host Simulator가 안전하게 처리하는지 확인한다.

### 케이스 B-1: Establish Communication 거부

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1 | TCP Connect | H → E | TCP 연결 성공 | T5 |
| 2 | Select.req | H → E | Select.rsp 수신 (Status=0) | T6 |
| 3 | S1F13 Establish Communication | H → E | S1F14 수신, COMMACK=1 (Denied) | T3 |
| 4 | 상태 확인 | — | GEM 상태가 Communication Not Established 유지 | — |
| 5 | 로그 확인 | — | 거부 원인이 로그에 기록됨 | — |

판정: COMMACK=1을 받았을 때 프로그램이 종료되지 않고 상태가 올바르면 **PASS**

### 케이스 B-2: Remote Command 거부

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1-4 | 정상 흐름과 동일 | — | Online 상태 도달 | — |
| 5 | S2F41 Host Command Send | H → E | S2F42 수신, HCACK=1 (Command Does Not Exist) | T3 |
| 6 | 상태 확인 | — | Online 상태 유지, 프로그램 종료 없음 | — |
| 7 | 로그 확인 | — | HCACK 값과 실패 원인이 로그에 기록됨 | — |

판정: HCACK≠0을 받았을 때 안전하게 처리하면 **PASS**

### 케이스 B-3: 잘못된 S6F11 수신

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1-4 | 정상 흐름과 동일 | — | Online 상태 도달 | — |
| 5 | 잘못된 형식의 S6F11 수신 | E → H | 파싱 오류 감지, 프로그램 종료 없음 | — |
| 6 | 로그 확인 | — | 오류 메시지와 Raw Data가 기록됨 | — |

판정: 잘못된 메시지를 받아도 프로그램이 종료되지 않으면 **PASS**

## 5. Scenario C — Timeout 흐름

### 목적

응답이 오지 않을 때 Timeout이 올바르게 동작하는지 확인한다.

### 케이스 C-1: S1F14 Timeout (Establish Communication)

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1 | TCP Connect | H → E | TCP 연결 성공 | T5 |
| 2 | Select.req | H → E | Select.rsp 수신 (Status=0) | T6 |
| 3 | S1F13 전송 | H → E | S1F14 미수신 | T3 |
| 4 | Timeout 감지 | — | T3 Timeout 발생, Scenario FAIL 기록 | T3 |
| 5 | 상태 확인 | — | 프로그램 종료 없음, 재시도 가능 상태 | — |

판정: T3 Timeout을 감지하고 프로그램이 안전하면 **PASS**

### 케이스 C-2: S2F42 Timeout (Remote Command)

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1-4 | 정상 흐름과 동일 | — | Online 상태 도달 | — |
| 5 | S2F41 전송 | H → E | S2F42 미수신 | T3 |
| 6 | Timeout 감지 | — | T3 Timeout 발생, 해당 트랜잭션 실패 기록 | T3 |
| 7 | 상태 확인 | — | Online 상태 유지, 프로그램 종료 없음 | — |

판정: Reply Timeout을 감지하고 상태가 유지되면 **PASS**

### 케이스 C-3: TCP 연결 실패

| 순서 | 동작 | 방향 | 예상 결과 | Timeout |
|------|------|------|-----------|---------|
| 1 | TCP Connect (잘못된 IP/Port) | H → E | TCP 연결 실패 | T5 |
| 2 | 상태 확인 | — | Disconnected 상태, 프로그램 종료 없음 | — |
| 3 | 로그 확인 | — | 연결 실패 원인이 기록됨 | — |

판정: 연결 실패를 감지하고 프로그램이 안전하면 **PASS**

## 6. 공통 판정 규칙

- 각 단계의 시작, 종료, 소요 시간을 기록한다.
- 실패 시 어느 단계에서 어떤 이유로 실패했는지 식별할 수 있어야 한다.
- 전체 Scenario 결과를 파일로 저장한다.
- 프로그램이 예외로 종료되면 해당 Scenario는 무조건 **FAIL**이다.

## 7. 미확정 사항

| 항목 | 비고 |
|------|------|
| S2F41의 구체적 RCMD 및 Parameter | Interface Specification 확보 후 확정 |
| S6F11의 구체적 CEID 및 Report 구조 | Interface Specification 확보 후 확정 |
| Equipment Simulator 설정 방법 | Simulator 선정 후 작성 |
| Scenario 파일 형식 (YAML/JSON) | HOST-009에서 확정 |

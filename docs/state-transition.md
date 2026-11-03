# HOST-001 상태 전이 초안

## 1. 개요

이 문서는 Host Simulator에서 관리하는 HSMS 연결 상태와 GEM Communication/Control 상태의 전이를 정리한다.

## 2. HSMS 상태 전이

### 상태 정의

| 상태 | 설명 |
|------|------|
| Disconnected | TCP 연결 없음 |
| Connecting | TCP 연결 시도 중 |
| Connected | TCP 연결 성공, HSMS Select 전 |
| Selected | HSMS Select 완료, SECS-II 메시지 교환 가능 |
| Disconnecting | 연결 종료 진행 중 |

### 전이 다이어그램

```text
                    Connect 요청
  Disconnected ──────────────────► Connecting
       ▲                              │
       │                    성공 ──────┤
       │                              ▼
       │              T5 Timeout   Connected
       │◄──────────────────────────── │
       │                              │
       │                    Select.req 전송
       │                              │
       │                              ▼
       │    Separate /             Selected
       │    TCP 단절 /        (SECS-II 교환 가능)
       │    T7 Timeout               │
       │◄─────────────────────────────┤
       │                              │
       │                    Deselect / Disconnect
       │                              │
       │                              ▼
       │◄──────────────────── Disconnecting
```

### 전이 조건

| 현재 상태 | 이벤트 | 다음 상태 | 비고 |
|-----------|--------|-----------|------|
| Disconnected | Connect 요청 | Connecting | Host Active 모드 |
| Connecting | TCP 연결 성공 | Connected | |
| Connecting | TCP 연결 실패 / T5 Timeout | Disconnected | 재접속 정책에 따라 재시도 |
| Connected | Select.req 전송 → Select.rsp 수신 (성공) | Selected | |
| Connected | Select 실패 / T6 Timeout | Disconnected | |
| Connected | T7 Timeout (Not Selected) | Disconnected | |
| Selected | Deselect 요청 | Connected | |
| Selected | Separate 수신 | Disconnected | 즉시 연결 종료 |
| Selected | TCP 단절 감지 | Disconnected | |
| Selected | Disconnect 요청 | Disconnecting | |
| Disconnecting | 종료 완료 | Disconnected | |
| 모든 상태 | TCP 오류 / 예외 | Disconnected | 프로그램 종료 없이 처리 |

### Linktest

- Selected 상태에서 주기적으로 Linktest.req를 전송하여 연결 유지를 확인한다.
- Linktest.rsp가 T6 이내에 수신되지 않으면 연결 단절로 판단한다.

### 재접속 정책

- 연결 단절 시 Auto Reconnect가 활성화되어 있으면 설정된 간격으로 재접속을 시도한다.
- 재접속 간격은 Equipment Profile의 Reconnect Interval을 따른다.
- 재접속 시 HSMS 상태는 Disconnected에서 다시 시작한다.

## 3. GEM Communication 상태 전이

### 상태 정의

| 상태 | 설명 |
|------|------|
| Communication Not Established | HSMS Selected 상태이나 GEM Communication 미수립 |
| Communication Established (Wait CRA) | S1F13 전송 후 S1F14 대기 중 |
| Communicating | S1F14 COMMACK=0 수신, Communication 수립 완료 |

### 전이 다이어그램

```text
  HSMS Selected
       │
       ▼
  Comm Not Established
       │
       │  S1F13 전송
       ▼
  Wait CRA (S1F14 대기)
       │
       ├─ S1F14 COMMACK=0 ──► Communicating
       │
       ├─ S1F14 COMMACK=1 ──► Comm Not Established (재시도)
       │
       └─ T3 Timeout ────────► Comm Not Established (재시도)
```

### 전이 조건

| 현재 상태 | 이벤트 | 다음 상태 |
|-----------|--------|-----------|
| Comm Not Established | S1F13 전송 | Wait CRA |
| Wait CRA | S1F14 COMMACK=0 | Communicating |
| Wait CRA | S1F14 COMMACK=1 | Comm Not Established |
| Wait CRA | T3 Timeout | Comm Not Established |
| Communicating | HSMS 연결 단절 | Comm Not Established |
| 모든 GEM 상태 | HSMS Disconnected | Comm Not Established |

## 4. GEM Control 상태 전이

### 상태 정의

| 상태 | 설명 |
|------|------|
| Equipment Offline | 설비가 Offline 상태 |
| Attempt Online | S1F17 전송 후 S1F18 대기 중 |
| Equipment Online | 설비가 Online 상태, 명령 수행 가능 |

### 전이 다이어그램

```text
  Communicating
       │
       ▼
  Equipment Offline
       │
       │  S1F17 전송
       ▼
  Attempt Online (S1F18 대기)
       │
       ├─ S1F18 ONLACK=0 ──► Equipment Online
       │
       ├─ S1F18 ONLACK=1 ──► Equipment Offline (Not Allowed)
       │
       ├─ S1F18 ONLACK=2 ──► Equipment Online (Already Online)
       │
       └─ T3 Timeout ────────► Equipment Offline
```

### 상태별 명령 허용 정책

| 상태 | 허용 명령 | 비고 |
|------|-----------|------|
| Equipment Offline | S1F17 (Online 요청) | 다른 SECS-II 명령 차단 |
| Equipment Online | S2F41, S5F2, S6F12 등 | 전체 SECS-II 명령 사용 가능 |

### 상태 초기화 조건

- HSMS 연결이 해제되면 GEM 상태는 Communication Not Established로 초기화된다.
- 재접속 후 S1F13부터 다시 시작해야 한다.

## 5. Timeout 요약

| Timer | 적용 구간 | 기본값 | 동작 |
|-------|-----------|--------|------|
| T3 | SECS-II Reply 대기 | 45초 | Timeout 시 해당 트랜잭션 실패 |
| T5 | Connect Separation | 10초 | 연결 재시도 간격 |
| T6 | Control Transaction | 5초 | Select/Deselect/Linktest 응답 대기 |
| T7 | Not Selected | 10초 | Connected 상태에서 Select 없으면 Disconnect |
| T8 | Network Intercharacter | 5초 | 메시지 수신 중 바이트 간격 초과 시 메시지 폐기 |

> **TBD**: 설비별 Timeout 값은 Interface Specification 확보 후 확정한다.

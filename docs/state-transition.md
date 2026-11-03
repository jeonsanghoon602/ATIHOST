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
       │         TCP 연결 실패     Connected
       │◄───────── (T5 대기 후        │
       │           재시도)             │
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
| Disconnected | Connect 요청 | Connecting | Host/Equipment 역할은 TBD |
| Connecting | TCP 연결 성공 | Connected | |
| Connecting | TCP 연결 실패 | Disconnected | T5 대기 후 재시도 (아래 참고) |
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

### T5 Connect Separation

T5는 개별 TCP 연결 시도의 Timeout이 아니라, 실패한 연결 시도와 다음 연결 시도 사이의 대기 시간(Connect Separation Time)이다.

```text
TCP 연결 실패 → Disconnected → T5 대기 → 다음 Connect 요청 → Connecting
```

개별 TCP 연결 시도의 제한 시간은 OS 또는 SDK의 TCP Connect Timeout이며, T5와 구분한다.

### 재접속 정책

- 연결 단절 시 Auto Reconnect가 활성화되어 있으면 재접속을 시도한다.
- 유효 재접속 대기 시간은 `max(T5, Reconnect Interval)`로 한다. T5는 연결 시도 사이의 최소 대기 시간이므로 Reconnect Interval이 T5보다 짧을 수 없다.
- Equipment Profile에서 Reconnect Interval을 설정할 때 `Reconnect Interval >= T5`를 검증한다.
- 재접속 시 HSMS 상태는 Disconnected에서 다시 시작한다.

## 3. GEM Communication 상태 전이

### 상태 정의

| 상태 | 설명 |
|------|------|
| Communication Not Established | HSMS Selected 상태이나 GEM Communication 미수립 |
| Communication Establishing (Wait CRA) | S1F13 전송 후 S1F14 대기 중 |
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
  Establishing / Wait CRA (S1F14 대기)
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
| Comm Not Established | S1F13 전송 | Establishing (Wait CRA) |
| Establishing (Wait CRA) | S1F14 COMMACK=0 | Communicating |
| Establishing (Wait CRA) | S1F14 COMMACK=1 | Comm Not Established |
| Establishing (Wait CRA) | T3 Timeout | Comm Not Established |
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

Host가 시작하는 명령(Host Initiated)과 Equipment가 보낸 Primary에 대한 Secondary 응답(Protocol Response)을 구분한다.

#### Host Initiated 명령

| 상태 | 허용 명령 | 비고 |
|------|-----------|------|
| Equipment Offline | S1F17 (Online 요청) | Host가 시작하는 다른 SECS-II 명령은 차단 |
| Equipment Online | S2F41 등 | 전체 Host Initiated 명령 사용 가능 |

#### Protocol Response (수신 Primary에 대한 Secondary 응답)

| 상태 | 동작 | 비고 |
|------|------|------|
| Equipment Offline | 수신한 S5F1에 S5F2 응답, S6F11에 S6F12 응답 전송 | 프로토콜 규약상 Secondary 응답은 상태와 무관하게 전송해야 할 수 있음 |
| Equipment Online | 수신한 Primary에 대해 정상 Secondary 응답 전송 | |

> **TBD**: Offline 상태에서 Equipment가 보낸 S5F1, S6F11 등에 대한 Secondary 응답 정책은 설비 Interface Specification 확보 후 확정한다. 설비에 따라 Offline에서도 응답이 필요하거나, 특정 메시지만 허용할 수 있다.

### 상태 초기화 조건

- HSMS 연결이 해제되면 GEM 상태는 Communication Not Established로 초기화된다.
- 재접속 후 S1F13부터 다시 시작해야 한다.

## 5. Timeout 요약

| Timer | 적용 구간 | 기본값 | 동작 |
|-------|-----------|--------|------|
| T3 | SECS-II Reply 대기 | 45초 | Timeout 시 해당 트랜잭션 실패 |
| T5 | Connect Separation | 10초 | 연결 실패 후 다음 연결 시도까지의 대기 시간. 개별 TCP 연결 시도의 Timeout과 구분 |
| T6 | Control Transaction | 5초 | Select/Deselect/Linktest 응답 대기 |
| T7 | Not Selected | 10초 | Connected 상태에서 Select 없으면 Disconnect |
| T8 | Network Intercharacter | 5초 | 메시지 수신 중 바이트 간격 초과 시 메시지 폐기 |

> **TBD**: 설비별 Timeout 값은 Interface Specification 확보 후 확정한다.

# HOST-001 메시지 카탈로그 초안

## 1. 개요

이 문서는 Host Simulator MVP에서 지원하는 SECS-II 메시지의 목록, Primary/Secondary 관계, Host/Equipment Initiated 구분을 정리한다.

구체적인 CEID, RPTID, VID, ALID, RCMD 값과 Data Item 구조는 설비 Interface Specification 확보 후 확정한다.

## 2. 필수 Stream/Function 목록

### S1 — Equipment Status

| Stream/Function | 방향 | 설명 | W-Bit | 비고 |
|----------------|------|------|-------|------|
| S1F1 | H → E | Are You There Request | W | Host Initiated |
| S1F2 | E → H | On Line Data | — | S1F1에 대한 응답 |
| S1F13 | H → E | Establish Communication Request | W | Host Initiated |
| S1F14 | E → H | Establish Communication Acknowledge | — | COMMACK 확인 |
| S1F15 | H → E | Request OFF-LINE | W | Host Initiated |
| S1F16 | E → H | OFF-LINE Acknowledge | — | OFLACK 확인 |
| S1F17 | H → E | Request ON-LINE | W | Host Initiated |
| S1F18 | E → H | ON-LINE Acknowledge | — | ONLACK 확인 |

### S2 — Equipment Control

| Stream/Function | 방향 | 설명 | W-Bit | 비고 |
|----------------|------|------|-------|------|
| S2F41 | H → E | Host Command Send | W | Remote Command 전송 |
| S2F42 | E → H | Host Command Acknowledge | — | HCACK 확인 |

### S5 — Alarm Management

| Stream/Function | 방향 | 설명 | W-Bit | 비고 |
|----------------|------|------|-------|------|
| S5F1 | E → H | Alarm Report Send | W | Equipment Initiated |
| S5F2 | H → E | Alarm Report Acknowledge | — | ACKC5 응답 |

### S6 — Data Collection

| Stream/Function | 방향 | 설명 | W-Bit | 비고 |
|----------------|------|------|-------|------|
| S6F11 | E → H | Event Report Send | W | Equipment Initiated |
| S6F12 | H → E | Event Report Acknowledge | — | ACKC6 응답 |

### HSMS Control Messages

| 메시지 | 방향 | 설명 |
|--------|------|------|
| Select.req | H → E | HSMS Session 시작 요청 |
| Select.rsp | E → H | Select 응답 |
| Deselect.req | H → E | Session 종료 요청 |
| Deselect.rsp | E → H | Deselect 응답 |
| Linktest.req | H ↔ E | 연결 확인 요청 |
| Linktest.rsp | H ↔ E | Linktest 응답 |
| Separate.req | H ↔ E | 즉시 연결 종료 |

## 3. Primary/Secondary 응답 관계

```text
Host Initiated (Host → Equipment):
  S1F1  → S1F2   (Are You There)
  S1F13 → S1F14  (Establish Communication)
  S1F15 → S1F16  (Request OFF-LINE)
  S1F17 → S1F18  (Request ON-LINE)
  S2F41 → S2F42  (Host Command Send)

Equipment Initiated (Equipment → Host):
  S5F1  → S5F2   (Alarm Report)
  S6F11 → S6F12  (Event Report)
```

## 4. CEID (Collection Event ID) 목록

| CEID | 이름 | 설명 | 비고 |
|------|------|------|------|
| TBD | TBD | TBD | Interface Specification 확보 후 작성 |

> **참고**: MVP 첫 시험에서는 최소 1개의 CEID(예: LoadReq 관련)를 선정하여 S6F11/S6F12 흐름을 검증한다. 구체적 CEID 값은 설비 사양서에서 확정한다.

## 5. RPTID (Report ID) 및 VID (Variable ID) 목록

| RPTID | 관련 CEID | VID 목록 | 비고 |
|-------|-----------|----------|------|
| TBD | TBD | TBD | Interface Specification 확보 후 작성 |

| VID | 이름 | Data Type | 설명 | 비고 |
|-----|------|-----------|------|------|
| TBD | TBD | TBD | TBD | Interface Specification 확보 후 작성 |

## 6. ALID (Alarm ID) 목록

| ALID | 이름 | 설명 | 비고 |
|------|------|------|------|
| TBD | TBD | TBD | Interface Specification 확보 후 작성 |

## 7. RCMD (Remote Command) 및 Parameter 목록

| RCMD | 설명 | Parameter | 비고 |
|------|------|-----------|------|
| TBD | TBD | TBD | Interface Specification 확보 후 작성 |

> **참고**: MVP 첫 시험에서는 최소 1개의 RCMD를 선정하여 S2F41/S2F42 흐름을 검증한다.

## 8. 응답 코드

| 코드 | 필드명 | 값 | 의미 |
|------|--------|------|------|
| COMMACK | S1F14 | 0 | Accepted |
| COMMACK | S1F14 | 1 | Denied, Try Again |
| OFLACK | S1F16 | 0 | Acknowledged |
| ONLACK | S1F18 | 0 | Accepted |
| ONLACK | S1F18 | 1 | Not Allowed |
| ONLACK | S1F18 | 2 | Already Online |
| HCACK | S2F42 | 0 | Acknowledged |
| HCACK | S2F42 | 1 | Command Does Not Exist |
| HCACK | S2F42 | 2 | Cannot Perform Now |
| HCACK | S2F42 | 3 | Parameter Error |
| HCACK | S2F42 | 4 | Acknowledged, Completion Will Be Signaled Later |
| HCACK | S2F42 | 5 | Rejected, Already In Desired Condition |
| HCACK | S2F42 | 6 | No Such Object |
| ACKC5 | S5F2 | 0 | Accepted |
| ACKC6 | S6F12 | 0 | Accepted |

## 9. 추정 금지 항목

AGENTS.md에 따라 다음 항목은 Interface Specification 없이 추정하지 않는다.

- 구체적 CEID 값과 의미
- RPTID와 VID의 연결 관계 및 Data Type
- ALID 값과 Alarm 조건
- RCMD 이름과 Parameter 구조
- Timer 값의 설비별 설정
- 상태 전이에서 설비 고유 조건

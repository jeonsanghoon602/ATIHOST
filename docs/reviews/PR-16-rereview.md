# PR #16 Re-review

- PR: `feat: add Equipment Profile management (HOST-004)`
- Reviewed commit: `1b282737a3ba93b98359751a28624da24fb3bcc7`
- Review date: 2026-11-04
- Verdict: 추가 수정 후 재검토

## 이전 리뷰 확인

1. Device ID 범위: 해결됨. 0~65535로 통일하고 경계 테스트를 추가했다.
2. 필수 JSON 속성: 해결됨. 필수 속성을 표시하고 누락 테스트를 추가했다.
3. Reconnect Interval과 T5 관계: 해결됨. 교차 검증과 테스트를 추가했다.
4. Timer 범위 근거: 코드에 SEMI E37 범위임을 명시했다. 설비 Interface Specification 원문 대조는 아직 하지 않았다.
5. 원자적 파일 저장: 해결되지 않음. 아래 Must fix 1 참고.

## Must fix

### 1. 기존 파일을 먼저 삭제하여 저장 실패 시 Profile을 잃음

`HostSimulator.Infrastructure/Profile/JsonProfileRepository.cs:80-84`는 임시 파일을 쓴 뒤 기존 파일을 삭제하고 이동한다. 삭제와 이동 사이에 프로세스가 종료되거나 `File.Move`가 실패하면 기존 Profile 파일이 사라진다. 이는 원자적 교체가 아니다.

기존 파일이 있으면 `File.Replace(tempPath, _filePath, null)`로 교체하고, 최초 저장일 때만 `File.Move`를 사용한다. 현재 `Atomic_save_does_not_leave_temp_file` 테스트는 정상 경로만 확인하므로 교체 실패 시 기존 파일이 보존되는 테스트를 추가한다.

### 2. 중복 Profile 이름을 허용하여 조회 결과가 모호함

`SaveAll`과 `LoadAll`은 대소문자만 다른 중복 이름도 허용하지만 `LoadByName`과 `Save`는 이름을 대소문자 무시 키로 사용한다. 예를 들어 `EQ01`과 `eq01`을 함께 저장하면 조회는 첫 번째 항목만 반환하고, 갱신 후에도 중복 항목이 남는다.

저장과 로드 경계에서 `StringComparer.OrdinalIgnoreCase`로 중복 이름을 검출해 `ProfileValidationException`을 발생시키고 테스트를 추가한다.

## 검증 결과

- NuGet restore: 성공
- Debug x64 빌드: 경고 0, 오류 0
- Debug 테스트: 81개 통과
- Release x64 빌드: 경고 0, 오류 0
- Release 테스트: 81개 통과
- Equipment Simulator 및 실제 앱 통합: 미검증

## 결론

이전 리뷰의 1~3은 해결됐다. 파일 교체와 중복 Profile 이름 검증을 수정한 뒤 다시 검토해야 한다.

# PR #16 Re-review 2

- PR: `feat: add Equipment Profile management (HOST-004)`
- Reviewed commit: `560ceb8d8baed43c028dafff0f965fa4d05cb3f7`
- Review date: 2026-11-04
- Verdict: 추가 수정 후 재검토

## 이전 리뷰 확인

1. 기존 파일 선삭제 문제: 해결됨. 기존 파일에는 `File.Replace`를 사용한다.
2. 중복 Profile 이름: 해결됨. 로드와 저장에서 대소문자 무시 중복 검사를 수행한다.

## Must fix

### 1. `File.Replace`가 간헐적으로 실패하여 정상 Profile 저장이 실패함

`HostSimulator.Infrastructure/Profile/JsonProfileRepository.cs:84-89`의 교체 경로가 이 Windows/.NET Framework 4.7.2 환경에서 간헐적으로 `IOException` 또는 `UnauthorizedAccessException`을 발생시켰다. Release 전체 테스트는 첫 실행에서 84개 중 1개가 실패했고, 추가 5회 반복에서도 2회 실패했다. 실패한 테스트는 매번 기존 Profile을 갱신하는 정상 경로였다.

원본 파일은 보존되지만 사용자가 Profile 변경을 저장하지 못한다. `File.Replace`의 일시적인 I/O 실패를 짧고 제한된 횟수만 재시도하고, 계속 실패하면 원래 예외를 반환한다. 수정 후 Debug/Release 전체 테스트를 반복 실행해 안정성을 확인한다.

참고: .NET Framework 4.7.2의 `File.Replace`는 I/O 및 접근 오류를 예외로 반환할 수 있다. <https://learn.microsoft.com/dotnet/api/system.io.file.replace?view=netframework-4.7.2>

### 2. 잘못된 JSON 배열 항목이 Profile 검증 예외가 아닌 런타임 예외로 종료됨

`HostSimulator.Infrastructure/Profile/JsonProfileRepository.cs:29-42`는 `JsonSerializationException`만 변환하고 배열의 null 항목을 확인하지 않는다. 직접 실행 결과 JSON `[null]`은 `NullReferenceException`, 문법이 깨진 `[{]`은 `JsonReaderException`을 발생시켰다.

JSON 역직렬화에서는 `JsonException` 전체를 `ProfileValidationException`으로 변환하고, `dtos.Select(ToProfile)` 전에 null 항목을 검출한다. 두 입력에 대한 테스트를 추가한다.

## 검증 결과

- NuGet restore: 성공
- Debug x64 빌드: 경고 0, 오류 0
- Debug 테스트: 제한 없는 실행에서 84개 통과
- Release x64 빌드: 경고 0, 오류 0
- Release 테스트: 최초 실행 실패, 추가 5회 중 2회 실패
- Equipment Simulator 및 실제 앱 통합: 미검증

## 결론

직전 리뷰의 두 항목은 코드에 반영됐지만 저장 교체가 간헐적으로 실패하고, 잘못된 JSON 입력이 일관된 검증 오류로 처리되지 않는다. 두 항목 수정 후 재검토가 필요하다.

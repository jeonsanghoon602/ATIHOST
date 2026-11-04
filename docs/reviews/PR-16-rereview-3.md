# PR #16 Re-review 3

- PR: `feat: add Equipment Profile management (HOST-004)`
- Reviewed commit: `aca84296d127aea63aae208ff72eeb24c663ebe3`
- Review date: 2026-11-04
- Verdict: 저장 교체 수정 후 재검토

## 이전 리뷰 확인

1. 잘못된 JSON 처리: 해결됨. `JsonException`을 변환하고 null 배열 항목을 검출하며, 대상 테스트 3개가 통과했다.
2. `File.Replace` 간헐 실패: 재시도가 추가됐지만 전체 테스트에서 다시 실패하여 해결되지 않음.

## Must fix

### 1. 재시도 후에도 정상 Profile 갱신이 간헐적으로 실패함

`HostSimulator.Infrastructure/Profile/JsonProfileRepository.cs:90-136`은 `File.Replace`의 `IOException`과 `UnauthorizedAccessException`을 50ms 간격으로 재시도한다. 그러나 제한 없는 Windows/.NET Framework 4.7.2 환경의 Debug 전체 테스트에서 `SaveAll_replaces_all_profiles`가 86개 중 유일하게 다시 실패했다. 같은 테스트를 단독 실행하면 통과하므로 파일 교체의 간헐 실패가 아직 남아 있다.

`File.Replace(source, destination, null, ignoreMetadataErrors: true)` 오버로드를 재시도 안에서 사용해 ACL/속성 병합 오류를 제외하고, Debug/Release 전체 테스트를 각각 반복 실행해 실패가 사라지는지 확인한다. 계속 실패하면 재시도 간격을 늘리기 전에 실제 최종 예외 코드를 기록해 원인을 확정한다.

참고: <https://learn.microsoft.com/dotnet/api/system.io.file.replace?view=netframework-4.7.2>

## 검증 결과

- NuGet restore: 성공
- Debug x64 빌드: 경고 0, 오류 0
- Release x64 빌드: 경고 0, 오류 0
- Debug 전체 테스트: 85개 통과, 1개 실패
- 실패 테스트 단독 재실행: 통과
- Release 전체 테스트: 86개 통과
- JSON 오류 처리 대상 테스트: 3개 통과
- Equipment Simulator 및 실제 앱 통합: 미검증

## 결론

JSON 검증 문제는 해결됐다. 파일 교체 재시도는 전체 테스트에서 다시 실패했으므로 병합 전에 저장 안정성을 한 번 더 수정해야 한다.

# Secs4Net 2.4.4 SDK Evaluation — HOST-003

## 1. Overview

This document records the evaluation of Secs4Net version 2.4.4 for use in the ATIHOST Host Simulator project. It covers license terms, deployment constraints, known limitations, and the criteria under which the SDK should be replaced.

## 2. Package Information

| Item | Value |
|------|-------|
| Package | Secs4Net |
| Version | 2.4.4 (pinned, must not float) |
| NuGet | https://www.nuget.org/packages/Secs4Net/2.4.4 |
| Source | https://github.com/mkjeff/secs4net |
| Source commit | 42976b54e2ef26ea69d2cfd8a1d7c4dea8b46be0 |
| Target Framework | netstandard2.0 |
| License | MIT |
| Platform | x64 (managed code, no native dependencies) |

Transitive NuGet dependencies (from project.assets.json):

- Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0
- CommunityToolkit.HighPerformance 8.4.0
- PooledAwait 1.0.49
- System.IO.Pipelines 8.0.0
- System.Threading.Channels 8.0.0

## 3. License and Deployment Terms

Secs4Net 2.4.4 is released under the **MIT License**.

- Commercial use: permitted.
- Redistribution: permitted with license notice.
- Modification: permitted.
- No warranty: provided as-is.
- Attribution: include the MIT license file when distributing.
- No per-seat or runtime licensing fees.
- No server-side restrictions.

**Deployment requirement:** Include the Secs4Net license notice (MIT) in the redistributed package.

## 4. Compatibility

| Item | Status |
|------|--------|
| .NET Framework 4.7.2 | Compatible (via netstandard2.0) |
| x64 Platform Target | Compatible (managed code) |
| x86 Platform Target | Compatible (managed code) |
| WPF coexistence | No conflict (pure library, no UI dependencies) |

The x64 Platform Target is confirmed for this project. Secs4Net 2.4.4 is purely managed code and runs on both x86 and x64 without native dependencies.

## 5. API Architecture (2.4.4)

Secs4Net 2.4.4 uses dependency-injection style construction:

- `SecsGemOptions` — configuration (IP, port, active/passive, device ID, timeouts).
- `HsmsConnection` — HSMS transport; implements `ISecsConnection`; constructor takes `IOptions<SecsGemOptions>` and `ISecsGemLogger`; owns `ConnectionChanged` event and `void Start(CancellationToken)`.
- `SecsGem` — SECS-II/GEM message layer; constructor takes `IOptions<SecsGemOptions>`, `ISecsConnection`, `ISecsGemLogger`; `DeviceId` is readonly.
- `ISecsGemLogger` — custom logging interface.

The PoC adapter wraps `HsmsConnection` only; `SecsGem` is not needed for connection management.

## 6. Known Limitations

1. **Version 2.4.4 is not the legacy 2.x API.** It uses `IOptions<SecsGemOptions>`, `HsmsConnection`, and async patterns. Legacy constructor signatures (`new SecsGem(bool, IPAddress, ushort)`) do not exist.
2. **No .NET 8+ dedicated build needed for net472.** The netstandard2.0 TFM is used on .NET Framework 4.7.2.
3. **Connection lifecycle is on HsmsConnection, not SecsGem.** `ConnectionChanged`, `Start(CancellationToken)` (synchronous void), and `DisposeAsync` are on the connection layer.
4. **Community-maintained.** Not backed by a commercial vendor. Bug fixes depend on community contributions.
5. **Limited documentation.** API documentation is minimal; source code is the primary reference.
6. **HSMS state machine scope.** Provides HSMS connection management. Advanced GEM state machine logic must be built on top.
7. **Thread safety.** The `ConnectionChanged` event fires on background threads. Callers must marshal to the UI thread when updating WPF controls.
8. **Memory on repeated cycles.** Each connect/disconnect cycle creates and disposes an `HsmsConnection` instance. GC pressure under rapid cycling should be verified with a profiler during Equipment Simulator testing.
9. **DeviceId type.** `SecsGemOptions.DeviceId` is `ushort` (confirmed from Secs4Net 2.4.4 source). The adapter casts to `ushort`; ConnectionParameters validates 0–65535 to match.
10. **HSMS Select required before disconnect.** `ConnectionState.Connected` (TCP established) is not sufficient; disposing the connection before `ConnectionState.Selected` (HSMS Select.req/rsp complete) causes a `NullReferenceException` in Secs4Net's PipeDecoder. The adapter must wait for `Selected` before reporting success.

## 7. Replacement Criteria

Replace Secs4Net 2.4.4 if any of the following conditions are met:

1. **Critical bug with no fix available:** A defect in HSMS connection handling, message encoding/decoding, or resource cleanup that cannot be resolved by a workaround or source patch within a reasonable timeframe.
2. **Memory leak or resource leak:** The 100-cycle connect/disconnect test or long-duration communication test shows unbounded memory growth or socket handle leaks that cannot be mitigated.
3. **Missing SEMI standard compliance:** A required HSMS or SECS-II behavior specified in the equipment Interface Specification cannot be implemented using the SDK.
4. **Project migration to .NET 8+:** If the project migrates away from .NET Framework 4.7.2, evaluate Secs4Net 3.x or a commercial SDK.
5. **Commercial support requirement:** If deployment to production environments requires vendor-backed support, a commercial HSMS/SECS SDK (e.g., Brooks, Cimetrix, PEER Group) should be evaluated.

## 8. HOST-003 PoC Scope and Evidence Status

The PoC validates:

- [x] NuGet package installs and builds under net472 / x64. — **Verified: Debug and Release x64 builds succeeded (0 warnings, 0 errors). 38/38 tests passed in both configurations.**
- [x] `HsmsConnection` can be instantiated via `IOptions<SecsGemOptions>` and `ISecsGemLogger` with explicit parameters. — **Verified: PoC executed against 127.0.0.1:1 (closed endpoint), adapter constructed and Start called successfully; TimeoutException returned as expected (exit code 2).**
- [ ] `HsmsConnection` can reach Selected state and be disposed in a cycle against a live Equipment Simulator without exceptions. — **Partially verified with Linkgenesis XGem300Pro: 1/1 cycle and 3/3 cycles with a 12-second delay reached Selected and completed. The final asynchronous-continuation cleanup fix still requires a live retest.**
- [ ] 100 connect/disconnect cycles complete without abnormal termination. — **Loop logic is covered by 38 tests. A 3-second-delay live run completed 5/10 cycles, showing the simulator needed more re-listen time. A 100-cycle run was stopped after XGem stopped listening while a disposal race was being diagnosed; the race was fixed and unit-tested, but the live 100-cycle run remains pending.**
- [x] Protocol Adapter isolates Secs4Net from the UI and Application layers. — **Verified by project reference structure: App does not reference Protocol.**

The PoC does **not** include:

- Full HSMS state machine (HOST-005).
- SECS-II message catalog (HOST-006, HOST-008).
- Production equipment control.

## 9. Console PoC Invocation

Final HOST-003 local simulator configuration:

| Item | Value |
|------|-------|
| Equipment Simulator | `C:\Users\sanghoon\Documents\GitHub\ATI_Calculator_jsh\PR_ROE2_JSH\Root_ROE2\bin\x64\Release\Root_ROE2.exe` |
| IP / Port | `127.0.0.1:6000` |
| Device ID | `1` |
| Host role | Active |
| Equipment Simulator role | Passive |

These values apply only to the local simulator. Production equipment settings remain pending the Interface Specification.

```
dotnet run --project HostSimulator.Poc -c Debug -p:Platform=x64 -- <ip> <port> <active|passive> <deviceId> <cycles> <timeoutSeconds> <cycleDelaySeconds>
```

The earlier XGem test used the following command and its observed 12-second re-listen delay:

```
dotnet run --project HostSimulator.Poc -c Debug -p:Platform=x64 -- 127.0.0.1 6000 active 1 100 10 12
```

All seven arguments are required. No defaults are assumed. `cycleDelaySeconds` is the pause between cycles (>= 0); some simulators need time to re-listen after disconnect. Exit code 0 = all cycles passed; 1 = usage error; 2 = one or more cycles failed.

The Root_ROE2 cycle delay has not been measured yet. Use the required explicit `cycleDelaySeconds` argument after measuring its re-listen behavior; do not assume the XGem value applies.

## 10. Open Items

| Item | Status | Reference |
|------|--------|-----------|
| Equipment Simulator selection | Final HOST-003 local test: Root_ROE2.exe; XGem300Pro retained as preliminary-test history | OI-013 |
| Live TCP connection test | Partial: Selected and disconnect verified; final cleanup fix needs retest | — |
| 100-cycle test with live simulator | Pending final rerun | — |
| Memory profiling under sustained load | Pending final 100-cycle run | — |
| Active/Passive role confirmation | Local simulator confirmed: Host Active / Equipment Passive; production equipment pending | OI-002 |
| SecsGemOptions.DeviceId exact type | Verified: `ushort` (source confirmed) | — |
| Secs4NetAdapter API correctness | Verified: `HsmsConnection(IOptions, ISecsGemLogger)`, `void Start(CancellationToken)` — Debug/Release build and PoC execution confirmed | — |

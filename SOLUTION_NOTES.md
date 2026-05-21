# Solution Notes

## Model

Claude Opus 4.7 (1M context), via Claude Code CLI on WSL2.

## Time Summary

Wall clock from first read of task brief to last commit:

| Milestone                              | Elapsed |
|----------------------------------------|---------|
| Task 1 (cancel policy) — done          | 31:48   |
| Task 2 (file claim) — done             | 38:13   |
| Task 3 (list customer policies) — done | 42:55   |

Budget: 45 minutes. Finished with ~2 min to spare.

## Branch Strategy

The repo's `master` already contained a full reference solution. Worked on a
new branch `solution/opus` cut from the initial commit `e5d3ce3` (the
candidate starting state) so master stayed untouched and the diff is
exactly what was implemented during the session.

## Main Decisions

- **SDK pin fix (`global.json`).** Initial commit pinned `8.0.421/latestFeature`;
  local env only had `8.0.126`. Relaxed to `8.0.100/latestMinor` so restore
  works on any 8.0.x feature band. Committed as a separate `chore:` commit.

- **Reused existing domain primitives.** `CancellationPolicy.ComputeProratedRefund`
  and `RenewalCreditCalculator.ComputeRefundEligibility` were already present
  in the initial commit. Wired them through the handler instead of duplicating
  the math.

- **Waiver wired through `RenewalCreditCalculator`.** Admin-fee waiver
  requires `RenewalCount >= 2` AND no claim within the last 12 months.
  Reused the existing calculator so the rule lives in one place.

- **`IClaimRepository` introduced incrementally.** Added `GetByPolicyIdAsync`
  for task 1, then `AddAsync` for task 2.

- **Logical multi-commit.** One commit per architectural layer per task
  (domain → app+infra → api+tests), so the review can be read top-down.
  Final history: 7 commits, all green at every step.

- **Tests as boundary contracts.** Domain unit tests for the new state
  transition, handler unit tests for the waiver/branching logic, integration
  tests through `WebApplicationFactory<Program>` for HTTP-status-code
  coverage. Avoided overlapping coverage between layers.

- **Pagination clamping in the handler, not the validator.** `page<=0` -> 1,
  `pageSize<=0` -> 20, `pageSize>100` -> 100. Keeps the API forgiving
  without returning 400 on common client mistakes.

- **JSON enum quirk in list-policies test.** API serializes enums as strings
  (`JsonStringEnumConverter`); the default test `HttpClient` does not.
  Fixed by passing a configured `JsonSerializerOptions` to
  `ReadFromJsonAsync` instead of changing API behavior.

## Test Outcome

70 tests pass (47 baseline + 23 added):
- Domain: 41 (+2)
- Application: 9 (+5)
- Integration: 20 (+16)

`dotnet test` clean, `dotnet build` clean, no warnings.

## Out of Scope / Not Done

- No persistence/seed data for filed claims beyond what the unit-of-work
  flushes — there's no GET endpoint for claims yet, only POST.
- No 422 / explicit ProblemDetails shapes; relied on the existing
  `ExceptionHandlingMiddleware` mapping (`DomainException` -> 400/404,
  `InvalidPolicyTransitionException` -> 409, `ValidationException` -> 400).
- No Mapperly mapper for `FileClaimResponse` (small, hand-rolled in the
  handler).

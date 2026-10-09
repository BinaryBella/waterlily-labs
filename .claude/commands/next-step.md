---
description: Build one step of the PLAN.md §2.8 build order, with its tests and checks
argument-hint: <step-number 1-10>
---

Implement **build step $ARGUMENTS** from `docs/PLAN.md` §2.8 (Build order), then verify it.

## 1. Load the step

- Read `docs/PLAN.md` in full and `CLAUDE.md`. Find step $ARGUMENTS in §2.8.
- If `$ARGUMENTS` is missing, isn't a number, or is outside the steps in §2.8, list the steps and stop.
- Restate the step's goal in one or two lines, and list which plan sections define it (for example §2.4 for the database or §4.1 for the cache pseudo code). Treat those sections as the spec for this step.

## 2. Check the prerequisites

- Confirm that the earlier steps (every step before $ARGUMENTS) are in place by checking that their files and types exist, and that `dotnet build` passes if a solution exists.
- If an earlier step is missing or broken, say which one and stop. Don't build on a gap, and don't silently do the earlier step's work.
- If step $ARGUMENTS already looks done, report what exists and ask whether to redo it or fill in the gaps.

## 3. Implement

- Do only step $ARGUMENTS. Don't start on later steps, except for a stub that the build strictly needs, and say so if you add one.
- Follow the plan exactly: names, folders, signatures, cache keys, procedure names and column names. If the plan is ambiguous or doesn't work in practice, pick the reading closest to the plan and **flag it as a deviation** in the report.
- Follow every hard constraint in `CLAUDE.md`. That means no scaffolding or codegen and no new packages beyond Moq, among the others.
- Comment code where the intent isn't obvious, especially delegates, caching and the stored procedure SQL.

## 4. Verify (required, don't skip)

Always run `dotnet build` with zero errors, and look at the warnings. Run `dotnet test` once the test project has tests. The database is SQL Server in the Docker container `mssql_server`, not LocalDB. Run every `sqlcmd` check with the `docker exec` form given in `CLAUDE.md`, so the sa password is never printed. Use only the `ProductManagement` database. Then run the checks for this step:

| Step | Required verification |
|---|---|
| 1 | The solution builds. DI is wired. The connection string goes into `appsettings.json` without the password, and the full string into user-secrets (see `CLAUDE.md` → Database). `dotnet run` starts. |
| 2 | The migration is generated. `dotnet ef database update` succeeds, and the Products table, check constraints and Category index exist (check with `sqlcmd`). |
| 3 | The migrations apply. Run both procedures with `sqlcmd` against the seed data and compare them to results you **calculated by hand** from that seed data (show your working). Confirm that `Down` drops the procedures, by rolling back to `InitialCreate` and then forward again. |
| 4 | The build passes. Exercise each repository method against the Docker SQL Server, including both procedures, and confirm the result records map with no column-name mismatch. |
| 5 | Add `MemoryCacheServiceTests`: the loader runs once across repeated calls, `CachedAsync` sets a 5-minute absolute expiry, and null results aren't cached. |
| 6 | The build passes. Briefly check that the change delegate evicts every key in `PLAN.md` §4.2. Full tests come in step 7. |
| 7 | Write the `ProductServiceTests` cases from `PLAN.md` §2.7 (Arrange/Act/Assert, named `Method_Scenario_ExpectedResult`). All tests pass. |
| 8–9 | Run the app and request the pages with `curl`: 200s, a 404 for a missing product, and POSTs rejected without an anti-forgery token. Check the AJAX partial responses. |
| 10 | Full build and test run, plus a requirement-by-requirement check against `PLAN.md` §1.2 and §1.3. |

If verification fails, fix it and run it again. If you can't make it pass, or the environment can't run it (for example, the `mssql_server` container isn't running; check with `docker ps`), stop and report exactly what failed and how. Never report a check as passed if you didn't run it.

## 5. Report

End with:
- **Done:** the files added or changed, one line each.
- **Verified:** each check you ran and its actual result (test counts, procedure output compared with the expected values).
- **Deviations from the plan:** each one with its reason, or "none".
- **Next:** the step after $ARGUMENTS and anything it needs.

Don't commit.

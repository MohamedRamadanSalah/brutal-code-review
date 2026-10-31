# .NET / ASP.NET Core / EF Core — Crime Checklist

Sweep every item against the code in scope. Each item names the mistake and the reason it hurts, so you can explain the mechanism and not just name the rule. Before you assert framework behavior, check the target framework and package versions in the `.csproj`.

## Contents
1. Async & threading
2. Dependency injection & lifetimes
3. EF Core & data access
4. API design & HTTP
5. Security
6. Errors, logging, observability
7. Types, time, money
8. Background work & resilience
9. Testing smells

---

## 1. Async & threading
- **`async void`** anywhere except event handlers. The caller can't catch its exceptions, so they crash the process or vanish, and the caller can't await completion.
- **Sync-over-async: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.** These block a thread-pool thread, which leads to thread-pool starvation under load and deadlocks in legacy sync contexts.
- **Fire-and-forget `Task`s** (`_ = DoAsync()`, or a call with no `await`). Exceptions get swallowed, and if the task uses a scoped service (DbContext) after the request ends, you get `ObjectDisposedException`.
- **No `CancellationToken`** flowing from the action into EF/HttpClient calls. When a client disconnects, the work keeps burning DB and CPU.
- **`Task.Run` in ASP.NET request code** "to make it async". It just swaps one pool thread for another, adds overhead and solves nothing.
- **Shared mutable state in singletons** (a `Dictionary`, a counter, a `List`) with no synchronization. It's a race, and `Dictionary` can corrupt itself or loop forever when written concurrently.
- **`lock` around `await`.** It won't compile, and people work around it badly. The real tool is `SemaphoreSlim`.
- **Parallel use of one `DbContext`** (`Task.WhenAll` over queries on the same context). DbContext isn't thread-safe, so this throws "A second operation was started…".

## 2. Dependency injection & lifetimes
- **Captive dependency:** a singleton that depends on a scoped service (DbContext, a scoped repo). The scoped instance lives forever, gets shared across requests and goes stale or isn't thread-safe. Check `ValidateScopes` and look at the actual registrations.
- **DbContext registered as singleton**, or held in a static. That's a guaranteed concurrency explosion and an ever-growing change tracker.
- **`new HttpClient()` per call.** It causes socket exhaustion. Use `IHttpClientFactory` or typed clients. A static singleton `HttpClient` ignores DNS changes unless `PooledConnectionLifetime` is set.
- **Service locator** (`IServiceProvider.GetService` sprinkled in business code). It hides dependencies and breaks scope rules.
- **Background services (`IHostedService`/`BackgroundService`) injecting scoped services directly** when they should create a scope with `IServiceScopeFactory`.
- **Options read once into a singleton** when the value is meant to change at runtime (`IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor`).

## 3. EF Core & data access
- **N+1 queries:** looping over entities and touching a navigation property (lazy loading), or querying inside a loop. One page load becomes 500 round trips.
- **Missing `AsNoTracking()`** on read-only queries. The change tracker pays snapshot overhead, and returned entities can get saved by accident.
- **Materializing too early:** `.ToList()` before `.Where()`, `AsEnumerable()` in the middle of a query. This filters the whole table in memory.
- **Selecting full entities** when the code needs three columns. Project with `Select` to a DTO.
- **`SaveChanges` inside a loop.** It means N transactions and N round trips, with partial failure leaving half-written state.
- **Multi-step writes with no transaction**, where one step fails after another has committed and the data ends up inconsistent.
- **SQL injection via `FromSqlRaw` / `ExecuteSqlRaw` with string interpolation or concatenation.** `FromSqlInterpolated` / `FromSql` parameterize, Raw with `$""` does NOT. Check exactly which overload is used.
- **No concurrency token** (`[Timestamp]`/`rowversion`/`IsConcurrencyToken`) on entities edited concurrently. Last write silently wins.
- **Unbounded queries:** no `Take`, no pagination, `Count()` + full load. They work in dev with 10 rows and die in prod with 10M.
- **`Include` chains that cause cartesian explosion.** Consider `AsSplitQuery`.
- **Unique business rules enforced only in code** (`if (!await db.X.AnyAsync(...)) add`) with no unique index. That's a race, see logic-flow.md.
- **Migrations:** a destructive column change with no data migration, a missing index on a new FK or filter column, or a migration that doesn't match the model.
- **Returning `IQueryable` out of the repository.** The query escapes the context's lifetime and gets executed somewhere else, or after disposal.

## 4. API design & HTTP
- **Returning EF entities directly.** It over-posts and over-exposes (password hashes, internal fields), causes cycles in serialization and leaks the schema.
- **Mass assignment:** binding a request straight onto an entity, so the client can set `IsAdmin`, `Price` or `UserId`.
- **Wrong status codes:** 200 with `{ success: false }`, 500 for validation errors, 404 vs 403 leaking existence.
- **No input validation**, or validation only on the client. Look for `[ApiController]` automatic 400s, FluentValidation, range/length checks.
- **Non-idempotent PUT/DELETE**, and POST that creates duplicates on retry with no idempotency key.
- **No pagination or limits on list endpoints. Page size taken from the client without a max.**
- **Breaking contract changes:** renamed or removed fields, changed enum values, changed nullability, with mobile clients still on old versions.
- **Middleware order:** `UseAuthentication` before `UseAuthorization`, CORS before the endpoints, exception handler first.

## 5. Security
- **Missing authorization on an endpoint.** First check for a global `FallbackPolicy`, `RequireAuthorization()` on the route group, or a convention filter. Don't flag it as missing until you've ruled those out.
- **IDOR:** fetching `/orders/{id}` with no ownership check (`order.UserId == currentUserId`). Authentication isn't authorization.
- **Trusting client-supplied identity:** `userId` in the body or query instead of from claims.
- **Secrets in `appsettings.json` or committed code. JWT validation turned off (`ValidateIssuer=false`, `ValidateLifetime=false`), a weak signing key.**
- **`AllowAnyOrigin` together with credentials, or a wildcard CORS in production.**
- **Logging PII, tokens, passwords or full request bodies.**
- **File uploads:** no size/type check, path traversal through a user-supplied filename, saving under wwwroot.
- **Raw exception details or stack traces returned to clients in production.**
- **Rate limiting missing** on login, OTP, coupon or other abuse-prone endpoints.

## 6. Errors, logging, observability
- **`catch (Exception) { }` or catch-and-return-null.** It swallows the failure, and the bug resurfaces three layers away with no trace.
- **`throw ex;` instead of `throw;`**, which destroys the stack trace.
- **Exceptions used for control flow** on hot paths.
- **String-interpolated log messages** (`_logger.LogInfo($"...")`). This loses structured logging and allocates even when the level is disabled. Use templates.
- **No correlation or context in logs**, so a failure in production can't be traced back to a request.

## 7. Types, time, money
- **`double`/`float` for money.** Rounding errors are guaranteed. Use `decimal`, and set the DB column precision.
- **`DateTime.Now` on the server.** Server-local time breaks across time zones and DST. Use `DateTime.UtcNow` / `DateTimeOffset`, and an injectable `TimeProvider` for tests.
- **Comparing `DateTime`s with different `Kind`s. Storing local times with no offset.**
- **Nullable reference types disabled, or `!` sprinkled everywhere** to silence warnings. That's the nullable version of crossing your fingers.
- **Enums persisted as ints and then reordered.** Existing data now means something else.
- **Culture-sensitive parsing or formatting** (`decimal.Parse` with no `CultureInfo.InvariantCulture`). It breaks on Arabic or other locales.

## 8. Background work & resilience
- **Outbound HTTP with no timeout, retry or circuit breaker (Polly / resilience handlers), and retries on non-idempotent calls.**
- **Background jobs that aren't idempotent**, so a re-run double-charges or double-sends.
- **Sending emails, SMS or push notifications inside the DB transaction**, so the message goes out even when the transaction rolls back. Use an outbox.
- **`BackgroundService.ExecuteAsync` that throws** and kills the host (.NET 6+ default behavior), or loops with no delay or cancellation.

## 9. Testing smells
- **Tests that only cover the happy path. No test for the bug's actual scenario.**
- **EF InMemory provider used to "prove" behavior that differs on real SQL** (transactions, constraints, case sensitivity, raw SQL).
- **Mocks asserting implementation details** instead of behavior. Tests that pass when the code is broken.

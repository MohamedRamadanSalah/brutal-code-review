# Logic Flow — Crime Checklist (any stack)

The most expensive bugs are rarely syntax or framework misuse. They're business logic that's wrong under real-world conditions. Walk the flow as a hostile user, a flaky network and two simultaneous requests would.

## How to walk a flow
1. State the **intended rule** in one sentence ("a coupon can be redeemed once per user, only before expiry, only on orders ≥ min amount").
2. List every **state** the entity can be in and every **transition** the code allows. Look for transitions that should be impossible but aren't blocked (cancelled → paid, refunded → refunded again).
3. For each step, ask: *what if this runs twice? at the same time? fails halfway? gets a null / empty / zero / negative / huge / unicode / past / future value? is called by someone who shouldn't?*
4. Follow the **failure paths** as carefully as the success path. Most code is only ever run on the happy path, and that's where it's untested.

## Concurrency & consistency
- **Check-then-act races:** `if (!exists) insert`, `if (balance >= x) balance -= x`, `if (!coupon.IsUsed) use`. Two concurrent requests both pass the check. The fix lives in the database: a unique constraint, an atomic conditional `UPDATE ... WHERE`, optimistic concurrency, or a row lock. Not a nicer `if`.
- **Read-modify-write on counters or stock** without atomicity. You get lost updates and overselling.
- **Partial failure:** step 1 commits, step 2 throws, and nothing compensates. You get money taken with no order, or an order with no payment.
- **External side effects inside or before the transaction** (charge card, send SMS, push) that can't be rolled back. Use an outbox / do it after commit / make it idempotent.
- **Retries without idempotency** (client retry, gateway retry, user double tap, job re-run) produce duplicate records or charges.
- **Cache invalidation:** a write that doesn't invalidate the cached read, or a cache key that ignores user/tenant/locale.

## Data & edge cases
- **Boundaries:** off-by-one in pagination, ranges, `<` vs `<=` on expiry ("expires at midnight": whose midnight?).
- **Empty / null / zero:** empty list → `First()` throws, division by zero, missing optional field, zero quantity, negative amounts slipping past validation.
- **Time:** time zones, DST, server vs device clock, comparing local to UTC, "today" computed on the server for a user in another zone. Expiry checked on the client only.
- **Money:** float arithmetic, rounding at the wrong step, currency mismatch, discounts that push totals negative, tax applied before vs after discount.
- **Uniqueness and identity:** case-insensitive email/phone duplicates, phone number formats (+966 vs 05…), whitespace and unicode normalization.
- **Large inputs:** unbounded lists, huge payloads, N² loops over user-controlled sizes.

## Authorization logic
- **Every read and write scoped to the caller** (owner, tenant, role). Verified on the server, not hidden in the UI.
- **State-dependent permissions:** can a user still edit after the order is approved? Cancel after shipping?
- **Admin-only actions reachable through a different endpoint** that skips the check.

## Contract & integration
- **Client and server disagree** on field names, nullability, enum values, units (cents vs SAR), date format or error format.
- **Old mobile app versions still in the wild:** does this server change break them?
- **Third-party failure modes:** timeouts, partial responses, webhooks arriving twice, out of order, or never.

## Error handling flow
- **Errors swallowed and replaced with defaults** that look like success (`return 0`, `return []`, `return null`).
- **The user is told "success" before the operation actually committed.**
- **Error messages that leak internals, or that are so generic support can't diagnose anything.**

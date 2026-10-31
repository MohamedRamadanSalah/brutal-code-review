---
type: llm
focus: trace
weight: 1
---

The review flags the check-then-act race (TimesUsed / alreadyUsed checks) allowing concurrent double redemption, and recommends a database-level fix such as a unique index on (CouponId, UserId), an atomic conditional update, or a concurrency token.

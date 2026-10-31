# Brutal Review — Mistakes Ledger
<!-- Maintained by the brutal-review skill. One entry per mistake PATTERN, worst habits first. -->

## Check-then-act race on shared state
- **Count:** 2
- **Severity:** Critical
- **Stack:** Logic
- **First seen:** 2026-08-14 · **Last seen:** 2026-09-22
- **Locations:** `backend/Bookings/BookingService.cs:58` (2026-08-14), `backend/Wallet/WalletService.cs:112` (2026-09-22)
- **Lesson:** An `if` in C# cannot protect a shared row from a concurrent request. Enforce it in the database (unique index / atomic conditional UPDATE / concurrency token).

## DateTime.Now on the server
- **Count:** 1
- **Severity:** Major
- **Stack:** .NET
- **First seen:** 2026-09-22 · **Last seen:** 2026-09-22
- **Locations:** `backend/Wallet/WalletService.cs:97` (2026-09-22)
- **Lesson:** Server code uses UTC (`DateTime.UtcNow` / `TimeProvider`), never server-local time.

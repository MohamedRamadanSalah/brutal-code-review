# Brutal Code Review

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![Claude Code](https://img.shields.io/badge/Claude%20Code-skill-D97757)
![.NET](https://img.shields.io/badge/.NET-ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![Flutter](https://img.shields.io/badge/Flutter-Dart-02569B?logo=flutter&logoColor=white)

A no-mercy, **evidence-first** code review skill for [Claude Code](https://claude.com/claude-code),
specialised in **.NET / ASP.NET Core / EF Core** backends and **Flutter / Dart** apps.

It reviews at three levels at once: **the lines** (is the code correct?), **the flow** (does the
business logic hold under concurrency, money and edge cases?) and **the design** (is the
architecture right, and will it survive the next ten changes?). Then it delivers the verdict with
brutal bluntness, so the lesson sticks.

> The brutality has to be earned by evidence. A savage roast that turns out to be wrong is worse
> than useless, so the skill researches first and judges second.

## How it works

| Phase | What happens |
|---|---|
| **0. Scope & ledger** | Works out what's under review (files, diff, PR) and loads your personal mistakes ledger. |
| **1. Recon** | Reads every file in full, traces the flow end to end (route → service → DB, or widget → state → API), checks DI, middleware, config and tests, verifies framework behaviour for your exact versions, and reconstructs the business rules. |
| **2. Prove every hit** | Each finding is pinned to `file:line`, given a concrete failure scenario, and attacked for false positives. Unproven findings are labelled *Suspicion*, never presented as fact. |
| **3. Verdict** | Ranked crimes (🔥 Critical / 🟠 Major / 🟡 Minor), each with the mechanism, how it blows up, and the fix. Plus what you got right, one-line lessons, and a pop quiz. |
| **4. Ledger** | Repeat mistakes are tracked by pattern, and the tone escalates on the 2nd and 3rd offence. |

It is **read-only**: it reviews, explains and teaches, and never edits your code.

## What it catches

Stack checklists live in [`references/`](references):

- **[dotnet.md](references/dotnet.md)**: DI lifetimes and captive dependencies, EF Core tracking and N+1, async pitfalls (`async void`, `.Result`), auth policies, IDOR, SQL injection, money types and transactions.
- **[flutter.md](references/flutter.md)**: `BuildContext` across async gaps, rebuild and `FutureBuilder` traps, controller disposal, state-management misuse, token storage and JSON parsing.
- **[logic-flow.md](references/logic-flow.md)**: invariants, check-then-act races, idempotency, state machines, time zones and edge cases.
- **[architecture.md](references/architecture.md)**: layering, dependency direction, where business rules live, and pattern misuse or over-engineering.

## Install

```bash
# all projects
git clone https://github.com/MohamedRamadanSalah/brutal-code-review ~/.claude/skills/brutal-review

# or one project only
git clone https://github.com/MohamedRamadanSalah/brutal-code-review .claude/skills/brutal-review
```

Then ask Claude Code something like:

```
roast my OrdersController and OrderService, no mercy
is my checkout screen architecture bad? be brutal
```

## Optional: automatic self-review hook

[`hooks/autoreview_hook.py`](hooks/autoreview_hook.py) makes Claude run this skill on **its own**
code changes before it finishes a task. It remembers which source files Claude edited, then blocks
the stop once and asks for a self-review. Add this to `~/.claude/settings.json` (adjust the path):

```json
{
  "hooks": {
    "PostToolUse": [
      { "matcher": "Write|Edit",
        "hooks": [{ "type": "command", "command": "python ~/.claude/skills/brutal-review/hooks/autoreview_hook.py mark" }] }
    ],
    "Stop": [
      { "hooks": [{ "type": "command", "command": "python ~/.claude/skills/brutal-review/hooks/autoreview_hook.py stop" }] }
    ]
  }
}
```

Set `BRUTAL_REVIEW_AUTO=0` to switch it off. Requires Python 3.9+.

## The ledger

Confirmed mistakes are recorded by pattern in `~/.claude/brutal-review/ledger.md` (count, severity,
locations, lesson). Your worst habits rise to the top, and the next review quotes them back at you.
Entries from self-review are tagged `Author: Claude`, so your own record stays honest.

## Evals

[`evals/`](evals) contains three graded test cases built on deliberately broken code:

| Case | Planted problems |
|---|---|
| `dotnet-orders-api` | SQL injection, captive DbContext, IDOR, `async void` + `.Result`, `double` for money, `SaveChanges` in a loop, plus a global auth policy the review must *not* falsely flag |
| `flutter-checkout-screen` | `BuildContext` after `await`, `FutureBuilder` refetch, undisposed controller, token storage, JSON number parsing |
| `coupon-repeat-offender` | A check-then-act coupon race that is already in the ledger, testing repeat-offence escalation |

Each case has a prompt, the input files and one grader per expected behaviour.

## Tone

Profanity is on by design, but it's aimed at the code and the decisions, never at the person.
Every insult rides on a fact, and clean code gets a clean verdict. If you write in Arabic or
another language, the review answers in that language and keeps code terms in English.

## License

[MIT](LICENSE) © 2026 Mohamed Ramadan

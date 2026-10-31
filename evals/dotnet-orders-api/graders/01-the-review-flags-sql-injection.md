---
type: llm
focus: trace
weight: 1
---

The review flags SQL injection in OrderService.SearchAsync because FromSqlRaw is called with an interpolated string containing the user-supplied term.

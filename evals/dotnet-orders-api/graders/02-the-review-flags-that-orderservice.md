---
type: llm
focus: trace
weight: 1
---

The review flags that OrderService is registered as a singleton while depending on the scoped AppDbContext (captive dependency / DbContext shared across requests).

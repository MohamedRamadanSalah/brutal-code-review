# Architecture & Design — Crime Checklist

Architecture crimes don't crash on day one. They make every later change slower, riskier and more expensive. Judge the design against **what this system actually needs** (team size, scale, domain complexity), not against a textbook. Over-engineering is a crime too.

## First, map what exists (recon)
Before judging, draw the real architecture from the code, not from folder names:
1. **Layers / projects and their references** (`.csproj` ProjectReferences, Dart `import`s / packages). Which layer depends on which? Use Grep on `using`/`import` to see what really happens.
2. **Where business rules live**: entities, domain services, application services/handlers, controllers, SQL, widgets, blocs? Find the rule in question and every place it's duplicated.
3. **Boundaries:** modules/bounded contexts, what talks to what (in-process call, HTTP, queue, shared DB).
4. **The project's own conventions:** read `docs/`, ADRs, `CLAUDE.md`, architecture skills or guides in the repo. Judge against the agreed architecture first. A deviation from the project's own rules is a harder hit than a deviation from your taste.

## Layering & dependencies
- **Dependency direction violated:** Domain/Core referencing Infrastructure, EF, ASP.NET or HTTP types; Flutter domain layer importing `package:flutter`, Dio or a concrete repository. The inner layers must not know the outer ones exist.
- **Fat controllers / fat widgets:** business rules, queries and mapping all in the controller action or the widget. Can't be reused, can't be tested, will get copy-pasted.
- **Anemic domain + god service:** entities that are just property bags while a 2,000-line `OrderService` does everything. The invariants have no home, so every caller has to remember them, and one won't.
- **Leaky abstractions:** repository interfaces exposing `IQueryable`, EF entities, `DbSet`, or HTTP status codes leaking into the domain. Flutter UI depending on raw JSON maps or DTOs instead of domain models.
- **Generic repository on top of EF** that adds nothing but hides `Include`/projection/tracking control. Ceremony with negative value.
- **Circular dependencies** between projects, modules or features. Shared "Common/Utils" dumping grounds that everything depends on.
- **Cross-feature reach-ins:** feature A querying feature B's tables or calling its internals instead of its public contract.

## Business logic placement & domain modeling
- **Invariants not enforced where the data changes.** If "an order total can't be negative" or "a booking can't overlap" is only checked in one endpoint, the next endpoint (or admin tool, or background job) breaks it. Rules belong in the domain model, or in one application service that every path goes through, and ideally also in DB constraints.
- **Same rule implemented in two places** (backend and Flutter, or two services) with slightly different logic. They will drift, and nobody will know which is right.
- **Business decisions made on the client** (price, discount, eligibility, permissions) that the server trusts.
- **No explicit state machine** for entities with a lifecycle (order, booking, payment, ticket). Status is a free-for-all `string`/`int` that any code can set to anything, so illegal transitions are possible.
- **Primitive obsession** for domain concepts: money as `decimal` with no currency, phone/email as raw strings validated in 6 places, IDs as interchangeable `int`s (passing a `UserId` where an `OrderId` goes).
- **Magic values** for business rules (`if (status == 3)`, `amount > 500`) with no named constant, configuration or explanation.
- **Business rules hidden in SQL, triggers or stored procedures** that nobody on the team knows exist.

## Boundaries, coupling & cohesion
- **Shotgun surgery:** one business change requires editing 9 files across 5 layers. That shows the abstraction boundaries are drawn in the wrong place.
- **Feature envy / divergent change:** a class that changes for many unrelated reasons, or logic that keeps reaching into another object's data.
- **Shared database between services** used as an integration mechanism: either a distributed monolith or a schema nobody can change.
- **Synchronous call chains across services** for things that could be async. One slow dependency takes down the whole request.
- **Events vs commands confused**, events with no clear owner, or integration events published before the transaction commits.

## Patterns: misuse and cargo cult
- **CQRS/MediatR/Clean Architecture/microservices applied to a CRUD app** with no reason. Five files and three layers of indirection to save a row. Name the cost and what simpler design would do.
- **Pattern with no payoff:** an interface with one implementation and no test seam needed, a factory that builds one thing, a strategy with one strategy.
- **Missing pattern where the pain is real:** a growing `switch` on type/status that should be polymorphism or a state machine, and copy-pasted validation that should be one validator.
- **Flutter state-management soup:** mixing Bloc, Provider, `setState` and global singletons in one feature with no rule for which to use where.

## Cross-cutting concerns
- **Transactions:** whose job is the unit of work? Mixed (`SaveChanges` called in repositories, services and controllers) means no one owns consistency.
- **Validation, auth, logging, error mapping** done ad-hoc in each endpoint instead of filters, middleware, pipeline behaviors, interceptors.
- **Configuration and environment:** hard-coded URLs or feature flags. No separation between dev, staging and prod.
- **Testability:** static calls, `DateTime.Now`, `new HttpClient`, singletons and `GetIt` lookups deep inside logic make the business rules impossible to unit test. If you can't test a rule without a database, the architecture is in the way.

## Evolution & scale
- **Breaking API/contract changes without versioning** while mobile clients in the wild lag behind.
- **Designs that are O(users) per request**, synchronous fan-out, no pagination or caching strategy for read-heavy paths.
- **Irreversible decisions made casually** (data model shape, public API, IDs exposed), and reversible ones agonized over.

## How to report architecture crimes
Architecture findings need the same proof as bugs: point at the concrete files and dependencies, then show the **concrete cost**. That might be "to add a new payment method you must touch X, Y, Z", "this rule is enforced in A but not B, so path B can create state S", or "this test needs a real DB because…". Then give the smallest refactor that would fix the direction, not a rewrite fantasy. If the current design is the right size for the problem, say so.

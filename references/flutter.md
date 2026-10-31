# Flutter / Dart — Crime Checklist

Sweep every item against the code in scope. Check the Flutter/Dart SDK and package versions in `pubspec.yaml` / `pubspec.lock` before you assert version-specific behavior (lint names, `context.mounted`, Riverpod 2 vs 3 APIs, Bloc 8+ event handlers).

## Contents
1. Lifecycle & async
2. Build method discipline
3. State management
4. Networking, parsing, errors
5. Null safety & Dart idioms
6. Performance & layout
7. Navigation
8. Security & storage
9. i18n, RTL, accessibility
10. Testing smells

---

## 1. Lifecycle & async
- **`BuildContext` used after an `await`** without checking `mounted` / `context.mounted`. The widget may be gone, so you get `Navigator`/`ScaffoldMessenger`/`Theme` lookups on a dead element. That means crashes, or the wrong route getting popped.
- **`setState` after `dispose`**: an async callback, timer or stream listener completes after the user has left the screen and throws "setState() called after dispose()".
- **Controllers not disposed:** `TextEditingController`, `AnimationController`, `ScrollController`, `PageController`, `FocusNode`, `StreamSubscription`, `Timer`. That's leaked memory and listeners firing into dead state.
- **`AnimationController` without `vsync`**, or a `TickerProviderStateMixin` misused.
- **Un-awaited futures** whose errors vanish (enable `unawaited_futures` / `discarded_futures`). Fire-and-forget saves that the UI reports as "done".
- **Async work in `initState`** that calls `setState` with no mounted guard, or uses `context` for inherited widgets in `initState` (use `didChangeDependencies`).
- **Missing error handling on futures and streams:** no `catchError` / `try`, no `onError` on `listen`. One exception and the screen spins forever.

## 2. Build method discipline
- **Side effects in `build()`:** API calls, `setState`, navigation, analytics, logging. `build` can run 60 times a second, and it will.
- **`FutureBuilder(future: fetchData())` creating the future inside `build`.** Every rebuild refires the request. Create it once in `initState`, or in the state layer.
- **`StreamBuilder` with a stream created in `build`.** Same disease.
- **Huge `build` methods** with logic, formatting and conditional soup. Extract widgets (classes, not helper methods that return widgets, so they get their own element and const-ability).
- **Missing `const` constructors** on static subtrees, causing pointless rebuild work.
- **`setState` wrapping the entire screen** when one counter changed. That's a rebuild storm.

## 3. State management
- **Business logic in widgets** that belongs in a Bloc/Cubit/Notifier/ViewModel. It can't be tested and gets duplicated across screens.
- **Bloc/Cubit `emit` after `close`**, or after an `await` once the bloc has closed (check `isClosed`). Events processed concurrently when they should be sequential (`transformer: sequential()` / `droppable()` for double submits).
- **Mutable state objects:** mutating a list inside the state and emitting the same instance. Equatable says "no change", so the UI never updates. Copy with `copyWith` / new lists.
- **Riverpod:** `ref.read` in `build` where `ref.watch` is needed (stale UI), `ref.watch` inside callbacks (wrong), missing `autoDispose` on screen-scoped providers (leaks and stale cache), using `ref` after `await` once disposed.
- **Provider:** `Provider.of(context)` without `listen: false` in callbacks, or providers created in the wrong place in the tree, so they get recreated on rebuild.
- **The same state duplicated in two sources of truth** (local `setState` and a bloc). They will disagree.

## 4. Networking, parsing, errors
- **JSON parsed with raw `dynamic` maps and casts** (`json['price'] as double`). When the backend sends `10` (an int), you get a crash. Use `num` → `toDouble()`, `json_serializable` / `freezed`, and null handling.
- **The client–server contract doesn't match the backend DTOs:** field names and casing, nullable fields assumed non-null, enum strings, date format and timezone (parse UTC, display local).
- **No timeout on HTTP / Dio calls. No cancellation when leaving the screen** (Dio `CancelToken`).
- **All errors collapsed into one "Something went wrong"**, with no distinction between network, 401, 4xx validation and 5xx. A 401 should trigger refresh or logout, not a generic snackbar.
- **Token refresh race:** several parallel 401s each triggering a refresh. That's a refresh storm, and an invalidated refresh token logs the user out. You need a single-flight lock in the interceptor.
- **Double submit:** a button that isn't disabled while the request is in flight, so you get duplicate orders or payments. Pair it with backend idempotency.
- **Loading/error/empty states not handled.** A list screen with no empty state looks broken.

## 5. Null safety & Dart idioms
- **`!` bang abuse.** Each one is a crash you scheduled for later. Ask why the value can be null, and handle it.
- **`late` fields that might never be initialized** → `LateInitializationError`. `late` used to dodge thinking about nullability.
- **`dynamic` everywhere**, which throws away the type system.
- **`==`/`hashCode` not overridden on value classes used in state comparisons** (or no Equatable/freezed).
- **Catching `Exception` but not `Error`, or catching everything with `catch (e)` and printing it.**
- **`print` in production code.** Use a logger. Prints also leak data in release logs on some platforms.

## 6. Performance & layout
- **`ListView(children: [...])` for long or unbounded lists.** Use `ListView.builder`.
- **`shrinkWrap: true` with `NeverScrollableScrollPhysics`** inside a scroll view, for big lists. That's O(n) layout. Use slivers.
- **Missing `Key`s on reorderable or stateful list items.** State jumps to the wrong row.
- **Heavy synchronous work on the UI isolate** (large JSON decode, image processing, crypto). Use `compute` / `Isolate.run`.
- **Network images with no caching or sizing** (`cacheWidth`/`cacheHeight`, `cached_network_image`). That means memory blowups on long feeds.
- **`Opacity`/`ClipRRect`/`saveLayer`-heavy widgets in animations or lists.**
- **`MediaQuery.of(context)` for one property.** Use `MediaQuery.sizeOf` and similar, so the widget doesn't rebuild on every inset change.

## 7. Navigation
- **Navigating inside `build`.** Pushing routes twice on double tap.
- **Passing huge objects or mutable state through route arguments.** Deep links that can't rebuild state from IDs alone.
- **Back-button / `PopScope` handling that traps users or skips cleanup.**
- **Auth guards only in the UI.** Deep links or the restored route stack bypass them.

## 8. Security & storage
- **Access/refresh tokens in `SharedPreferences`** (plain text). Use `flutter_secure_storage`.
- **Secrets or API keys hard-coded in Dart.** They ship in the binary, and anyone can extract them.
- **Trusting client-side checks** (price, discount, role) the backend doesn't re-verify.
- **Logging tokens, OTPs or personal data.**
- **Certificate validation disabled** (`badCertificateCallback => true`) left in from debugging.

## 9. i18n, RTL, accessibility
- **Hard-coded user-facing strings** instead of l10n/ARB.
- **`EdgeInsets.only(left:)` / `Alignment.centerLeft` / `Positioned(left:)`** where directional versions (`EdgeInsetsDirectional`, `AlignmentDirectional`, `PositionedDirectional`) are needed. They break Arabic/RTL layouts.
- **Icons that should mirror in RTL** (arrows, back) and don't.
- **String concatenation for sentences** (word order differs by language), and numbers/dates formatted without `intl`.
- **Tap targets < 48dp, no `Semantics` labels on icon buttons, text that ignores text scaling and overflows.**

## 10. Testing smells
- **No widget test for the flow that broke. Tests that `pump` without `pumpAndSettle`, or mock away the very logic under test.**
- **Golden tests used as the only check of logic.**

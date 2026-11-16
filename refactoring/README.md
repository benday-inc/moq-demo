# Refactoring: the first test

**The question this answers:** How do I unit test code that creates its own dependencies?

This demo stands on its own. It's about refactoring legacy code, not about
mocking, and the numbered demos (`01`-`07`) don't build on it or use its
technique.

## Goal

Get one passing unit test around `PersonManager.SaveAsync` without a database
and without redesigning anything. This is the legacy-code foothold: the code
creates its own data access with `new` and reads the real clock, so there's
nothing to inject a mock into. Once one test exists, it's the safety net for
whatever refactoring comes next.

## How

Extract-and-override, from Michael Feathers' *Working Effectively with Legacy
Code*. Move each hard-wired dependency into a `protected virtual` method (a
"seam"), which is a one-line, low-risk change. Then the test builds a
*partial mock* of `PersonManager`: a `Mock<PersonManager>` with
`CallBase = true` runs all the real code except the seams, which the test
overrides with `Protected().Setup(...)`. This is scaffolding, not the way to
write new code. The numbered demos show where you're headed: constructor
injection, with no seams and no partial mocks.

## What changes

**before** is the inherited app. `PersonManager` news up `PersonDataAccess`
inline, reads `DateTime.UtcNow` directly, and logs with `new Logger()`. It has
no interfaces, no constructor parameters and no tests. It works end to end
against SQL Server.

**after** makes the smallest production change that creates a seam, then
mocks through it:

- `PersonManager` gets two `protected virtual` seams, `CreateDataAccess()` and
  `GetUtcNow()`, and every `new PersonDataAccess()` and `DateTime.UtcNow` in it
  goes through them.
- `PersonDataAccess` methods become `virtual` so Moq can override them.
- A new `Benday.MoqDemo.UnitTests` project builds a partial mock
  (`new Mock<PersonManager> { CallBase = true }`) and overrides both seams with
  `Protected().Setup(...)`.

The tests that prove it:

- `SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow`: the clock
  seam returns a fixed date, and both timestamps match it.
- `SaveAsync_WhenPersonIsNew_SavesThroughDataAccess`: the mocked data access
  received exactly one `SavePersonAsync` call.

Take the `CreateDataAccess()` setup away and both tests fail with
"The ConnectionString property has not been initialized": the real data
access tried to reach SQL Server. Demo `02-data-access` removes that
dependency for good.

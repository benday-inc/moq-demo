# 01: The first test

**The question this answers:** How do I unit test code that creates its own dependencies?

## Goal

Get one passing unit test around `PersonManager.SaveAsync` without a database
and without redesigning anything. This is the legacy-code foothold: the code
creates its own data access with `new` and reads the real clock, so there's
nothing to inject a mock into. Once one test exists, it's the safety net for
the refactoring in the stages that follow.

## How

Extract-and-override, from Michael Feathers' *Working Effectively with Legacy
Code*. Move each hard-wired dependency into a `protected virtual` method (a
"seam"), which is a one-line, low-risk change. Then the test builds a
*partial mock* of `PersonManager`: a `Mock<PersonManager>` with
`CallBase = true` runs all the real code except the seams, which the test
overrides with `Protected().Setup(...)`. This is scaffolding, not the way to
write new code; by stage 06 every seam is gone.

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
access tried to reach SQL Server. That's the dependency the rest of the
series removes.

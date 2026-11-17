# 06: What time is it?

**The question this answers:** How do I unit test code that uses `DateTime.Now`?

## Goal

Make time an input. `PersonManager` stamps `CreateDate` and `LastModified`
with `DateTime.UtcNow`, so a test can't know what value to expect. This is the
"data scenario" reason to mock: you need a dependency to hand back one
specific answer.

## How

Pass `TimeProvider` (built into .NET) through the constructor and read the
clock from it. Then show two test clocks:

- A Moq mock: `Mock<TimeProvider>` with `Setup(x => x.GetUtcNow()).Returns(now)`.
  It works because `TimeProvider` is an abstract class with virtual members.
- `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`, a
  ready-made fake that can also move time forward with `Advance()`.

## What changes

**before** is demo 06's `after`, plus the
`Microsoft.Extensions.TimeProvider.Testing` package in the unit test project.

**after** injects the clock:

- `PersonManager(repository, logger, timeProvider)`; every `DateTime.UtcNow`
  becomes `_timeProvider.GetUtcNow().UtcDateTime`. The console app passes
  `TimeProvider.System`.
- `PersonManagerFixture` gets a third mock, `Mock<TimeProvider>`, and a fixed
  `Now`.

The tests that prove it:

- `SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow`
- `SaveAsync_WhenPersonExists_UpdatesLastModifiedButNotCreateDate`: this was
  `..._DoesNotChangeCreateDate` from demo 03 on. With the clock pinned it can check
  `LastModified` too.
- `SaveAsync_WhenTimeMovesForward_UpdatesLastModified`: `FakeTimeProvider`
  with `Advance(TimeSpan.FromHours(1))` between two saves.

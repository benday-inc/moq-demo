# 03: Inject, then verify

**The question this answers:** How do I verify a method was called with Moq?

## Goal

Prove that failures get logged, and that successes don't. Stage 02 couldn't
check this, because `PersonManager` created its logger with `new` inside each
catch block.

## How

Inject one dependency: extract an `ILogger` interface, pass it through
`PersonManager`'s constructor, and have the console app pass the real
`Logger`. Tests pass a `Mock<ILogger>` and check behavior with `Verify`:
`Times.Once` with the exact exception that was thrown, and `Times.Never` on the
success path. The data access stays on the stage 01 seam on purpose; this
stage changes one dependency, not all of them.

## What changes

Stage 02 could prove the disk-space handler ran, but not that anything was
logged, because `PersonManager` creates `new Logger()` inside each handler.

**before** is stage 02's `after`, plus an empty `Benday.MoqDemo.Common` project
that Business already references, so the video skips the project setup.

**after** injects the logger through the constructor and verifies behavior:

- `ILogger` goes in Common, and `Logger` implements it.
- `PersonManager(ILogger logger)` null-checks the logger and stores it. Both
  `new Logger().WriteToLog(ex)` calls become `_logger.WriteToLog(ex)`.
- The console app builds `new PersonManager(new Logger())`.
- The fixture passes a `Mock<ILogger>` to the partial mock's constructor:
  `new Mock<PersonManager>(Logger.Object) { CallBase = true }`.

The tests that prove it:

- `SaveAsync_WhenPersonIsNew_DoesNotLog`: `It.IsAny<Exception>()` with
  `Times.Never`.
- `SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException`: the exception
  that was thrown is the one that got logged, `Times.Once`.
- `SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException`: the same check
  for the generic handler.

The data access is still reached through stage 01's partial mock. That's
deliberate: this stage injects one dependency, not all of them.

Here's the stage 02 version of the partial mock, before the constructor
argument:

```csharp
_partialMock = new Mock<PersonManager> { CallBase = true };
```

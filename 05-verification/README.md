# 05: Did it call the right stuff?

**The question this answers:** How do I verify a method was called with Moq?

## Goal

Test requirements that aren't return values. "When the save fails, the error
gets logged" is a rule about an interaction: did `PersonManager` call the
right thing, with the right argument, the right number of times?

## How

First make the collaborator replaceable: extract `ILogger` from `Logger` and
pass it through `PersonManager`'s constructor. Then use `Verify`:

- `Verify(..., Times.Once)` proves a call happened.
- `Verify(..., Times.Never)` proves it didn't.
- Passing a specific argument instead of `It.IsAny<T>()` proves it happened
  with the value that matters.

## What changes

**before** is demo 04's `after`, unchanged.

**after** injects the logger:

- `ILogger` is extracted into Common, and `Logger` implements it.
- `PersonManager(repository, logger)`; the inline `new Logger()` calls become
  `_logger.WriteToLog(ex)`. The console app passes `new Logger()`.
- `PersonManagerFixture` gets a second mock, `Mock<ILogger>`.

The tests that prove it:

- `SaveAsync_WhenPersonIsNew_SavesThroughTheRepository`: `Times.Once` on the
  repository.
- `SaveAsync_WhenPersonIsNew_DoesNotLog`: `Times.Never`.
- `SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException`: the exception
  that was thrown is the one that got logged.
- `SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException`

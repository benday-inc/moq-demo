# 02: Testing the impossible

**The question this answers:** How do I test a catch block I can't trigger?

## Goal

Test the code that runs when SQL Server is out of disk space. Nothing in the
app can produce that failure, and producing it for real would mean starting
SQL Server and filling its disk. This is the "practically impossible to test
any other way" reason to mock.

## How

Use the data access mock from stage 01 and tell it to fail: `ThrowsAsync(...)`
makes `SavePersonAsync` throw the exception on demand. Then verify the handler
ran (`Protected().Verify(..., Times.Once())`), that the original exception is
rethrown, and that a different exception goes to the other handler
(`Times.Never()`).

## What changes

`PersonManager.SaveAsync` has a handler for `SqlServerOutOfDiskSpaceException`,
but nothing in the app ever throws one. Without a mock, the only way to test
that handler is a utility that starts SQL Server and fills the disk with
garbage.

**before** is stage 01's `after`, unchanged.

**after** makes the mocked data access throw the conditions you can't produce:

- `FixDiskSpaceProblem()` goes from `private` to `protected virtual`, so the
  partial mock can verify it was called.
- `DataAccess.Setup(...).ThrowsAsync(...)` makes `SavePersonAsync` throw.
- `PartialMock.Protected().Verify("FixDiskSpaceProblem", ...)` checks the
  handler ran (`Times.Once()`), or didn't run (`Times.Never()`).

The tests that prove it:

- `SaveAsync_WhenDatabaseIsOutOfDiskSpace_FixesDiskSpaceProblem`
- `SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException`: it's the same
  exception instance, not a wrapped copy.
- `SaveAsync_WhenSaveFailsForAnotherReason_DoesNotFixDiskSpaceProblem`: the
  generic handler runs instead.

`Logger` is still created with `new` inside the handlers, so these tests can't
check that anything was logged. Stage 03 fixes that.

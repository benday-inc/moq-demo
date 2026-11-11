# 03: Testing the impossible

**The question this answers:** How do I test a catch block I can't trigger?

## Goal

Test the code that runs when SQL Server is out of disk space. Nothing in the
app can produce that failure, and producing it for real would mean starting
SQL Server and filling its disk. This is the "you can't produce the condition"
reason to mock.

## How

Tell the repository mock to fail: `Setup(...).ThrowsAsync(...)` makes
`SaveAsync` throw the exception on demand. Then assert what the business said
should happen, which here is that the original exception comes back out.

## What changes

`PersonManager.SaveAsync` has a handler for `SqlServerOutOfDiskSpaceException`,
but nothing in the app ever throws one.

**before** is demo 02's `after`, unchanged.

**after** changes only the tests. No production code changes at all:

- `SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException`: it's the same
  exception instance, not a wrapped copy.
- `SaveAsync_WhenSaveFailsForAnotherReason_RethrowsTheException`: the generic
  handler gets the same treatment.

`Logger` is still created with `new` inside the handlers, so these tests can't
check that anything was logged. You'll see the log lines go by in the test
output instead. Demo 04 fixes that.

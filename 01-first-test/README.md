# 01: The first test

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

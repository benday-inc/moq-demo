# 05: Faking the clock

**The question this answers:** How do I unit test code that uses `DateTime.Now`?

## Goal

Make "now" something a test controls, using the clock abstraction .NET
already ships, and get it out of the partial mock before stage 06 removes the
partial mock entirely.

## How

Replace the `GetUtcNow()` seam with `TimeProvider` through the constructor.
The console app passes `TimeProvider.System`. Tests pass a `FakeTimeProvider`
(from `Microsoft.Extensions.TimeProvider.Testing`), which is fixed at a known
time and can move forward with `Advance()`. One test shows the Moq
alternative, `Mock<TimeProvider>`, which works because `GetUtcNow()` is
virtual.

## What changes

Since stage 01, tests have pinned the time by overriding a
`protected virtual GetUtcNow()` seam on a partial mock. Stage 06 removes the
partial mock, and that seam would go with it. So the clock moves into the
constructor first.

**before** is stage 04's `after`, plus a reference to
`Microsoft.Extensions.TimeProvider.Testing` in the test project.

**after** injects .NET's `TimeProvider`:

- `PersonManager(ILogger logger, TimeProvider timeProvider)` null-checks and
  stores it. `GetUtcNow()` is deleted, and its three callers use
  `_timeProvider.GetUtcNow().UtcDateTime`.
- The console app passes `TimeProvider.System`.
- The fixture's default clock is a `FakeTimeProvider` fixed at
  2026-11-17 09:00 UTC, passed to the partial mock's constructor.

The tests that prove it:

- `SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow`: the same
  test as before, now reading the expected time from the fake clock.
- `SaveAsync_WhenPersonExists_UpdatesLastModifiedButNotCreateDate`: calls
  `Clock.Advance(TimeSpan.FromDays(1))` between creating and saving, which a
  hand-rolled seam can't do.
- `SaveAsync_WithAMockedTimeProvider_SetsCreateDateToNow`: the Moq
  alternative. `TimeProvider` is an abstract class with a virtual
  `GetUtcNow()`, so `Mock<TimeProvider>` works too. `FakeTimeProvider` is less
  setup, and it can move time forward.

Here's the stage 04 version of the first test:

```csharp
[Fact]
public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
{
    var expected = new DateTime(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);
    PartialMock.Protected().Setup<DateTime>("GetUtcNow").Returns(expected);
    var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

    await SystemUnderTest.SaveAsync(person);

    person.CreateDate.ShouldEqual(expected, "CreateDate should come from the clock seam");
    person.LastModified.ShouldEqual(expected, "LastModified should come from the clock seam");
}
```

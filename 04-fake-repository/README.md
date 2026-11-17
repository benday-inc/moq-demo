# 03: Mock or fake?

**The question this answers:** Should I mock my repository, or use an in-memory fake?

## Goal

Know when Moq is the wrong tool. A `Mock<T>` is a scripted answer: you tell it
what to return and it returns that, with no memory of anything else. That is
exactly right when a test needs one specific answer (person 2 doesn't exist,
the save failed). It gets awkward when the test cares about **state across
calls**: save, then load, and expect the thing you saved. Scripting that with
a mock means re-implementing the database inside the test.

## How

Use a **fake**: a real, working implementation with a shortcut.
`InMemoryRepository<PersonEntity>` from `Benday.EfCore.Testing` implements the
same `IAsyncReadableRepository<PersonEntity, int>` as `SqlPersonRepository`,
with a `List<T>` instead of a table. `PersonManager` can't tell the difference.
It assigns sequential Ids, exposes the stored records as `Items`, and tracks
which methods were called (`WasSaveCalled`, `SaveArgumentValue`, and so on).

The rule of thumb the episode lands on:

- **Mock** when you need an answer you can't produce for real: null, an
  exception, a specific time, "the validator said no."
- **Fake** when you need state: round trips, updates, more than one call.
- Verify behavior with a mock; verify data with a fake.

A fake is not magic. `InMemoryReadableRepository<T, TKey>` in
`Benday.Common.Testing` is the thirty-line version; the `Benday.EfCore.Testing`
one adds Id assignment and call tracking.

## What changes

**before** is demo 03's `after`, plus the `Benday.EfCore.Testing` package in
the unit test project.

**after** changes only the tests. No production code changes at all.

The round trip written with a mock first, on camera, to show the cost. It
needs a `Callback` to capture the entity and stamp an Id, and a second `Setup`
to hand it back:

```csharp
PersonEntity? stored = null;
Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>()))
    .Callback<PersonEntity>(entity => { entity.Id = 1; stored = entity; });
Repository.Setup(x => x.GetByIdAsync(1))
    .ReturnsAsync(() => stored);
```

That's a database with two rows of code. It stays in the video and this
README; the fixture keeps the fake version.

The tests that prove it, added to `PersonManagerFixture`:

- `LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson`: the same name as the
  integration test, because it's the same test. SQL Server in one project, a
  `List<T>` in the other.
- `SaveAsync_WhenCalledTwice_UpdatesTheStoredPerson`: two saves of one person
  leave one record in `Items`, with the update applied and `CreateDate`
  untouched. That's the "state across calls" test a mock makes you earn.

`PersonManagerIntegrationFixture` already has
`LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson` against SQL Server. Same
assertions, three fidelities: a real database, a fake, and (in the README
above) a scripted mock.

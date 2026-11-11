# 01: Don't mock the adapter

**The question this answers:** How do I unit test code that calls the database? Part 1: should I mock my mapping code?

## Goal

Start from an inherited, untested app and get its first tests in place
without any mocks at all. `PersonManager` copies fields between `Person` (what
the app works with) and `PersonEntity` (what the database stores) by hand.
That copying is where data bugs hide, and it's pure logic with no
dependencies, so it should be tested with real objects. This is also the
setup for the next demo: once the two shapes have a tested converter between
them, the database can go behind an interface.

## How

Extract the hand-copying from `PersonManager` into
`PersonToEntityAdapter : AdapterBase<Person, PersonEntity>` (from
`Benday.EfCore`). `PersonManager` creates it with `new`; it isn't injected,
because a mocked adapter would silently copy nothing. The adapter's tests use
`FakeValueGenerator` to fill a source object, adapt it, and check every field
arrived. There's no Moq in this demo; the unit test project doesn't even
reference it yet.

## What changes

**before** is the inherited app. `PersonManager` news up `PersonDataAccess`
inline, reads `DateTime.UtcNow` directly, logs with `new Logger()`, and
copies fields by hand in three places. It works end to end against SQL
Server. The setup the video skips is already there: `Benday.EfCore` is
referenced, `PersonEntity` derives from `EntityBase`, `Person` derives from
`DomainModelBase`, and an empty `Benday.MoqDemo.UnitTests` project holds
`TestConstants.cs`. `dotnet test` reports "Zero tests ran" until the first
test exists.

**after** extracts the copying into an adapter:

- `PersonToEntityAdapter` in Business implements the two `PerformAdapt`
  overrides.
- The database owns `PersonEntity.Id`, so Person-to-entity never copies it.
  Entity-to-Person does.
- `CopyToEntity` and the hand-built `Person` in `LoadByIdAsync` are gone.
- `PersonManager` still creates its own data access, logger and clock. Those
  are the next four demos.

The tests that prove it, in `PersonToEntityAdapterFixture`, with no mocks:

- `Adapt_FromPersonToEntity_CopiesTheFields`
- `Adapt_FromPersonToEntity_DoesNotCopyTheId`
- `Adapt_FromEntityToPerson_CopiesTheFieldsAndTheId`

`FakeValueGenerator.Create<T>()` fills in the source object, so a field that
isn't copied fails with a message like
"Notes should be copied -- Expected: 'Notes_0'. Actual: <empty string>".

# 04: Don't mock the adapter

**The question this answers:** Should I mock my mapping code?

## Goal

Show when *not* to mock. The field-copying between `Person` and
`PersonEntity` is where data bugs hide, and it's pure logic with no
dependencies, so it should be tested with real objects.

## How

Extract the hand-copying from `PersonManager` into
`PersonToEntityAdapter : AdapterBase<Person, PersonEntity>` (from
`Benday.EfCore`). `PersonManager` creates it with `new`; it isn't injected,
because a mocked adapter would silently copy nothing. The adapter's tests use
`FakeValueGenerator` to fill a source object, adapt it, and check every field
arrived. No mocks at all. The existing `PersonManager` tests pass unchanged,
which shows the extraction didn't change behavior.

## What changes

`PersonManager` copies fields between `Person` and `PersonEntity` by hand, in
two directions, in three places. That's logic worth testing, and it's the
kind you test with real objects, not mocks.

**before** is stage 03's `after`, plus setup the video skips: Business and
DataAccess reference `Benday.EfCore` 11.0.11-alpha, `PersonEntity` derives
from `EntityBase`, and `Person` derives from `DomainModelBase`. `EntityBase`
adds no mapped columns, so the database doesn't change.

**after** extracts the copying into an adapter:

- `PersonToEntityAdapter : AdapterBase<Person, PersonEntity>` in Business
  implements the two `PerformAdapt` overrides.
- The database owns `PersonEntity.Id`, so Person-to-entity never copies it.
  Entity-to-Person does.
- `PersonManager` creates the adapter with `new` rather than taking it through
  the constructor. It's pure logic, so use the real one: a mocked adapter
  would silently copy nothing.
- `CopyToEntity` and the hand-built `Person` in `LoadByIdAsync` are gone.

The tests that prove it, in `PersonToEntityAdapterFixture`, with no mocks:

- `Adapt_FromPersonToEntity_CopiesTheFields`
- `Adapt_FromPersonToEntity_DoesNotCopyTheId`
- `Adapt_FromEntityToPerson_CopiesTheFieldsAndTheId`

`FakeValueGenerator.Create<T>()` fills in the source object, so a field that
isn't copied fails with a message like
"Notes should be copied -- Expected: 'Notes_0'. Actual: <empty string>".

`PersonManagerFixture` doesn't change. Its 8 tests still pass, which shows the
extraction didn't change behavior.

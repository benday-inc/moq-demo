# 07: The validator and the bool trap

`PersonManager` saves anything it's given. This stage adds validation, and
it springs the most common surprise in auto-mocked tests on purpose.

**before** is stage 06's `after`, plus setup the video skips: the real rules
in `PersonValidator : IValidatorStrategy<Person>` (first name, last name, and
an email address with an `@`), and `PersonValidatorFixture`, which tests
those rules with real `Person` objects and no mocks.

**after** passes the validator through the constructor:

- `PersonManager` takes `IValidatorStrategy<Person>` and throws
  `InvalidObjectException` before saving when it returns `false`. The console
  app passes `new PersonValidator()` and prints the message for an invalid
  person.

**The trap.** Add only that constructor parameter, run the tests, and all 8
`PersonManagerFixture` tests fail with
`InvalidObjectException : Person is not valid.` `MockUtility` auto-mocked the
new validator, and a Loose mock returns `default(bool)`, which is `false`.
Every person is suddenly invalid, including the ones the tests never meant to
validate.

The fix is one setup where the fixture builds the system under test, so
every test starts from "valid":

```csharp
_systemUnderTest = MockUtility.CreateInstance<PersonManager>();

// A Loose mock returns false for bool, so every person would be invalid.
_systemUnderTest.GetRequiredMock<IValidatorStrategy<Person>>()
    .Setup(x => x.IsValid(It.IsAny<Person>()))
    .Returns(true);
```

The tests that prove it:

- `SaveAsync_WhenPersonIsInvalid_DoesNotCallRepository`: overrides the
  default with `Returns(false)`, expects `InvalidObjectException`, and
  verifies `SaveAsync` was called `Times.Never`.
- The 8 existing tests pass again.
- `LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson` passes
  `FakeValidatorStrategy<Person>` from `Benday.EfCore.Testing`, which defaults
  to `true`. That's the contrast: a fake carries a sensible default, while a
  mock returns whatever `default(T)` is.
- `PersonValidatorFixture` (from `before`): 4 tests of the real rules.

At the end of this stage there are no `virtual` seams, no partial mocks, and
no `DateTime.UtcNow` anywhere in Business.

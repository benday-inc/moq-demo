# Extra: the setup tax

**The question this answers:** Why are my Moq test fixtures so wordy?

This folder is not a stage. It sits outside the `01`-`07` before/after chain
(`prepare-demo.ps1` copies it as-is) and holds the six-dependency example from the
VST08 talk's slides "Annoyance #1: It Gets Wordy" and "MockUtility: Name Only
What You Need", so every line on those slides compiles and passes.

## Goal

Show the cost of hand-wiring mocks when a class has enough dependencies for it
to hurt. `PersonManager` in demos 06-07 has three or four, which is too few
to make the point.

## How

`PersonRegistrationService` takes six constructor dependencies: the
repository, `IDuplicateChecker`, `IEmailSender`, `IAuditLog`, `ILogger` and
`TimeProvider`. `RegisterAsync(Person)` saves a new person and sends a welcome
email. Two fixtures hold the identical test,
`RegisterAsync_WhenPersonIsNew_SendsWelcomeEmail`:

- `PersonRegistrationServiceMoqOnlyFixture.cs`: six `Mock<T>` fields and a
  `CreateSystemUnderTest()` method that passes every `.Object`.
- `PersonRegistrationServiceFixture.cs`: `MockUtility.CreateInstance<T>()`
  builds all six, and the test asks for the one it needs with
  `GetRequiredMock<IEmailSender>()`.

The test works with no setup at all because Loose mocks return defaults:
`IsDuplicateAsync` returns `false` ("not a duplicate"), and every `Task` method
returns a completed task.

## Build and test

From this folder: `dotnet build`, then `dotnet test`. No SQL Server needed.

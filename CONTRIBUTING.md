# Contributing

Thank you for your interest in contributing to Majo.Command!

Majo.Command is a lightweight command-tree library for .NET. Bug fixes, documentation improvements, tests, and feature suggestions are welcome.

## Development Requirements

- .NET 10 SDK
- Git

The library and its test project currently target .NET 10.

## Build

Clone the repository, then restore and build the solution:

```bash
git clone https://github.com/sweetymajo/Majo.Command.git
cd Majo.Command

dotnet restore Majo.Command.slnx
dotnet build Majo.Command.slnx
```

If restore reports a missing local NuGet source directory, create `artifacts/nuget` and try again.

## Testing

`Majo.Command.Test` contains automated tests using xUnit v3. Run them with:

```bash
dotnet test Majo.Command.Test/Majo.Command.Test.csproj -p:UsePackageReference=false
```

Tests cover command registration, matching, argument parsing, execution, help generation, and concurrent invocations.

When a change affects existing behavior, adding or updating a relevant test is appreciated. The tests normally run entirely in memory, without an application database or external services.

## Local NuGet Package Validation

The test project supports two ways to reference Majo.Command:

- `UsePackageReference=false` (default): reference the library project directly during development.
- `UsePackageReference=true`: test against a locally built NuGet package.

For changes involving package contents, dependencies, or consumer-facing build behavior, you can validate the packaged library with:

```bash
dotnet pack Majo.Command/Majo.Command.csproj -c Release

dotnet restore Majo.Command.Test/Majo.Command.Test.csproj -p:UsePackageReference=true --force

dotnet test Majo.Command.Test/Majo.Command.Test.csproj -c Release --no-restore -p:UsePackageReference=true
```

Package-mode testing uses the local NuGet source configured in the repository's `NuGet.config`, under `artifacts/nuget`.

## Pull Requests

Please describe the problem or improvement your contribution addresses and summarize the changes you made.

Before opening a pull request, please build the solution and run the relevant tests. If your change affects the public API or documented behavior, update the relevant documentation where useful.

Focused pull requests are easier to review. Questions and suggestions can be discussed in the pull request.

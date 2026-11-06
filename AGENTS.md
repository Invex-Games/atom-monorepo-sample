# Agent Instructions

Guidance for AI agents working in **atom-monorepo-sample**, a sample .NET monorepo that shows Atom's monorepo
support: two independently versioned apps (AppA and AppB) that share a library (Shared).

## Layout

- `src/<Product>/` holds each product's project and its tests. The product's paths in `_atom/IBuild.cs` must cover both.
- `_atom/` is the Atom build: the products and workflows in `IBuild.cs`, the per-product targets in
  `IProductTargets.cs`.
- Package versions are central, in `Directory.Packages.props`.

## Rules

- `.github/workflows/*.yml` and `src/AppA/GitVersion.yml` and `src/AppB/GitVersion.yml` are generated. After changing
  the build, the products or the root `GitVersion.yml`, run `dotnet run --project _atom -- gen` and commit the result.
  CI fails if they are out of date.
- Each product needs its own targets. A product target in the Validate workflow must depend directly on
  `DetectAffectedProducts`, and `ValidateGate` must depend on every product target and on `ValidateBuild`. `atom gen`
  fails otherwise.
- Adding a product means: a `Product` in `IBuild.cs` (with a `GitVersionConfig` if it is versioned), its targets in
  `IProductTargets.cs`, the targets in the Validate workflow and the gate, its projects in `MonorepoSample.slnx`, and, for
  an app, a `ProductBuild` workflow.
- Keep the sample small and focused on the monorepo features.

## Build

```shell
dotnet build MonorepoSample.slnx
dotnet test MonorepoSample.slnx
```

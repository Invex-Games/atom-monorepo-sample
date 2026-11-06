# Atom monorepo sample

A small .NET monorepo built with [Atom](https://github.com/Invex-Games/atom). It holds two apps that share a library.
Each app is versioned, built and released on its own, and pull requests only test what they change.

It follows the [Monorepos guide](https://github.com/Invex-Games/atom/blob/94896ed4b8bd60b89acfed9dd407f48e7a9c9fd5/docs/workflows/monorepos.md). Read that for the details behind each piece.

> **Prerelease:** monorepo support isn't in a stable Atom release yet. This sample uses a prerelease Atom package built
> from the feature branch, pinned in [`Directory.Packages.props`](Directory.Packages.props). Atom 5.0.0 and the
> current 5.1.0 release candidates don't have it, so keep the exact pin even if a package updater offers a "newer"
> version. The API may still change before it ships.

## What's in it

| Product | Paths | What it is | Release tags |
|---------|-------|------------|--------------|
| Shared | `src/Shared/` | A library both apps use. It has no version or pipeline of its own. | none |
| AppA | `src/AppA/` | `appa`, a console app that greets people. | `appa/v1.0.0` |
| AppB | `src/AppB/` | `appb`, a console app that counts words. | `appb/v1.0.0` |

Each product folder holds its project and its tests. The products are declared in [`_atom/IBuild.cs`](_atom/IBuild.cs),
and their targets are in [`_atom/IProductTargets.cs`](_atom/IProductTargets.cs).

Some paths affect every product: `global.json`, `Directory.Build.*`, `Directory.Packages.*`, `_atom/` and the
workflows. A change to `Directory.Packages.props`, for example, tests and versions both apps.

## The workflows

Atom generates every file in `.github/workflows/` and each app's `GitVersion.yml`. Don't edit them by hand: change the
build and run `dotnet run --project _atom -- gen`.

**Validate** runs on every pull request into `main`:

1. `DetectAffectedProducts` compares the pull request with `main` and lists the products it touches. A change to
   Shared affects Shared, AppA and AppB.
2. `TestShared`, `TestAppA` and `TestAppB` run only when their product is affected. Their jobs show as skipped
   otherwise.
3. `ValidateBuild` runs every time and checks the product declarations.
4. `ValidateGate` runs only if one of those jobs failed or the run was cancelled, and then fails.

Require only **ValidateGate** in branch protection. GitHub counts a skipped required check as passed, so a pull request
that touches only AppB still passes, and one with a failing test does not.

In the repository's merge settings, allow only squash merging. A rebase merge (or a direct push of several commits) can
build an app from a commit that only touched the other app, and GitVersion then gives it a different version. See the
guide's [Gotchas](https://github.com/Invex-Games/atom/blob/94896ed4b8bd60b89acfed9dd407f48e7a9c9fd5/docs/workflows/monorepos.md#gotchas).

**AppA - Build** and **AppB - Build** run when a change to that app (or Shared, or a global path) reaches `main`,
when a full release (not a pre-release) of that app is published, or manually. They publish the app with its own version, and on a release upload
it to the release.

## Versions

Each app gets its version from GitVersion, using its own generated `src/<App>/GitVersion.yml`. That file is the root
[`GitVersion.yml`](GitVersion.yml) with the app's tag prefix and `ignore.paths` added, so a commit that only touched
AppB doesn't change AppA's version.

To see an app's version locally:

```shell
dotnet run --project _atom -- SetupBuildInfo --product AppA
```

## Releasing an app

GitVersion only sees an app's release tag on a commit that touched the app. A tag on a commit that only changed the
other app is ignored, so the release would get the wrong version.

1. On an up-to-date `main`, find the commit to release from:

   ```shell
   git switch main && git pull
   dotnet run --project _atom -- ShowReleases
   ```

2. Create a full GitHub release (leave "Set as a pre-release" unticked) with a new tag `appa/v1.0.0` (or `appb/...`),
   targeting that commit. With the GitHub CLI:

   ```shell
   gh release create appa/v1.0.0 --target <commit from step 1> --title "AppA 1.0.0"
   ```

   The build workflows trigger on GitHub's `released` event, which GitHub doesn't send for a pre-release. Turning a
   pre-release into a full release later does start the build.

3. **AppA - Build** runs, checks that the tag matches the version GitVersion computes, and uploads AppA to the release.
   **AppB - Build** sees that the tag isn't AppB's and skips its jobs.

If **AppA - Build** fails because the tag doesn't match AppA's version, the release is published without AppA in it.
Moving the tag and re-running the job doesn't help, because a re-run builds the original commit. Delete the release and
its tag, then create it again on the commit that `ShowReleases` prints:

```shell
gh release delete appa/v1.0.0 --cleanup-tag --yes
gh release create appa/v1.0.0 --target <commit> --title "AppA 1.0.0"
```

## Trying it out

- Open a pull request that edits only `src/AppB/`. Validate runs `TestAppB` and skips `TestShared` and `TestAppA`.
- Open one that edits `src/Shared/`. All three test jobs run.
- See which products your branch affects:

  ```shell
  GITHUB_BASE_REF=main dotnet run --project _atom -- DetectAffectedProducts
  ```

## Building

You need the .NET SDK 10.0.403 or later (pinned in [`global.json`](global.json)).

```shell
dotnet build MonorepoSample.slnx
dotnet test MonorepoSample.slnx
dotnet run --project src/AppA/MonorepoSample.AppA -- Ada Grace
```

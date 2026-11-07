namespace Atom;

/// <summary>
///     The per-product targets. Targets are fixed when the build compiles, so each product has its own.
/// </summary>
internal interface IProductTargets : IProductsDefinition,
    IReleaseTagHelper,
    IDotnetTestHelper,
    IDotnetPublishHelper,
    IGitHubReleaseHelper,
    IValidateBuild
{
    const string AppAProject = "MonorepoSample.AppA";
    const string AppBProject = "MonorepoSample.AppB";

    // Product targets in the validation workflow must depend directly on DetectAffectedProducts, or their jobs can't
    // read the affected products. atom gen checks this.

    Target TestShared =>
        t => t
            .DescribedAs("Tests the Shared library")
            .DependsOn(nameof(DetectAffectedProducts))
            .Executes(cancellationToken => Test("MonorepoSample.Shared.Tests", false, cancellationToken));

    Target TestAppA =>
        t => t
            .DescribedAs("Tests AppA")
            .DependsOn(nameof(DetectAffectedProducts))
            .Executes(cancellationToken => Test("MonorepoSample.AppA.Tests", true, cancellationToken));

    Target TestAppB =>
        t => t
            .DescribedAs("Tests AppB")
            .DependsOn(nameof(DetectAffectedProducts))
            .Executes(cancellationToken => Test("MonorepoSample.AppB.Tests", true, cancellationToken));

    // The gate depends on every product target and on the shared ValidateBuild target. The workflow only runs it when
    // one of them failed or the run was cancelled, so in CI it always fails. Locally its dependencies have already
    // succeeded, so it does nothing.
    Target ValidateGate =>
        t => t
            .DescribedAs("Fails if any product job failed or the run was cancelled")
            .DependsOn(nameof(ValidateBuild))
            .DependsOn(nameof(TestShared))
            .DependsOn(nameof(TestAppA))
            .DependsOn(nameof(TestAppB))
            .Executes(() =>
            {
                if (GetService<IWorkflowContext>().WorkflowType is not null)
                    throw new StepFailedException("A product job failed or the run was cancelled.");
            });

    // Run with --product AppA (the AppA workflow injects it), so the version is AppA's own
    Target PackAppA =>
        t => t
            .DescribedAs("Publishes AppA")
            .ProducesArtifact(AppAProject)
            .Executes(cancellationToken => DotnetPublishAndStage(AppAProject, cancellationToken: cancellationToken));

    Target PackAppB =>
        t => t
            .DescribedAs("Publishes AppB")
            .ProducesArtifact(AppBProject)
            .Executes(cancellationToken => DotnetPublishAndStage(AppBProject, cancellationToken: cancellationToken));

    Target ReleaseAppA =>
        t => t
            .DescribedAs("Uploads AppA to its GitHub release")
            .RequiresParam(nameof(GitHubToken))
            .ConsumesArtifact(nameof(PackAppA), AppAProject)
            .Executes(cancellationToken => Release(AppAProject, cancellationToken));

    Target ReleaseAppB =>
        t => t
            .DescribedAs("Uploads AppB to its GitHub release")
            .RequiresParam(nameof(GitHubToken))
            .ConsumesArtifact(nameof(PackAppB), AppBProject)
            .Executes(cancellationToken => Release(AppBProject, cancellationToken));

    // GitVersion can only see a product's release tag on a commit that touched the product, so this prints the commit
    // to create each versioned product's next release on
    Target ShowReleases =>
        t => t
            .DescribedAs("Shows the commit to create each app's next release on")
            .Executes(async cancellationToken =>
            {
                foreach (var product in Products.Where(x => x.GitVersionConfig is not null))
                {
                    // The same :(glob) pathspecs that GetReleaseTagCommit() uses for the product being built, so that
                    // '*' doesn't match across '/', as in GitVersion's ignore.paths
                    var paths = GetProductPaths(product)
                        .Select(x => $":(glob){x}")
                        .ToList();

                    var commit = await GetReleaseTagCommit(paths, cancellationToken);

                    Logger.LogInformation("{Product}: tag {Commit} as {Prefix}<version>",
                        product.Name,
                        commit,
                        product.TagPrefix);
                }
            });

    private async Task Test(string project, bool setVersion, CancellationToken cancellationToken)
    {
        // Shared has no version of its own, so its tests don't ask GitVersion for one
        var exitCode = await DotnetTestAndStage(project,
            new()
            {
                SetVersionsFromProviders = setVersion,
            },
            cancellationToken);

        if (exitCode != 0)
            throw new StepFailedException($"The tests in {project} failed.");
    }

    private async Task Release(string artifact, CancellationToken cancellationToken)
    {
        // The release's tag must match the version GitVersion computed from it. It doesn't when the tag is on a commit
        // that GitVersion ignores for this product (one that touched none of its paths), because GitVersion then can't
        // see the tag.
        var tag = ReleaseTagFor(BuildVersion);

        if (GitHub.IsGitHubActions && !string.Equals(GitHub.Variables.RefName, tag, StringComparison.Ordinal))
            throw new StepFailedException(
                $"The release tag '{GitHub.Variables.RefName}' doesn't match {BuildName}'s version {BuildVersion}. Put the tag on the latest commit that touched {BuildName}: run 'dotnet run --project _atom -- ShowReleases' to find it.");

        await UploadArtifactToRelease(artifact, tag, cancellationToken: cancellationToken);
    }
}

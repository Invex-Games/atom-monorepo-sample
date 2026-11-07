namespace Atom;

/// <summary>
///     The monorepo's build. It declares the products, and generates one validation workflow for every pull request
///     plus one build workflow per app.
/// </summary>
[BuildDefinition]
[GenerateEntryPoint]
internal interface IBuild : IWorkflowBuildDefinition,
    IGitHubWorkflows,
    IGitVersion,
    ISetupBuildInfo,
    IValidateBuild,
    IProductTargets
{
    IReadOnlyList<IBuildOption> IBuildDefinition.Options =>
    [
        BuildOptions.GitVersion.ProvideBuildId,
        BuildOptions.GitVersion.ProvideBuildVersion,
        BuildOptions.Steps.SetupDotnet.Dotnet100X(),
    ];

    IReadOnlyList<Product> IProductsDefinition.Products =>
    [
        // A path group: it has no pipelines or version of its own, but a change to it affects AppA and AppB
        new("Shared")
        {
            Paths = ["src/Shared/"],
        },

        // Each app is versioned and released on its own, with tags such as appa/v1.0.0
        new("AppA")
        {
            Paths = ["src/AppA/"],
            DependsOn = ["Shared"],
            GitVersionConfig = "src/AppA/GitVersion.yml",
        },
        new("AppB")
        {
            Paths = ["src/AppB/"],
            DependsOn = ["Shared"],
            GitVersionConfig = "src/AppB/GitVersion.yml",
        },
    ];

    IReadOnlyList<WorkflowDefinition> IWorkflowBuildDefinition.Workflows =>
    [
        // Every pull request runs this workflow. It tests only the affected products, and ValidateGate is the only
        // check that branch protection needs to require.
        WorkflowPresets.ProductValidate(this,
            nameof(ValidateGate),
            [
                new("Shared", nameof(TestShared)),
                new("AppA", nameof(TestAppA)),
                new("AppB", nameof(TestAppB)),
            ],
            sharedTargets: [new(nameof(ValidateBuild))])
        .WithOptions(ReadContents) with
        {
            Types = [WorkflowTypes.GitHub.Action],
        },

        // Each app builds when its paths (or Shared's, or a global path) change on main, and when one of its own
        // releases is published. Its tests already ran in the pull request's Validate workflow.
        ProductBuild("AppA", nameof(PackAppA), nameof(ReleaseAppA)),
        ProductBuild("AppB", nameof(PackAppB), nameof(ReleaseAppB)),
    ];

    // Atom's workflows give the token no permissions unless asked. Every job checks out the repository, which needs
    // contents: read in a private repository. The release job asks for contents: write itself.
    private static IBuildOption ReadContents =>
        BuildOptions.GitHub.TokenPermissions.Set(new Permissions.Exact(new()
        {
            Contents = PermissionsLevel.Read,
        }));

    private WorkflowDefinition ProductBuild(string product, string packTarget, string releaseTarget) =>
        WorkflowDefinition
            .ForProduct(this, product, "Build")
            .WithTriggers(WorkflowTriggers.Manual, WorkflowTriggers.GitHub.OnRelease())
            .WithOptions(ReadContents) with
            {
                Targets =
                [
                    new(packTarget),
                    new(releaseTarget)
                    {
                        Options =
                        [
                            BuildOptions.Inject.Secret(nameof(GitHubToken)),
                            BuildOptions.GitHub.TokenPermissions.Set(new Permissions.Exact(new()
                            {
                                Contents = PermissionsLevel.Write,
                            })),
                            BuildOptions.Target.RunIfWorkflowCondition(Tex.GitHub.EventName.Eq("release")),
                        ],
                    },
                ],
                Types = [WorkflowTypes.GitHub.Action],
            };
}

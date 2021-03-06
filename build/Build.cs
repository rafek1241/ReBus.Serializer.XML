using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Nuke.GitHub;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions("continous-integration",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master","feature/*" },
    OnPullRequestBranches = new[] { "master" },
    InvokedTargets = new[] { nameof(ContinousIntegration) },
    ImportSecrets = new[] { nameof(NugetApiKey), GithubTokenSecretName },
    PublishArtifacts = true
)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    
    const string GithubTokenSecretName = "GITHUB_TOKEN";
    
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json"; //default
    [Parameter] string NugetApiKey;
    [Parameter(Name = GithubTokenSecretName)] string GitHubAuthenticationToken;
    
    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath NugetDestinationDirectory => ArtifactsDirectory / "nuget";
    AbsolutePath TestsResultDirectory => ArtifactsDirectory / "tests";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
            {
                Console.WriteLine(JsonConvert.SerializeObject(GitRepository));
                SourceDirectory.GlobDirectories("**/bin", "**/obj")
                    .ForEach(DeleteDirectory);
                TestsDirectory.GlobDirectories("**/bin", "**/obj")
                    .ForEach(DeleteDirectory);
                EnsureCleanDirectory(ArtifactsDirectory);
            }
        );

    Target Restore => _ => _
        .Executes(() =>
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution)
                );
            }
        );

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion.InformationalVersion)
                    .EnableNoRestore()
                );
            }
        );

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
            {
                DotNetTest(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                );
            }
        );

    Target Pack => _ => _
        .DependsOn(Test)
        .Produces(NugetDestinationDirectory / "*.nupkg")
        .Executes(() =>
            {
                DotNetPack(s => s
                    .SetProject(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetIncludeSymbols(true)
                    .SetVersion(GitVersion.NuGetVersionV2)
                    .SetOutputDirectory(NugetDestinationDirectory)
                );
            }
        );

    Target Deploy => _ => _
        .DependsOn(Pack)
        .OnlyWhenStatic(() => GitRepository.IsOnMasterBranch())
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
            {
                GlobFiles(NugetDestinationDirectory, "*.nupkg")
                    .NotEmpty()
                    .Where(x => !x.EndsWith("symbols.nupkg"))
                    .ForEach(x =>
                        {
                            DotNetNuGetPush(s => s
                                .SetTargetPath(x)
                                .SetSource(NugetApiUrl)
                                .SetApiKey(NugetApiKey)
                                .SetSkipDuplicate(true)
                            );
                        }
                    );
            }
        );

    Target PublishGithubRelease => _ => _
        .DependsOn(Deploy)
        .OnlyWhenStatic(() => GitRepository.IsOnMasterBranch())
        .Requires(() => GitHubAuthenticationToken)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(async () =>
            {
                var releaseTag = GitVersion.MajorMinorPatch;
                var nugets = GlobFiles(NugetDestinationDirectory, "*.nupkg")
                    .NotEmpty()
                    .Where(x => !x.EndsWith("symbols.nupkg"))
                    .ToArray();

                var repo = GitHubTasks.GetGitHubRepositoryInfo(GitRepository);

                await GitHubTasks
                    .PublishRelease(s => s
                        .SetArtifactPaths(nugets)
                        .SetCommitSha(GitVersion.Sha)
                        .SetTag(releaseTag)
                        .SetRepositoryName(repo.repositoryName)
                        .SetRepositoryOwner(repo.gitHubOwner)
                        .SetToken(GitHubAuthenticationToken)
                    );
            }
        );

    Target ContinousIntegration => _ => _
        .DependsOn(PublishGithubRelease);
}
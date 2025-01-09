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
using Nuke.Components;
using Serilog;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;


[UnsetVisualStudioEnvironmentVariables]
[GitHubActions(
    "Build",
    GitHubActionsImage.UbuntuLatest,
    ImportSecrets = new[] { nameof(GitHubToken) },
    EnableGitHubToken = true,
    AutoGenerate = false,
    InvokedTargets = new[] { nameof(Publish) }
)]
class BuildDef : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<BuildDef>(x => x.Build);

    [PackageExecutable(packageId: "gpr", packageExecutable: "gpr.dll")]
    readonly Tool Gpr;


    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("NuGet feed url")]
    readonly string NuGetTargetFeedUrl;
    
    [Parameter("NuGet ApiKey used to push packages")] 
    readonly string NuGetApiKey;

    [Parameter("GitHub NuGet feed url (used to push nuget packages to the repository feed)")]
    readonly string GitHubNuGetFeedUrl = "https://nuget.pkg.github.com/d3-lucapiombino/";
    
    [Parameter("GitHub Token (used to push nuget packages to the repository feed)")] 
    readonly string GitHubToken;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    
    protected override void OnBuildInitialized()
    {
        Log.Information($"Repository {GitRepository.Identifier}");
        Log.Information($"Repository branch {GitRepository.Branch}");
        Log.Information($"Repository url {GitRepository.HttpsUrl}");

        Log.Information($"GitVersion:");
        var props = GitVersion.ToPropertyDictionary(i => i.Name, i => i?.ToString());
        foreach (var prop in props)
        {
            if (prop.Value != null)
                Log.Information("- {prop}: {value}", prop.Key, prop.Value);
        }
    }

    public Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
            );
        });

    Target Build => _ => _
         .DependsOn(Test)
         .Executes(() =>
         {
         });

    Target Pack => _ => _
        .Produces(ArtifactsDirectory / "*.nupkg")
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetNoBuild(true)
                .SetNoRestore(true)
                .SetConfiguration(Configuration)
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(ArtifactsDirectory)
            );
        });

    Target PushPackagesToGitHub => _ => _
        .DependsOn(Pack, Test)
        .OnlyWhenStatic(() => GitHubNuGetFeedUrl != null && GitHubToken != null)
        .Executes(() =>
        {
            
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg")
                .NotEmpty();

            // WHY Gpr and not dotnet nuget push?
            //
            // The dotnet nuget push command only works if the worker image is set to windows-latest, however, 
            // because the start-up time of a Windows worker is significantly longer than ubuntu-latest I rather 
            // trade an additional dependency for an overall faster CI/CD pipeline. It is a personal 
            // choice and a trade off which I'm happy to make in this particular case (more on the benefit of 
            // speed later).
            foreach (var package in packages)
            {
                Log.Information($"Upload package {package} to {GitHubNuGetFeedUrl} using token {GitHubToken}");

                Gpr($"push -k {GitHubToken} {package}");
            }

            
            
            // DOES NOT WORK TODAY

            //DotNet(
            //    $"nuget add source {GitHubNuGetFeedUrl} --name github --username {GitHubUser} --password {GitHubToken} --store-password-in-clear-text"
            //);

            //DotNetNuGetPush(s => s
            //    //.SetSource(NuGetTargetFeedUrl)
            //    //.SetApiKey(NuGetApiKey ?? GitHubToken)
            //    .SetSource("github")
            //    .SetSkipDuplicate(true)
            //    .SetTargetPath(ArtifactsDirectory / "*.nupkg")
            //    .SetLogOutput(true)
                
            //); 
            
        });


    Target PushPackagesToNugetOrg => _ => _
        .DependsOn(Pack, Test)
        .OnlyWhenStatic(() => NuGetTargetFeedUrl != null && NuGetApiKey != null)
        .Executes(() =>
        {
            DotNetNuGetPush(s => s
                .SetSource(NuGetTargetFeedUrl)
                .SetApiKey(NuGetApiKey)
                .SetSkipDuplicate(true)
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
            );
        });

    public Target Publish => _ => _
        .DependsOn(PushPackagesToGitHub, PushPackagesToNugetOrg)
        .Executes(() =>
        {
        });

}

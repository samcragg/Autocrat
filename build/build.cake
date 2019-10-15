#load "utilities.cake"

string configuration = Argument("configuration", "Release");

Task("BuildManaged")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings
        {
            NoLogo = true,
        },
        NoRestore = true,
    };

    IEnumerable<FilePath> projects =
        GetFiles("../src/*/*.csproj").Concat(
            GetFiles("../tests/*/*.csproj"));

    foreach (FilePath project in projects)
    {
        DotNetCoreBuild(project.FullPath, buildSettings);
    }
});

Task("BuildNative")
    .Does(() =>
{
    var buildSettings = new MSBuildSettings
    {
        Configuration = configuration,
        NodeReuse = true,
        NoLogo = true,
        PlatformTarget = PlatformTarget.x64,
        ToolVersion = MSBuildToolVersion.VS2019,
        Verbosity = Verbosity.Minimal,
    };
    MSBuild("../src/Autocrat.Bootstrap/Autocrat.Bootstrap.vcxproj", buildSettings);
    MSBuild("../tests/Bootstrap.Tests/Bootstrap.Tests.vcxproj", buildSettings);
});

Task("Restore")
    .Does(() =>
{
    var restoreSettings = new DotNetCoreRestoreSettings
    {
        MSBuildSettings = new DotNetCoreMSBuildSettings
        {
            NoLogo = true,
        },
        NoDependencies = true,
    };

    IEnumerable<FilePath> projects =
        GetFiles("../src/*/*.csproj").Concat(
            GetFiles("../tests/*/*.csproj"));

    Parallel.ForEach(projects, (FilePath project) =>
    {
        DotNetCoreRestore(project.FullPath, restoreSettings);
    });
});

Task("RestoreGoogleTest")
    .Does(() =>
{
    CheckoutGitRepo(
        "googletest",
        "https://github.com/google/googletest",
        "release-1.10.0",
        "googletest/include/gtest",
        "googletest/scripts",
        "googletest/src");

    Information("Fusing gtest");
    Run("repos/googletest/googletest/scripts", "python", "fuse_gtest_files.py", GetLibsFolder());
});

Task("RestoreGSL")
    .Does(() =>
{
    CheckoutGitRepo(
        "gsl",
        "https://github.com/microsoft/GSL",
        "",
        "include/gsl");

    CopyDirectory("repos/gsl/include", GetLibsFolder());
});

Task("RestoreSpdlog")
    .Does(() =>
{
    CheckoutGitRepo(
        "spdlog",
        "https://github.com/gabime/spdlog",
        "v1.4.2",
        "include/spdlog");

    CopyDirectory("repos/spdlog/include", GetLibsFolder());
});

Task("RunManagedTests")
    .Does(() =>
{
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Verbosity = DotNetCoreVerbosity.Minimal,
    };
    
    foreach (FilePath project in GetFiles("../tests/*/*.csproj"))
    {
        DotNetCoreTest(project.FullPath, testSettings);
    };
});

Task("RunNativeTests")
    .Does(() =>
{
    Run(
        "../tests/Bootstrap.Tests/bin",
        "../tests/Bootstrap.Tests/bin/Bootstrap.Tests.exe",
        "--gtest_output=xml:bootstrap_results.xml");
});

Task("Build")
    .IsDependentOn("BuildManaged")
    .IsDependentOn("BuildNative");

Task("RunTests")
    .IsDependentOn("RunManagedTests")
    .IsDependentOn("RunNativeTests");

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("RunTests");

RunTarget(Argument("target", "Default"));
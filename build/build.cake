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

Task("BuildNativeLinux")
    .WithCriteria(IsRunningOnUnix())
    .Does(() =>
{
    VerifyCommandSucceeded(Run(
        "../src/Autocrat.Bootstrap",
        "make"
    ));

    VerifyCommandSucceeded(Run(
        "../tests/Bootstrap.Tests",
        "make"
    ));
});

Task("BuildNativeWindows")
    .WithCriteria(IsRunningOnWindows())
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

Task("RestoreCppMock")
    .Does(() =>
{
    CheckoutGitRepo(
        "cpp_mock",
        "https://github.com/samcragg/cpp_mock",
        "v1.0.2",
        "include");

    CopyDirectory("repos/cpp_mock/include", GetLibsFolder());
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

Task("RestoreNuGet")
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

Task("RestoreSpdlog")
    .Does(() =>
{
    CheckoutGitRepo(
        "spdlog",
        "https://github.com/gabime/spdlog",
        "v1.6.1",
        "include/spdlog");

    CopyDirectory("repos/spdlog/include", GetLibsFolder());
});

Task("RestoreTermColor")
    .Does(() =>
{
    CheckoutGitRepo(
        "termcolor",
        "https://github.com/ikalnytskyi/termcolor",
        "",
        "include/termcolor");

    CopyDirectory("repos/termcolor/include", GetLibsFolder());
});

Task("RunManagedTests")
    .Does(() =>
{
    var testSettings = new DotNetCoreTestSettings
    {
        ArgumentCustomization = args => args.Append("--nologo"),
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Verbosity = DotNetCoreVerbosity.Minimal,
    };

    string[] projects =
    {
        "../tests/Abstractions.Tests/Abstractions.Tests.csproj",
        "../tests/Compiler.Tests/Compiler.Tests.csproj",
        "../tests/NativeAdapters.Tests/NativeAdapters.Tests.csproj",
    };
    foreach (string project in projects)
    {
        DotNetCoreTest(project, testSettings);
    };
});

Task("RunNativeLinuxTests")
    .WithCriteria(IsRunningOnUnix())
    .Does(() =>
{
    VerifyCommandSucceeded(Run(
        "../tests/Bootstrap.Tests",
        "make",
        "test"));
});

Task("RunNativeWindowsTests")
    .WithCriteria(IsRunningOnWindows())
    .Does(() =>
{
    VerifyCommandSucceeded(Run(
        "../tests/Bootstrap.Tests/bin",
        "../tests/Bootstrap.Tests/bin/Bootstrap.Tests.exe",
        "--gtest_output=xml:bootstrap_results_windows.xml"));
});

Task("Build")
    .IsDependentOn("BuildManaged")
    .IsDependentOn("BuildNativeWindows")
    .IsDependentOn("BuildNativeLinux");

Task("RunTests")
    .IsDependentOn("RunManagedTests")
    .IsDependentOn("RunNativeWindowsTests")
    .IsDependentOn("RunNativeLinuxTests");

Task("Default")
    .IsDependentOn("RestoreNuGet")
    .IsDependentOn("Build")
    .IsDependentOn("RunTests");

RunTarget(Argument("target", "Default"));

#load "utilities.cake"

string configuration = Argument("configuration", "Release");
string coreRTVersion = "0.2";
string environmentName = IsRunningOnWindows() ? "Windows" : "Linux";

string[] managedTestProjects =
{
    "../tests/Abstractions.Tests/Abstractions.Tests.csproj",
    "../tests/Compiler.Tests/Compiler.Tests.csproj",
    "../tests/NativeAdapters.Tests/NativeAdapters.Tests.csproj",
};

Task("AnalyzeNative")
    .WithCriteria(IsRunningOnUnix())
    .Does(() =>
{
    var toExclude = new[]
    {
        "pal_win32.cpp",
    };

    var sources = GetFiles("../src/Autocrat.Bootstrap/src/*.cpp");
    Parallel.ForEach(sources, (FilePath source) =>
    {
        if (!toExclude.Contains(source.GetFilename().FullPath))
        {
            VerifyCommandSucceeded(Run(
                "../src/Autocrat.Bootstrap",
                "clang-format",
                "-n",
                "-Werror",
                source.FullPath
            ));

            VerifyCommandSucceeded(Run(
                "../src/Autocrat.Bootstrap",
                "clang-tidy",
                "--quiet",
                source.FullPath
            ));
        }
    });
});

Task("BuildManaged")
    .IsDependentOn("Restore")
    .IsDependentOn("UpdateVersions")
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
    PipInstall("scons");
    VerifyCommandSucceeded(RunWithPythonEnvironment(
        $"scons -Qj4 -C .. mode={configuration}"));
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

Task("Clean")
    .Does(() =>
{
    CleanDirectories("../src/**/bin");
    CleanDirectories("../src/**/obj");
    CleanDirectories("../tests/**/bin");
    CleanDirectories("../tests/**/obj");
});

Task("CleanResults")
    .Does(() =>
{
    CleanDirectory("results");
    CleanDirectory("report");
});

Task("FetchCoreRT")
    .Does(() =>
{
    CleanDirectories("CoreRT");
    FilePath archive = DownloadFile(
        $"https://github.com/samcragg/corert/releases/download/v{coreRTVersion}/PackageFiles.{environmentName}.zip");
    System.IO.Compression.ZipFile.ExtractToDirectory(archive.FullPath, "CoreRT");
});

Task("GenerateCoverageManaged")
    .IsDependentOn("CleanResults")
    .Does(() =>
{
    DotNetCoreTestSettings testSettings = CreateDefaultTestSettings(
        configuration,
        "--collect:\"XPlat Code Coverage\"");
    testSettings.Settings = "runsettings.xml";

    foreach (string project in managedTestProjects)
    {
        DotNetCoreTest(project, testSettings);
    };

    Information("Generating report");
    VerifyCommandSucceeded(Run(
        ".",
        "dotnet",
        "tool",
        "run",
        "reportgenerator",
        "-verbosity:Error",
        "-targetdir:report/managed",
        "-reports:results/**/coverage*.xml"));
});

Task("GenerateCoverageNative")
    .WithCriteria(IsRunningOnUnix())
    .IsDependentOn("CleanResults")
    .Does(() =>
{
    PipInstall("gcovr");
    PipInstall("scons");

    VerifyCommandSucceeded(RunWithPythonEnvironment(
        $"scons -Qj4 -C .. coverage=1 mode={configuration}"));

    VerifyCommandSucceeded(Run(
        "../tests/Bootstrap.Tests/bin",
        "../tests/Bootstrap.Tests/bin/Bootstrap.Tests"));

    EnsureDirectoryExists("report/native");
    var objectDirectory = $"../tests/Bootstrap.Tests/obj/{configuration}/src";
    VerifyCommandSucceeded(RunWithPythonEnvironment(
        $"gcovr --config gcovr.cfg --object-directory {objectDirectory} {objectDirectory}"));
});

Task("MergeReports")
    .Does(() =>
{
    VerifyCommandSucceeded(Run(
        ".",
        "dotnet",
        "tool",
        "run",
        "reportgenerator",
        "-verbosity:Error",
        "-targetdir:results/merged",
        "-reporttypes:Cobertura",
        "-reports:results/**/*.xml"));
});

Task("Package")
    .IsDependentOn("Publish")
    .Does(() =>
{
    var settings = new DotNetCorePackSettings
    {
        ArgumentCustomization = args => args.Append("--nologo"),
        Configuration = configuration,
        NoBuild = true,
        NoDependencies = true,
        NoRestore = true,
        OutputDirectory = "packages",
    };

    DotNetCorePack("../src/Autocrat.Abstractions/Autocrat.Abstractions.csproj", settings);
    DotNetCorePack("../src/Autocrat.Compiler/Autocrat.Compiler.csproj", settings);
});

Task("PackageCoreRT")
    .IsDependentOn("FetchCoreRT")
    .Does(() =>
{
    CopyFileToDirectory("../src/Autocrat.Bootstrap/include/exports.h", "../src/Autocrat.Bootstrap/bin");
    DotNetCorePack(
        "Autocrat.CoreRT.csproj",
        new DotNetCorePackSettings
        {
            ArgumentCustomization = args =>
                args.Append("--nologo")
                    .Append($"-p:EnvironmentName={environmentName}"),
            Configuration = configuration,
            OutputDirectory = "packages",
        });
});

Task("Publish")
    .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        ArgumentCustomization = args => args.Append("--nologo"),
        Configuration = configuration,
        NoBuild = true,
        NoDependencies = true,
        NoRestore = true,
        OutputDirectory = "../src/Autocrat.Compiler/bin/Publish",
    };

    DotNetCorePublish("../src/Autocrat.Compiler/Autocrat.Compiler.csproj", settings);
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

Task("RunIntegrationTests")
    .IsDependentOn("CleanResults")
    .Does(() =>
{
    DotNetCoreTestSettings testSettings = CreateDefaultTestSettings(configuration);
    testSettings.Logger = "trx";

    DotNetCoreTest("../tests/Integration.Tests/Integration.Tests.csproj", testSettings);
});

Task("RunManagedTests")
    .IsDependentOn("CleanResults")
    .Does(() =>
{
    DotNetCoreTestSettings testSettings = CreateDefaultTestSettings(configuration);
    testSettings.Logger = "trx";

    foreach (string project in managedTestProjects)
    {
        DotNetCoreTest(project, testSettings);
    };
});

Task("RunNativeTests")
    .IsDependentOn("CleanResults")
    .Does(() =>
{
    var results = MakeAbsolute(Directory("results") + File("bootstrap_results.xml")).FullPath;
    var program = IsRunningOnWindows() ? "Bootstrap.Tests.exe" : "Bootstrap.Tests";
    VerifyCommandSucceeded(Run(
        "../tests/Bootstrap.Tests/bin",
        "../tests/Bootstrap.Tests/bin/" + program,
        $"\"--gtest_output=xml:{results}\""));
});

Task("UpdateVersions")
    .Does(() =>
{
    string version = XmlPeek("../Directory.Build.props", "/Project/PropertyGroup/Version");
    XmlPoke(
        "../src/Autocrat.Compiler/PackageFiles/ManagedToNative.csproj",
        "/Project/PropertyGroup/CoreRTVersion",
        version,
        new XmlPokeSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        });
});

Task("Build")
    .IsDependentOn("BuildManaged")
    .IsDependentOn("BuildNativeWindows")
    .IsDependentOn("BuildNativeLinux");

Task("RunTests")
    .IsDependentOn("RunManagedTests")
    .IsDependentOn("RunNativeTests");

Task("GenerateCoverage")
    .IsDependentOn("BuildManaged")
    .IsDependentOn("GenerateCoverageNative")
    .IsDependentOn("GenerateCoverageManaged")
    .IsDependentOn("MergeReports");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("RunTests")
    .IsDependentOn("Package")
    .IsDependentOn("PackageCoreRT")
    .IsDependentOn("RunIntegrationTests");

RunTarget(Argument("target", "Default"));

#load "utilities.cake"

const string MainSolution = "../Autocrat.sln";
string configuration = Argument("configuration", "Release");

Task("Build")
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

    DotNetCoreBuild(MainSolution, buildSettings);
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
    };

    IEnumerable<FilePath> projects =
        GetFiles("../src/**/*.csproj").Concat(
            GetFiles("../test/**/*.csproj"));

    foreach (FilePath project in projects)
    {
        DotNetCoreRestore(project.FullPath, restoreSettings);
    }
});

Task("RestoreGoogleTest")
    .Does(() =>
{
    string outputDir = MakeAbsolute(Directory("..")).FullPath + "/libs";
    
    CheckoutGitRepo(
        "googletest",
        "https://github.com/google/googletest",
        "release-1.8.1",
        "googletest/include/gtest",
        "googletest/scripts",
        "googletest/src");

    Information("Fusing gtest");
    Run("repos/googletest/googletest/scripts", "python", "fuse_gtest_files.py", outputDir);
});

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build");

RunTarget(Argument("target", "Default"));
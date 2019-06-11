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
            NoLogo = true
        },
        NoRestore = true
    };

    DotNetCoreBuild(MainSolution, buildSettings);
});

Task("Restore")
    .Does(() =>
{
    DotNetCoreRestore(MainSolution);
});

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build");

RunTarget(Argument("target", "Default"));
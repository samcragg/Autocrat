#load "utilities.cake"

var repos = new Dictionary<string, (string url, string tag)>
{
    { "cpp_mock", ("https://github.com/samcragg/cpp_mock", "v1.0.2") },
    { "googletest", ("https://github.com/google/googletest", "release-1.10.0") },
    { "spdlog", ("https://github.com/gabime/spdlog", "v1.8.0") },
    { "termcolor", ("https://github.com/ikalnytskyi/termcolor", "") },
};

void CheckoutGitRepo(string repo, params string[] subDirectories)
{
    (string url, string tag) = repos[repo];
    string target = "repos/" + repo;
    if (DirectoryExists(target))
    {
        DeleteDirectory(target, new DeleteDirectorySettings { Force = true, Recursive = true });
    }
    CreateDirectory(target);

    Run(target, "git", "init", "-q");
    Run(target, "git", "remote", "add", "origin", url);

    if (subDirectories.Length > 0)
    {
        Run(target, "git", "config", "core.sparseCheckout true");
        System.IO.File.WriteAllText(target + "/.git/info/sparse-checkout", string.Join("\n", subDirectories));
    }

    Information("Fetching '{0}'", url);
    if (string.IsNullOrEmpty(tag))
    {
        Run(target, "git", "fetch", "-q", "--depth 1", "origin");
        Run(target, "git", "checkout", "master", "-q");
    }
    else
    {
        Run(target, "git", "fetch", "-q", "--depth 1", "origin", "tag", tag);
        Run(target, "git", "checkout", tag, "-q");
    }
}

Task("CppMock")
    .Does(() =>
{
    CheckoutGitRepo(
        "cpp_mock",
        "include");

    CopyDirectory("repos/cpp_mock/include", GetLibsFolder());
});

Task("GoogleTest")
    .Does(() =>
{
    CheckoutGitRepo(
        "googletest",
        "googletest/include/gtest",
        "googletest/scripts",
        "googletest/src");

    Information("Fusing gtest");
    Run("repos/googletest/googletest/scripts", "python", "fuse_gtest_files.py", GetLibsFolder());
});

Task("Spdlog")
    .Does(() =>
{
    CheckoutGitRepo(
        "spdlog",
        "include/spdlog");

    CopyDirectory("repos/spdlog/include", GetLibsFolder());
});

Task("TermColor")
    .Does(() =>
{
    CheckoutGitRepo(
        "termcolor",
        "include/termcolor");

    CopyDirectory("repos/termcolor/include", GetLibsFolder());
});

RunTarget(Argument("target", ""));

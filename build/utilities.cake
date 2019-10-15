void CheckoutGitRepo(string repo, string url, string tag, params string[] subDirectories)
{
    repo = "repos/" + repo;
    if (DirectoryExists(repo))
    {
        DeleteDirectory(repo, new DeleteDirectorySettings { Force = true, Recursive = true });
    }
    CreateDirectory(repo);

    Run(repo, "git", "init", "-q");
    Run(repo, "git", "remote", "add", "origin", url);

    if (subDirectories.Length > 0)
    {
        Run(repo, "git", "config", "core.sparseCheckout true");
        System.IO.File.WriteAllText(repo + "/.git/info/sparse-checkout", string.Join("\n", subDirectories));
    }

    Information("Fetching '{0}'", url);
    if (string.IsNullOrEmpty(tag))
    {
        Run(repo, "git", "fetch", "-q", "--depth 1", "origin");
        Run(repo, "git", "checkout", "master", "-q");
    }
    else
    {
        Run(repo, "git", "fetch", "-q", "--depth 1", "origin", "tag", tag);
        Run(repo, "git", "checkout", tag, "-q");
    }
}

string GetLibsFolder()
{
    return MakeAbsolute(Directory("..")).FullPath + "/libs";
}

int Run(string workingDirectory, string command, params string[] arguments)
{
    var builder = new ProcessArgumentBuilder();
    foreach (string arg in arguments)
    {
        builder.Append(arg);
    }
    
    return StartProcess(command, new ProcessSettings
    {
        Arguments = builder,
        WorkingDirectory = workingDirectory,
    });
}

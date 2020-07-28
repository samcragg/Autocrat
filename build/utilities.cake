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

void VerifyCommandSucceeded(int returnCode)
{
    if (returnCode != 0)
    {
        throw new Exception("Command returned " + returnCode);
    }
}

string GetLibsFolder()
{
    return MakeAbsolute(Directory("..")).FullPath + "/libs";
}

void PipInstall(string tool)
{
    int result = RunWithPythonEnvironment($"which {tool} > /dev/null");
    if (result != 0)
    {
        Information($"Installing {tool} from pip");
        VerifyCommandSucceeded(
            RunWithPythonEnvironment("pip install --disable-pip-version-check -q " + tool));
    }
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

int RunWithPythonEnvironment(string command)
{
    if (!FileExists("tools/py/bin/activate"))
    {
        Information("Setting up Python virtual environment");
        CleanDirectory("tools/py");
        VerifyCommandSucceeded(Run(".", "python3", "-m", "venv", "tools/py"));
    }

    return Run(
        ".",
        "bash",
        "-c",
        "\"source tools/py/bin/activate && " + command + "\"");
}

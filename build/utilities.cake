DotNetCoreTestSettings CreateDefaultTestSettings(string configuration, params string[] additionalArgs)
{
    return new DotNetCoreTestSettings
    {
        ArgumentCustomization = args =>
        {
            args.Append("--nologo");
            foreach (string a in additionalArgs)
            {
                args = args.Append(a);
            }
            return args;
        },
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        ResultsDirectory = "results",
        Verbosity = DotNetCoreVerbosity.Minimal,
    };
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

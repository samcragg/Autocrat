# Building

This library is designed to build under Windows and Linux by using
[Cake](https://cakebuild.net/), however, there are bootstrap scripts to download
and invoke the tool, so there isn't anything else to install apart from the
normal C# and C++ build tools.

## Windows

Make sure that the .NET Core SDK 3.1 is installed and the Visual C++ 2019
compiler (although it's probably easier to install Visual Studio for this,
you can just install the Build Tools for Visual Studio 2019). Then run the
`build.cmd` (or `build.ps1` from PowerShell) inside the `build` directory and
it will perform a full build and generate the NuGet packages.

## Linux

To build under Linux, the following will need installing:

+ [.NET Core SDK 3.1](https://docs.microsoft.com/en-gb/dotnet/core/install/linux)
+ g++ (version 9 is currently used, but versions supporting C++17 should work)
+ python (version 3+, should be installed by default)

You will also need to install `python3-venv` if running under Ubuntu, as by
default the python that ships with it doesn't include it. In addition to these,
for a full build (including code analysis), the following additional packages
will need to be available:

+ clang-format
+ clang-tidy

With all the above, you should then be able to run the `builds.sh` script from
inside the `build` directory and it will perform a full build, run the tests,
perform code analysis and then generate the NuGet packages.

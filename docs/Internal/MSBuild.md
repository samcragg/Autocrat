# MSBuild

To transform the project from managed code into a native executable, several
projects files are used to help control the MSBuild process. These projects are
shipped with the NuGet package and live in the `PackageFiles` directory in the
`Autocrat.Compiler` project.

+ `Package.props`/`Package.targets` - Used to integrate the NuGet package into
  the publish pipeline of the target project.
+ `Dependencies.proj` - Used to restore the platform specific package that
  contains the managed to native (CoreRT) compiler.
+ `ManagedToNative.csproj` - Used to feed the CoreRT compiler.
+ `NativeProgram.proj` - Wraps the tasks in the platform specific
  `NativeCompiler.Platform.targets`, allowing the generated C++ code to be
  compiled and for the native libraries to be linked together.

The reason to split the compilation of the generated C++ code and the linking
into two steps is to allow the former to occur in parallel with the
transformation of the managed code into a native library.

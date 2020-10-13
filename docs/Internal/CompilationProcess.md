# Compilation Process

Although this knowledge isn't required for using the framework, it can be
helpful to have an understanding of how the generated executable is created.
The input to the process starts with a project that references the
`Autocrat.Compiler` package. This package adds a target that is executed after
the project is published (inventively named `AutocratTarget`) - the source of
this lives inside the `Autocrat.Compiler/PackageFiles` directory. This target
does a few things:

+ Transforms the project to a new managed library (see below).
+ Creates an empty managed project (`ManagedToNative.csproj`) that references
  the generated library and the platform specific `Autocrat.CoreRT` package,
  which includes the Microsoft ILCompiler and tweaked libraries.
+ Publishes the `ManagedToNative` project as a native static library
+ Invokes the native tools linker to combine the `CoreRT` libraries,
  `Autocrat.Bootstrap` library (bundled inside the `Autocrat.CoreRT` package)
  and the generated static library (via the `AutocratLink` target).
+ Copies the generated executable into the publish directory and cleans up some
  of the temporary files made when generating the above.

Summarising the above, the project goes through the following pipeline:

    ManagedProject -> GeneratedAssembly -> NativeLibrary -> Executable

## Tool integration

The managed assembly transformation happens via a custom MSBuild task, which is
shipped with the package and added to the target project via the custom targets
(NuGet automatically adds these targets if it finds them in the correct place in
the package).

The transformed assembly is compiled to a native library via the (tweaked)
CoreRT compiler, which is shipped in a platform specific package referenced from
the `Autocrat.Compiler` package targets.

The final native compilation is performed by the platforms native tools, which
need to be installed separately.

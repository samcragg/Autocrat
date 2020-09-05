# Debugging

## Visual Studio on Windows

There is currently a few things needed to be done to debug the application with
Visual Studio on Windows.

The first thing you need to do is publish a debug build of the application:

    dotnet publish -c Debug

This will generate the `exe` and `pdb` files in the
`bin\Debug\netcoreapp3.1\publish` folder by default. To debug the application,
open the `exe` with Visual Studio (either via the File -> Open ->
Project/Solution option or dragging the `exe` file over an open Visual Studio
instance). Open up any source files you want to place a breakpoint in, add the
breakpoints and then you can start debugging.

For better results, you may want to clear the `Require source file to exactly
match the original version` option under Tools -> Options -> Debugging ->
General. This is due to the way the code is generated (the actual assembly is
passed through an analyzer to generate another assembly that is fed into the
native compiler) and it particularly affects the code that implements the
`IInitializer` interface.

# Compilation Process

Although this knowledge isn't required for using the framework, it can be
helpful to have an understanding of how the generated executable is created.
The input to the process starts with a project that references the
`Autocrat.Compiler` package. This package adds a target that is executed after
the project is published (inventively named `AutocratTarget`). This target
does a few things:

+ Transforms the project to a new managed library (see below)
+ Creates an empty managed project (`ManagedToNative`) that references the
  generated library and `Microsoft.DotNet.ILCompiler`
+ Publishes the `ManagedToNative` project as a native static library
+ Invokes the native tools linker to combine the `CoreRT`,
  `Autocrat.Bootstrap` and generated static libraries (via the `AutocratLink`
  target)
+ Copies the generated executable into the publish directory and cleans up some
  of the temporary files made when generating the above

Summarising the above, the project goes through the following pipeline:

    ManagedProject -> GeneratedAssembly -> NativeLibrary -> Executable

## Managed code transformation

In order to ease application development, the framework uses various interfaces
that get transformed by `Autocrat.Compiler` to be able to be called from the
native bootstrap. This also allows dependency injection to be done at compile
time rather than needing runtime reflection.

To do this, `Autocrat.Compiler` first loads the `csproj` file and looks for
classes implementing the `Autocrat.Abstractions.IInitializer` interface and
creates a native callable method that creates the class and invokes the method,
i.e. code along these lines:

    [NativeCallable(...)]
    public static void OnInitialize()
    {
        var dependency = new InjectedDependency();
        var instance = new Instance(dependency);
        instance.OnInitialize();
    }

Note the dependencies are scanned for at compile time and the generated method
creates them as locals (i.e. their transient). In addition to initializers,
classes that are called back from the framework methods also get transformed
so they can be called from native code, e.g. given this code:

    framework.Register<Class>(arguments);

would be transformed to:

    ExportedNativeMethods.Register_MethodInClass(arguments, 0);

where `0` represents the index of the method in the native method stubs array
(each method in the class gets transformed similar to how the `IInitializer`
method gets created and then an array is generated that contains all the
exported managed methods in the native code)

Here's a complete example of the above. Starting with this code:

    public class Initializer : IInitializer
    {
        private readonly INetworkService networkService;

        public Initializer(INetworkService networkService)
        {
            this.networkService = networkService;
        }

        public void OnConfigurationLoaded()
        {
            this.networkService.RegisterUdp<UdpHandler>(123);
        }
    }

    public class UdpHandler : IUdpHandler
    {
        public void OnDataReceived(int port, byte[] data)
        {
            Console.WriteLine("Data received on: {0}", port);
        }
    }

The above code would cause the following C# code to be generated:

    public class Initialization
    {
        [NativeCallable(...)]
        public static void OnConfigurationLoaded()
        {
            var instance = new Initializer(new NetworkService());
            instance.OnConfigurationLoaded();
        }
    }

    public static class NativeCallableMethods
    {
        [NativeCallable(...)]
        public static void UdpHandler_OnDataReceived(int port, byte[] data)
        {
            var instance = new UdpHandler();
            instance.OnDataReceived(port, data);
        }
    }

In addition to this, the `Initializer.OnConfigurationLoaded` method is
transformed to this:

    public void OnConfigurationLoaded()
    {
        NetworkService.OnDataReceived(123, 0);
    }

Notice that we no longer use the `INetworkService` instance and we also pass in
`0` to the static method. Taking a look at the generated C++ code, the
following gets compiled and linked with the native executable:

    extern "C" void UdpHandler_OnDataReceived(std::int32_t, void*);

    method_types& get_known_method(std::size_t handle)
    {
        static std::array<method_types, 1> known_methods =
        {
            &UdpHandler_OnDataReceived,
        };

        return known_methods.at(handle);
    }

This method is what's used when a `NetworkServic.OnDataReceived` is called to
find the method to register, where `method_types` is defined in the bootstrap
library with all the expected method types inside a `std::variant`.

Although the above is quite complicated, the user code is relatively simple and
intuitive. The generated code looks more involved, however, most of it is to
register the method as a callback, however, once that's done there is minimal
boilerplate added to the original method.

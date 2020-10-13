# Managed code transformation

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
delegates that are called back from the framework methods also get transformed
so they can be called from native code, e.g. given this code:

    service.Register(arguments, this.CallbackMethod);

would be transformed to:

    Service.Register(arguments, 0);
    ...
    public static class NativeCallableMethods
    {
        [NativeCallable(...)]
        public static void CallbackMethod(object arguments)
        {
            var instance = new Instance();
            instance.CallbackMethod(arguments);
        }
    }

where `0` represents the index of the method in the native method stubs array
(the methods in the `NativeCallableMethods` get put inside an array that gets
compiled by the native compiler, see the below example). For this to work, the
delegates are marked with `NativeDelegate` and the instead of implementing the
interface, `Service` has the `RewriteInterface` attribute applied to it so that
the interface methods get rewritten to call static methods on the class, which
allows the delegates to be rewritten to use the indexes. Note that for
interfaces implemented this way `null` will be passed in to them as they will
never be used.

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
            var handler = new UdpHandler();
            this.networkService.RegisterUdp(123, handler.OnDataReceived);
        }
    }

    public class UdpHandler
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
            var instance = new Initializer(null);
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

    extern "C" void UdpHandler_OnDataReceived(std::int32_t, const void*);

    method_types& get_known_method(std::size_t handle)
    {
        static std::array<method_types, 1> known_methods =
        {
            &UdpHandler_OnDataReceived,
        };

        return known_methods.at(handle);
    }

This method is what's used when a `NetworkService.OnDataReceived` is called to
find the method to register, where `method_types` is defined in the bootstrap
library with all the expected method types inside a `std::variant`.

Although the above is quite complicated, the user code is relatively simple and
intuitive. The generated code looks more involved, however, most of it is to
register the method as a callback, however, once that's done there is minimal
boilerplate added to the original method.

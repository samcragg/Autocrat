# Configuration

Autocrat supports strongly typed configuration classes that can be injected in
to the class constructor. To do this, just mark the class with the
`Configuration` attribute:

```c#
using Autocrat.Abstractions;

[Configuration]
public class ConfigClass
{
    public int MyProperty { get; set; }
}
```

## Configuration source

On start-up, the application will load the data from a file called `config.json`
located in the same folder as the executable, if it exists. Note that the
current working directory is _not_ taken into account (i.e. it gets the folder
from the path of the running process).

## Configuration sections

It's probably more natural to group your configuration into sections. This can
be done by having a dedicated class per section and then the root configuration
class is just a composition of these (note there can only be **one** class
marked with the `Configuration` attribute, referred to as the root configuration
class, which represents the whole configuration file).

For example, given the following JSON:

```json
{
    "network": {
        "port": 12345
    },
    "service": {
        "display": "Sample"
    }
}
```

It's can be split up into separate classes (which can then be injected
individually instead of the whole configuration class), as follows:

```c#
public class NetworkConfig
{
    public int Port { get; set; }
}

public class ServiceConfig
{
    public string Display { get; set; }
}

[Configuration]
public class RootConfig
{
    public NetworkConfig Network { get; set; }
    public ServiceConfig Service { get; set; }
}
```

Any of the three classes can be injected into your class constructor to access
the configuration data at runtime, i.e.

```c#
public class MyService
{
    public MyService(ServiceConfig config)
    {
        // Here config.Display would equal "Sample"
    }
}
```

## How it works

During compile time, the assembly is scanned for a class marked with the
`ConfigurationAttribute` and a deserializer is generated to read the JSON
values into the properties. This is then added to the dependency injection
machinery, enabling the class to be read from a JSON file and injected into a
constructor without requiring any runtime reflection.

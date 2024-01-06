# NestedArgs

NestedArgs is a .NET library designed to streamline the creation and handling of complex command-line arguments in .NET applications. It allows for the definition of nested commands and options, making it easier to build intuitive and powerful command-line interfaces for your applications.

# Key Advantages of NestedArgs

NestedArgs stands out among other command line parsing libraries due to several key features and advantages, including its support for Ahead-of-Time (AOT) compilation. Here are the main benefits:

1. **Hierarchical Command Structure**: NestedArgs allows for defining commands and sub-commands in a nested, hierarchical manner. This feature is particularly useful for complex applications with multiple levels of commands and options, offering a more organized and intuitive user interface.

2. **Flexible Option Handling**: The library provides extensive support for options, including long and short names, default values, required options, and more. This flexibility makes it easier to tailor the command line interface to the specific needs of the application.

3. **Intuitive API**: NestedArgs offers a straightforward and fluent API, making it easy to define commands and options in a way that is both readable and maintainable.

4. **AOT Compilation Support**: NestedArgs is compatible with Ahead-of-Time compilation, ensuring faster startup times and improved performance for applications that require it. This is particularly beneficial for environments where quick execution is crucial.

5. **Seamless .NET Integration**: Designed specifically for .NET applications, NestedArgs integrates seamlessly with the .NET ecosystem, leveraging familiar patterns and practices.

6. **Automatic Help Generation**: NestedArgs automatically generates help text for all commands and options, making it easier for end users to understand how to use the application.

7. **Customization and Extensibility**: It offers ample opportunities for customization and can be extended to suit specific requirements, providing a balance between out-of-the-box functionality and adaptability.

8. **Lightweight and Efficient**: The library is designed to be lightweight and efficient, minimizing the overhead added to your application.

In summary, NestedArgs provides a robust, user-friendly, and efficient solution for handling command line arguments in .NET applications, with the added advantage of AOT compilation support, making it an excellent choice for a wide range of use cases.

## Getting Started

### Installation

To install NestedArgs, use NuGet package manager:

```bash
Install-Package NestedArgs
```

or via the .NET CLI:

```bash
dotnet add package NestedArgs
```

### Basic Usage

Here's a simple example to get you started with NestedArgs:

```csharp
using NestedArgs;

internal class Program
{
    private static void Main(string[] args)
    {
        Command rootCommand = new CommandBuilder("myapp", "Description of my app.")
            .Option(new Option()
            {
                LongName = "option1",
                ShortName = 'o',
                Description = "A sample option",
                IsRequired = true
            })
            // Add more options and subcommands as needed
            .Build();

        var matches = rootCommand.Parse(args);
        // Your command handling logic here
    }
}
```
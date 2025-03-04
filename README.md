# NestedArgs

NestedArgs is a .NET library designed to simplify the creation and management of complex command-line arguments in .NET applications. By supporting nested commands and options, it enables developers to craft intuitive, powerful, and well-structured command-line interfaces with ease.

## Key Advantages of NestedArgs

NestedArgs distinguishes itself from other command-line parsing libraries through its robust feature set and support for Ahead-of-Time (AOT) compilation. Here are the primary benefits:

1. **Hierarchical Command Structure**  
   NestedArgs enables you to define commands and subcommands in a nested, hierarchical way. This is ideal for applications with multiple command levels, providing an organized and user-friendly interface. For instance, think of a `git`-like structure with commands such as `git commit` or `git push`, each with their own options—NestedArgs makes this simple and intuitive.

2. **Flexible Option Handling**  
   The library offers versatile option configuration, including:  
   - Long and short names (e.g., `--verbose` or `-v`)  
   - Default values for optional inputs  
   - Required options that enforce user input  
   - Flags (options without values)  
   - Multi-value options (e.g., `--file file1 --file file2`)  
   This flexibility allows you to design a command-line interface tailored to your application’s unique needs.

3. **Intuitive API**  
   With a fluent and readable API, NestedArgs simplifies the process of defining commands and options. The chained method calls (e.g., `.Option().SubCommand()`) produce clean, maintainable code that’s easy to understand and extend.

4. **AOT Compilation Support**  
   NestedArgs fully supports Ahead-of-Time (AOT) compilation, which pre-compiles your application into native code for faster startup times and better performance. This is especially valuable in scenarios like startup scripts or performance-critical environments where eliminating Just-In-Time (JIT) compilation overhead is a priority.

5. **Seamless .NET Integration**  
   Built with .NET developers in mind, NestedArgs aligns with familiar .NET patterns and practices, ensuring a smooth adoption process. It integrates effortlessly into your existing .NET projects, reducing the learning curve.

6. **Automatic Help Generation**  
   NestedArgs generates help text automatically for all commands and options, saving you time and ensuring users have clear, consistent documentation. Just define your structure, and NestedArgs handles the rest.

7. **Customization and Extensibility**  
   The library strikes a balance between ready-to-use functionality and adaptability. You can customize option types, add validation logic, or extend behavior to meet specific requirements—all while leveraging a solid foundation.

8. **Lightweight and Efficient**  
   Designed to minimize overhead, NestedArgs keeps your application responsive and efficient, even in resource-constrained environments. It’s a lean solution that delivers power without bloat.

In short, NestedArgs is a robust, user-friendly, and high-performance tool for managing command-line arguments in .NET, enhanced by AOT compilation support. It’s an excellent choice for everything from simple utilities to complex, multi-tiered applications.

## Getting Started

### Installation

Install NestedArgs via NuGet Package Manager:

```bash
Install-Package NestedArgs
```

Or use the .NET CLI:

```bash
dotnet add package NestedArgs
```

**Compatibility**: NestedArgs supports .NET 6.0 and later versions.

### Basic Usage

Here’s an example to demonstrate how to set up a basic command-line interface with NestedArgs, including a subcommand to showcase its hierarchical capabilities:

```csharp
using NestedArgs;

internal class Program
{
    private static void Main(string[] args)
    {
        // Define the root command
        Command rootCommand = new CommandBuilder("myapp", "A sample application with nested commands.")
            .Option(new Option
            {
                LongName = "option1",
                ShortName = 'o',
                Description = "A required root-level option",
                IsRequired = true
            })
            .SubCommand(new CommandBuilder("subcmd", "A subcommand for specific tasks.")
                .Option(new Option
                {
                    LongName = "suboption",
                    ShortName = 's',
                    Description = "An optional subcommand option",
                    IsRequired = false
                })
                .Build())
            .Build();

        // Parse the arguments
        var result = rootCommand.Parse(args);

        // Handle the parsed results
        if (result.Status == ParseStatus.Success)
        {
            var matches = result.Matches;
            Console.WriteLine($"Root option value: {matches.Value("option1")}");
            if (matches.SubCommandMatches("subcmd") is CommandMatches subMatches)
            {
                Console.WriteLine($"Subcommand option value: {subMatches.Value("suboption") ?? "Not provided"}");
            }
        }
        else
        {
            Console.WriteLine("Error parsing arguments. Use --help for usage info.");
        }
    }
}
```

**Running the Example**:  
- `myapp --option1 value` triggers the root command with `option1`.  
- `myapp subcmd --option1 value --suboption subvalue` includes the subcommand and its option.  

### Handling Parsed Arguments

After parsing, the `CommandMatches` object provides access to option values:  
- `matches.Value("option1")` retrieves the value of `--option1`.  
- `matches.SubCommandMatches("subcmd")` accesses the subcommand’s matches, allowing you to retrieve `--suboption` or other nested values.  

This structure simplifies handling multi-level command-line inputs in a clear and logical way.
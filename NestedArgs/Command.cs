using System.Text;

namespace NestedArgs;

public class Command
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<Option> Options { get; set; } = new List<Option>();
    public Dictionary<string, Command> SubCommands { get; set; } = new Dictionary<string, Command>();
    public Command? Parent { get; set; }

    public void AddOption(Option option)
    {
        if (option.LongName == "help")
        {
            throw new Exception("Name is reserved.");
        }

        if (Options.Any(v => v.ShortName == option.ShortName) ||
            Options.Any(v => v.LongName == option.LongName))
        {
            throw new Exception("Name already in use.");
        }
            
        Options.Add(option);
    }

    public void AddSubCommand(Command subCommand)
    {
        SubCommands[subCommand.Name] = subCommand;
    }
    
    public CommandMatches ParseWithExceptions(string[] args)
    {
        return ParseCommand(args, this);
    }

    public CommandMatches Parse(string[] args)
    {
        try
        {
            return ParseCommand(args, this);
        }
        catch (Exception ex)
        {
            HandleParsingError(ex);
            Environment.Exit(1);
            throw;
        }
    }

    public string GetHelp()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"\u001b[36m{GetCommandPath(this)}\u001b[0m");

        if (!string.IsNullOrEmpty(Description))
            builder.AppendLine($"\u001b[37m{Description}\u001b[0m");

        builder.AppendLine();
        builder.AppendLine("\u001b[33;1mUSAGE:\u001b[0m");
        builder.AppendLine($"    \u001b[33m{GetUsage(this)}\u001b[0m");

        if (Options.Any())
        {
            builder.AppendLine();
            builder.AppendLine("\u001b[32;1mOPTIONS:\u001b[0m");
            foreach (var option in Options)
            {
                builder.Append($"    \u001b[32m-{option.ShortName}\u001b[0m, \u001b[32m--{option.LongName}\u001b[0m");
                if (option.IsRequired)
                    builder.AppendLine(" \u001b[31;1m(required)\u001b[0m");
                else
                    builder.AppendLine();

                builder.AppendLine($"            \u001b[37m{option.Description}\u001b[0m" + (option.IsRequired ? "" : $" [default: \u001b[36m{option.DefaultValue ?? "none"}\u001b[0m]"));
            }
            builder.AppendLine("        \u001b[32m--help\u001b[0m");
            builder.AppendLine("            \u001b[37mPrint help information\u001b[0m");
        }

        if (SubCommands.Any())
        {
            int maxKeyLength = SubCommands.Max(sc => sc.Key.Length);
            builder.AppendLine();
            builder.AppendLine("\u001b[34;1mSUBCOMMANDS:\u001b[0m");
            foreach (var subCommand in SubCommands)
                builder.AppendLine($"    \u001b[34m{subCommand.Key.PadRight(maxKeyLength)}\u001b[0m    \u001b[37m{subCommand.Value.Description}\u001b[0m");
        }

        return builder.ToString();
    }
    
    public void HandleParsingError(Exception ex)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"\u001b[31merror:\u001b[0m {ex.Message}");
        builder.AppendLine();

        if (ex is CommandException ce)
        {
            builder.AppendLine("\u001b[33;1mUSAGE:\u001b[0m");
            builder.AppendLine($"    \u001b[33m{GetUsage(ce.Command)}\u001b[0m");
        }

        builder.AppendLine("\nFor more information try \u001b[32m--help\u001b[0m");
        Console.Write(builder.ToString());
    }

    string GetUsage(Command command)
    {
        return BuildUsageForCommand(command, true);
    }

    private static string BuildUsageForCommand(Command command, bool topLevel)
    {
        var parentUsage = command.Parent != null ? BuildUsageForCommand(command.Parent, false) + " " : "";

        var usageBuilder = new StringBuilder(parentUsage);
        usageBuilder.Append(command.Name);

        if (topLevel && command.Options.Any(option => !option.IsRequired))
            usageBuilder.Append(" [OPTIONS]");

        foreach (var option in command.Options)
        {
            if (option.IsRequired)
                usageBuilder.Append($" --{option.LongName} <{option.LongName}>");
        }

        if (topLevel && command.SubCommands.Any())
            usageBuilder.Append(" [SUBCOMMAND]");

        return usageBuilder.ToString();
    }

    private static string GetCommandPath(Command command)
    {
        var path = new Stack<string>();
        var current = command;
        while (current != null)
        {
            path.Push(current.Name);
            current = current.Parent;
        }
        return string.Join(" \u279C ", path);
    }
    
    internal static CommandMatches ParseCommand(string[] args, Command command)
    {
        var matches = new CommandMatches(command);

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];

            if (!arg.StartsWith("-"))
            {
                if (command.SubCommands.TryGetValue(arg, out var subCommand))
                {
                    matches.SubCommandMatch = ParseCommand(args.Skip(index + 1).ToArray(), subCommand);
                    break;
                }

                string? suggestedSubCommand = StringExtensions.FuzzyMatch(arg, command.SubCommands.Keys);
                string additionalMessage = suggestedSubCommand != null ? $"\n\n\u001b[33;1mDid you mean '{suggestedSubCommand}'?\u001b[0m" : "";
                throw new CommandException(command, $"The subcommand '\u001b[31m{arg}\u001b[0m' wasn't recognized.{additionalMessage}");
            }

            if (arg == "--help")
            {
                Console.Write(command.GetHelp());
                Environment.Exit(1);
            }

            string? optionName = null;
            string? value = null;
            bool isShortOption = arg.StartsWith("-") && !arg.StartsWith("--");
            Option? relevantOption = null;

            if (isShortOption)
            {
                optionName = arg.Substring(1, 1);
                value = arg.Length > 2 ? arg.Substring(2).TrimStart('=') : null;
                relevantOption = command.Options.FirstOrDefault(opt => opt.ShortName.ToString() == optionName);
            }
            else
            {
                var equalIndex = arg.IndexOf('=');
                optionName = equalIndex != -1 ? arg.Substring(2, equalIndex - 2) : arg.Substring(2);
                value = equalIndex != -1 ? arg.Substring(equalIndex + 1) : null;
                relevantOption = command.Options.FirstOrDefault(opt => opt.LongName == optionName);
            }

            if (relevantOption == null)
            {
                string? suggestedOption = StringExtensions.FuzzyMatch(optionName, command.Options.Select(o => o.LongName));
                string additionalMessage = suggestedOption != null ? $"\n\n\u001b[33;1mDid you mean '{suggestedOption}'?\u001b[0m" : "";
                throw new CommandException(command, $"Found argument '\u001b[31m{arg}\u001b[0m' which wasn't expected, or isn't valid in this context.{additionalMessage}");
            }

            if (value == null && relevantOption.TakesValue)
            {
                if (index + 1 < args.Length)
                    value = args[++index];
                else
                    throw new CommandException(command, $"The following argument requires a value:\n\u001b[31m{arg}\u001b[0m");
            }

            if (value == null && relevantOption.DefaultValue != null)
                value = relevantOption.DefaultValue;

            if (matches.OptionValues.TryGetValue(relevantOption.LongName, out var values))
            {
                if (!relevantOption.AllowMultiple)
                    throw new CommandException(command, $"The argument '\u001b[31m--{relevantOption.LongName}\u001b[0m' was provided more than once and does not allow multiple.");

                if (value != null)
                    values.Add(value);
            }
            else
                matches.OptionValues[relevantOption.LongName] = value != null ? new List<string>() { value } : new List<string>();
        }

        var missingRequiredOptions = command.Options
            .Where(option => option.IsRequired && !matches.OptionValues.ContainsKey(option.LongName))
            .Select(option => $"    --{option.LongName} <{option.LongName.ToUpper()}>")
            .ToList();

        if (missingRequiredOptions.Any())
        {
            var missingOptionsMessage = string.Join(Environment.NewLine, missingRequiredOptions);
            throw new CommandException(command, $"The following required arguments were not provided:\n\u001b[31m{missingOptionsMessage}\u001b[0m");
        }

        return matches;
    }
}

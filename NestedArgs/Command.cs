using System.Text;

namespace NestedArgs;

public enum ParseStatus
{
    Success,
    Failure,
    HelpRequested
}

public class ParseError
{
    public Command Command { get; }
    public string Message { get; }

    public ParseError(Command command, string message)
    {
        Command = command;
        Message = message;
    }
}

public class ParseResult
{
    public ParseStatus Status { get; }
    public CommandMatches? Matches { get; }
    public ParseError? Error { get; }
    public Command? HelpCommand { get; }

    private ParseResult(ParseStatus status, CommandMatches? matches = null, ParseError? error = null, Command? helpCommand = null)
    {
        Status = status;
        Matches = matches;
        Error = error;
        HelpCommand = helpCommand;
    }

    public static ParseResult Success(CommandMatches matches) => new ParseResult(ParseStatus.Success, matches: matches);
    public static ParseResult Failure(ParseError error) => new ParseResult(ParseStatus.Failure, error: error);
    public static ParseResult HelpRequested(Command command) => new ParseResult(ParseStatus.HelpRequested, helpCommand: command);
}

public class OptionGroup
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public GroupConstraint Constraint { get; set; }
    public List<Option> Options { get; set; } = new List<Option>();
}

public enum GroupConstraint
{
    ExactlyOne,
    ZeroOrOne,
    AtLeastOne,
    Any
}

public class Command
{
    public string Name { get; }
    public string? Description { get; set; }
    public List<Option> Options { get; set; } = new List<Option>();
    public List<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();
    public Dictionary<string, Command> SubCommands { get; set; } = new Dictionary<string, Command>();
    public Command? Parent { get; set; }

    public Command(string name, string? description = null)
    {
        Name = name;
        Description = description;

        Options.Add(new Option
        {
            LongName = "help",
            Description = "Print help information",
            TakesValue = false
        });
    }

    public void AddOption(Option option)
    {
        if (Options.Any(v => v.LongName == option.LongName) ||
            (option.ShortName != null && Options.Any(v => v.ShortName == option.ShortName)))
        {
            throw new CommandException(this, $"Name '{option.LongName}' ({option.ShortName}) already in use.");
        }
        Options.Add(option);
    }

    public void AddSubCommand(Command subCommand)
    {
        if (SubCommands.ContainsKey(subCommand.Name))
        {
            throw new CommandException(this, $"Subcommand '{subCommand.Name}' already exists.");
        }
        SubCommands[subCommand.Name] = subCommand;
    }

    public ParseResult Parse(string[] args)
    {
        var res = ParseCommand(args, this);
        if (res.Status == ParseStatus.Failure)
        {
            var err = res.Error;
            if (err != null)
            {
                Console.WriteLine($"\u001b[31;1m{err.Message}\u001b[0m");
                Console.WriteLine();
                Console.WriteLine(err.Command.GetHelp());
            }
        }
        else if (res.Status == ParseStatus.HelpRequested)
        {
            var cmd = res.HelpCommand;
            if (cmd != null)
                Console.WriteLine(cmd.GetHelp());
        }

        return res;
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
            foreach (var option in Options.Where(opt => opt.GroupName == null))
            {
                builder.Append("    ");
                if (option.ShortName.HasValue)
                {
                    builder.Append($"\u001b[32m-{option.ShortName.Value}\u001b[0m, ");
                }
                builder.Append($"\u001b[32m--{option.LongName}\u001b[0m");
                builder.AppendLine();

                string description = option.Description;
                if (option.IsRequired)
                {
                    description += " \u001b[31;1m(required)\u001b[0m";
                }
                else if (option.TakesValue && option.DefaultValue != null)
                {
                    description += $" [default: \u001b[36m{option.DefaultValue}\u001b[0m]";
                }
                builder.AppendLine($"            \u001b[37m{description}\u001b[0m");
            }
        }

        if (OptionGroups.Any())
        {
            builder.AppendLine();
            builder.AppendLine("\u001b[35;1mOPTION GROUPS:\u001b[0m");
            foreach (var group in OptionGroups)
            {
                string constraintText = group.Constraint switch
                {
                    GroupConstraint.ExactlyOne => "choose exactly one",
                    GroupConstraint.ZeroOrOne => "choose zero or one",
                    GroupConstraint.AtLeastOne => "choose at least one",
                    GroupConstraint.Any => "choose any number",
                    _ => "unknown constraint"
                };
                builder.AppendLine($"    \u001b[35m{group.Name.ToUpper()} ({constraintText}):\u001b[0m");
                builder.AppendLine($"    \u001b[37m{group.Description}\u001b[0m");
                foreach (var option in group.Options)
                {
                    builder.Append("        ");
                    if (option.ShortName.HasValue)
                    {
                        builder.Append($"\u001b[32m-{option.ShortName.Value}\u001b[0m, ");
                    }
                    builder.Append($"\u001b[32m--{option.LongName}\u001b[0m");
                    builder.AppendLine();
                    builder.AppendLine($"                \u001b[37m{option.Description}\u001b[0m");
                }
            }
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
        return string.Join(" > ", path);
    }

    internal static ParseResult ParseCommand(string[] args, Command command)
    {
        var matches = new CommandMatches(command);
        int index = 0;

        while (index < args.Length && args[index].StartsWith("-"))
        {
            string arg = args[index];
            if (arg.StartsWith("--"))
            {
                string? optionName;
                string? value = null;
                int equalIndex = arg.IndexOf('=');
                if (equalIndex != -1)
                {
                    optionName = arg.Substring(2, equalIndex - 2);
                    value = arg.Substring(equalIndex + 1);
                }
                else
                {
                    optionName = arg.Substring(2);
                }
                var relevantOption = command.Options.FirstOrDefault(opt => opt.LongName == optionName);
                if (relevantOption == null)
                {
                    string? suggestedOption = StringExtensions.FuzzyMatch(optionName, command.Options.Select(o => o.LongName));
                    string additionalMessage = suggestedOption != null ? $"\n\n\u001b[33;1mDid you mean '{suggestedOption}'?\u001b[0m" : "";
                    return ParseResult.Failure(new ParseError(command, $"Found argument '{arg}' which wasn't expected, or isn't valid in this context.{additionalMessage}"));
                }
                if (relevantOption.TakesValue)
                {
                    if (value == null)
                    {
                        if (index + 1 < args.Length)
                        {
                            value = args[++index];
                        }
                        else
                        {
                            return ParseResult.Failure(new ParseError(command, $"The following argument requires a value: {arg}"));
                        }
                    }
                }
                else
                {
                    if (value != null)
                    {
                        return ParseResult.Failure(new ParseError(command, $"Option --{relevantOption.LongName} does not take a value, but one was provided."));
                    }
                }
                if (matches.OptionValues.ContainsKey(relevantOption.LongName))
                {
                    if (!relevantOption.AllowMultiple)
                    {
                        return ParseResult.Failure(new ParseError(command, $"The argument '--{relevantOption.LongName}' was provided more than once and does not allow multiple values."));
                    }
                    if (value != null)
                    {
                        matches.OptionValues[relevantOption.LongName].Add(value);
                    }
                }
                else
                {
                    matches.OptionValues[relevantOption.LongName] = value != null ? new List<string> { value } : new List<string>();
                }
            }
            else
            {
                int charIndex = 1;
                if (arg.Length < 2)
                {
                    return ParseResult.Failure(new ParseError(command, $"Invalid short option: {arg}"));
                }
                while (charIndex < arg.Length)
                {
                    char shortName = arg[charIndex];
                    var relevantOption = command.Options.FirstOrDefault(opt => opt.ShortName == shortName);
                    if (relevantOption == null)
                    {
                        string? suggestedOption = StringExtensions.FuzzyMatch(shortName.ToString(), command.Options.Where(o => o.ShortName != null).Select(o => o.ShortName?.ToString()!));
                        string additionalMessage = suggestedOption != null ? $"\n\n\u001b[33;1mDid you mean '-{suggestedOption}'?\u001b[0m" : "";
                        return ParseResult.Failure(new ParseError(command, $"Unknown short option '-{shortName}' in '{arg}'.{additionalMessage}"));
                    }
                    if (relevantOption.TakesValue)
                    {
                        string? value = null;
                        if (charIndex + 1 < arg.Length)
                        {
                            value = arg.Substring(charIndex + 1);
                            if (value.StartsWith("="))
                                value = value.Substring(1);
                            charIndex = arg.Length;
                        }
                        else if (index + 1 < args.Length)
                        {
                            value = args[++index];
                            if (value.StartsWith("="))
                                value = value.Substring(1);
                            charIndex = arg.Length;
                        }
                        else
                        {
                            return ParseResult.Failure(new ParseError(command, $"Option -{shortName} requires a value."));
                        }
                        if (matches.OptionValues.ContainsKey(relevantOption.LongName))
                        {
                            if (!relevantOption.AllowMultiple)
                            {
                                return ParseResult.Failure(new ParseError(command, $"Option -{shortName} does not allow multiple values."));
                            }
                            matches.OptionValues[relevantOption.LongName].Add(value);
                        }
                        else
                        {
                            matches.OptionValues[relevantOption.LongName] = new List<string> { value };
                        }
                    }
                    else
                    {
                        if (matches.OptionValues.ContainsKey(relevantOption.LongName))
                        {
                            if (!relevantOption.AllowMultiple)
                            {
                                return ParseResult.Failure(new ParseError(command, $"Option -{shortName} does not allow multiple occurrences."));
                            }
                        }
                        else
                        {
                            matches.OptionValues[relevantOption.LongName] = new List<string>();
                        }
                        charIndex++;
                    }
                }
            }

            index++;
        }

        if (matches.OptionValues.ContainsKey("help"))
            return ParseResult.HelpRequested(command);

        if (index < args.Length)
        {
            string subCommandName = args[index];
            if (command.SubCommands.TryGetValue(subCommandName, out var subCommand))
            {
                var subResult = ParseCommand(args.Skip(index + 1).ToArray(), subCommand);
                if (subResult.Status == ParseStatus.HelpRequested || subResult.Status == ParseStatus.Failure)
                    return subResult;
                matches.SubCommandMatch = subResult.Matches;
            }
            else
            {
                string? suggestedSubCommand = StringExtensions.FuzzyMatch(subCommandName, command.SubCommands.Keys);
                string additionalMessage = suggestedSubCommand != null ? $"\n\nDid you mean '{suggestedSubCommand}'?" : "";
                return ParseResult.Failure(new ParseError(command, $"The subcommand '{subCommandName}' wasn't recognized.{additionalMessage}"));
            }
        }

        var missingRequiredOptions = command.Options
                .Where(option => option.IsRequired &&
                                 !matches.OptionValues.ContainsKey(option.LongName) &&
                                 option.DefaultValue == null)
                .Select(option => $"    --{option.LongName} <{option.LongName.ToUpper()}>")
                .ToList();
        if (missingRequiredOptions.Any())
        {
            var missingOptionsMessage = string.Join(Environment.NewLine, missingRequiredOptions);
            return ParseResult.Failure(new ParseError(command, $"The following required arguments were not provided:\n{missingOptionsMessage}"));
        }

        foreach (var group in command.OptionGroups)
        {
            var providedOptions = group.Options.Count(opt => matches.OptionValues.ContainsKey(opt.LongName));
            bool valid = group.Constraint switch
            {
                GroupConstraint.ExactlyOne => providedOptions == 1,
                GroupConstraint.ZeroOrOne => providedOptions <= 1,
                GroupConstraint.AtLeastOne => providedOptions >= 1,
                GroupConstraint.Any => true,
                _ => false
            };
            if (!valid)
            {
                var groupOptions = string.Join(", ", group.Options.Select(opt => $"--{opt.LongName}"));
                string message = group.Constraint switch
                {
                    GroupConstraint.ExactlyOne => $"Exactly one of the following options must be provided: {groupOptions}",
                    GroupConstraint.ZeroOrOne => $"At most one of the following options can be provided: {groupOptions}",
                    GroupConstraint.AtLeastOne => $"At least one of the following options must be provided: {groupOptions}",
                    GroupConstraint.Any => "",
                    _ => "Invalid group constraint"
                };
                return ParseResult.Failure(new ParseError(command, message));
            }
        }

        return ParseResult.Success(matches);
    }
}

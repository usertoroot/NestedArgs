namespace NestedArgs;

public class OptionGroupBuilder
{
    private readonly OptionGroup _group;
    private readonly Command _command;

    public OptionGroupBuilder(string name, string description, GroupConstraint constraint, Command command)
    {
        _group = new OptionGroup { Name = name, Description = description, Constraint = constraint };
        _command = command;
    }

    public OptionGroupBuilder Option(Option option)
    {
        if (option.IsRequired || option.DefaultValue != null)
        {
            throw new CommandException(_command, "Options in an option group cannot have IsRequired set to true nor can a default value be specified.");
        }
        _group.Options.Add(option);
        return this;
    }

    public OptionGroup Build() => _group;
}

public class CommandBuilder
{
    private Command _command;

    public CommandBuilder(string name, string? description = null)
    {
        _command = new Command(name, description);
    }

    public CommandBuilder Option(Option option)
    {
        _command.AddOption(option);
        return this;
    }

    public CommandBuilder SubCommand(Command command)
    {
        command.Parent = _command;
        _command.AddSubCommand(command);
        return this;
    }

    public CommandBuilder OptionGroup(string name, string description, GroupConstraint constraint, Action<OptionGroupBuilder> configure)
    {
        var groupBuilder = new OptionGroupBuilder(name, description, constraint, _command);
        configure(groupBuilder);
        var group = groupBuilder.Build();
        _command.OptionGroups.Add(group);
        foreach (var option in group.Options)
        {
            option.GroupName = name;
            _command.AddOption(option);
        }
        return this;
    }

    public Command Build() => _command;
}
namespace NestedArgs;

public class CommandBuilder
{
    private Command _command;

    public CommandBuilder(string name, string? description = null)
    {
        _command = new Command() 
        { 
            Name = name,
            Description = description
        };
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

    public Command Build() => _command;
}
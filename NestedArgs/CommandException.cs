using NestedArgs;

public class CommandException : Exception
{
    public Command Command { get; }

    public CommandException(Command command, string message)
        : base(message)
    {
        Command = command;
    }
}
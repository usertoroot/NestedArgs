namespace NestedArgs;

public class Option
{
    public required string LongName { get; set; }
    public required char ShortName { get; set; }
    public required string Description { get; set; }
    public bool TakesValue { get; set; } = true;
    public bool AllowMultiple { get; set; } = false;
    public bool IsRequired { get; set; } = false;
    public string? DefaultValue { get; set; } = null;
}

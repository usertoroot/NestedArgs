using System.Globalization;
using System.Text;

namespace NestedArgs;

public class CommandMatches
{
    private delegate bool TryParseDelegate<T>(string? s, out T result);
    private delegate bool TryParseNumberDelegate<T>(string? s, NumberStyles style, IFormatProvider provider, out T result);
    private delegate T ConvertDelegate<T>(string? s, IFormatProvider provider);

    public Dictionary<string, List<string>> OptionValues { get; private set; } = new Dictionary<string, List<string>>();
    public CommandMatches? SubCommandMatch { get; set; }
    public Command Command { get; }
    public string? SubCommand => SubCommandMatch?.Command.Name;

    public CommandMatches(Command command)
    {
        Command = command;
    }

    public string? Value(string optionName)
    {
        var matchingOption = Command.Options.First(o => o.LongName == optionName);
        if (matchingOption.AllowMultiple)
            throw new CommandException(Command, "This option allows multiple values; use Values instead of Value.");

        if (OptionValues.TryGetValue(optionName, out var value))
        {
            if (value.Count == 1)
                return value[0];
            else if (value.Count == 0)
                return null;
            else
                throw new CommandException(Command, "This option does not allow multiple values, but multiple were specified.");
        }
        else
        {
            if (matchingOption.IsRequired && matchingOption.DefaultValue == null)
                throw new CommandException(Command, $"Option --{optionName} is required, but not set.");
            return matchingOption.DefaultValue;
        }
    }

    public sbyte? ValueAsSByte(string optionName)
    {
        return ParseOrConvertNumber<sbyte>(optionName, sbyte.TryParse);
    }

    public byte? ValueAsByte(string optionName)
    {
        return ParseOrConvertNumber<byte>(optionName, byte.TryParse);
    }

    public ushort? ValueAsUInt16(string optionName)
    {
        return ParseOrConvertNumber<ushort>(optionName, ushort.TryParse);
    }

    public uint? ValueAsUInt32(string optionName)
    {
        return ParseOrConvertNumber<uint>(optionName, uint.TryParse);
    }

    public ulong? ValueAsUInt64(string optionName)
    {
        return ParseOrConvertNumber<ulong>(optionName, ulong.TryParse);
    }

    public short? ValueAsInt16(string optionName)
    {
        return ParseOrConvertNumber<short>(optionName, short.TryParse);
    }

    public int? ValueAsInt32(string optionName)
    {
        return ParseOrConvertNumber<int>(optionName, int.TryParse);
    }

    public long? ValueAsInt64(string optionName)
    {
        return ParseOrConvertNumber<long>(optionName, long.TryParse);
    }

    public float? ValueAsFloat(string optionName)
    {
        return ParseOrConvertNumber<float>(optionName, float.TryParse);
    }

    public double? ValueAsDouble(string optionName)
    {
        return ParseOrConvertNumber<double>(optionName, double.TryParse);
    }

    public decimal? ValueAsDecimal(string optionName)
    {
        return ParseOrConvertNumber<decimal>(optionName, decimal.TryParse);
    }

    public bool? ValueAsBool(string optionName)
    {
        var matchingOption = Command.Options.First(o => o.LongName == optionName);
        if (matchingOption.TakesValue)
            return ParseOrConvert<bool>(optionName, bool.TryParse);
        else
            return Has(optionName);
    }

    public DateTime? ValueAsDateTime(string optionName)
    {
        string? stringValue = Value(optionName);
        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;
        return null;
    }

    private T? ParseOrConvert<T>(string optionName, TryParseDelegate<T> tryParse) where T : unmanaged
    {
        string? stringValue = Value(optionName);
        if (tryParse(stringValue, out T result))
            return result;
        return null;
    }

    private T? ParseOrConvertNumber<T>(string optionName, TryParseNumberDelegate<T> tryParse) where T : unmanaged
    {
        string? stringValue = Value(optionName);
        if (tryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out T result))
            return result;
        return null;
    }

    public List<string>? Values(string optionName)
    {
        var matchingOption = Command.Options.First(o => o.LongName == optionName);
        if (!matchingOption.AllowMultiple)
            throw new CommandException(Command, "This option does not allow multiple, Value should be used instead.");

        if (OptionValues.TryGetValue(optionName, out var value))
        {
            if (value.Count > 0)
                return value;
        }

        if (matchingOption.IsRequired)
            throw new CommandException(Command, "Option is required, but not set.");

        if (matchingOption.DefaultValue != null)
            return new List<string>() { matchingOption.DefaultValue };
        else
            return null;
    }

    public CommandMatches? SubCommandMatches(string name)
    {
        if (SubCommandMatch?.Command.Name == name)
            return SubCommandMatch;
        return null;
    }

    public bool Has(string optionName)
    {
        return OptionValues.ContainsKey(optionName);
    }

    public override string ToString()
    {
        return ToString(0);
    }

    public string ToString(int indent)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{Command.Name}");

        foreach (var option in OptionValues)
        {
            var matchingOption = Command.Options.First(o => o.LongName == option.Key);
            if (matchingOption.TakesValue)
            {
                if (matchingOption.AllowMultiple)
                {
                    var values = Values(option.Key);
                    var valuesStr = values != null ? "(" + string.Join(", ", values) + ")" : "<empty>";
                    builder.AppendLine($"{new string(' ', (indent + 1) * 2)}Option: {option.Key}, Values: {valuesStr}");
                }
                else
                {
                    builder.AppendLine($"{new string(' ', (indent + 1) * 2)}Option: {option.Key}, Value: {Value(option.Key)}");
                }
            }
            else
            {
                builder.AppendLine($"{new string(' ', (indent + 1) * 2)}Option: {option.Key}, Present: {Has(option.Key)}");
            }
        }

        if (SubCommandMatch != null)
            builder.AppendLine($"{new string(' ', (indent + 1) * 2)}Subcommand: {SubCommandMatch.ToString(indent + 1)}");

        return builder.ToString();
    }
}

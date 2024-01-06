using NestedArgs;

internal class Program
{
    private static void Main(string[] args)
    {
        Command rootCommand = new CommandBuilder("fcast", "Control FCast Receiver through the terminal.")
            .Option(new Option()
            {
                LongName = "connection_type",
                ShortName = 'c',
                Description = "Type of connection: tcp or ws (websocket)",
                DefaultValue = "tcp",
                IsRequired = false
            })
            .Option(new Option()
            {
                LongName = "host",
                ShortName = 'h',
                Description = "The host address to send the command to",
                IsRequired = true
            })
            .Option(new Option()
            {
                LongName = "port",
                ShortName = 'p',
                Description = "The port to send the command to",
                IsRequired = false
            })
            .SubCommand(new CommandBuilder("play", "Play media")
                .Option(new Option()
                {
                    LongName = "mime_type",
                    ShortName = 'm',
                    Description = "Mime type (e.g., video/mp4)",
                    IsRequired = true
                })
                .Option(new Option()
                {
                    LongName = "file",
                    ShortName = 'f',
                    Description = "File content to play",
                    IsRequired = false
                })
                .Option(new Option()
                {
                    LongName = "url",
                    ShortName = 'u',
                    Description = "URL to the content",
                    IsRequired = false
                })
                .Option(new Option()
                {
                    LongName = "content",
                    ShortName = 'c',
                    Description = "The actual content",
                    IsRequired = false
                })
                .Option(new Option()
                {
                    LongName = "timestamp",
                    ShortName = 't',
                    Description = "Timestamp to start playing",
                    DefaultValue = "0",
                    IsRequired = false
                })
                .Option(new Option()
                {
                    LongName = "speed",
                    ShortName = 's',
                    Description = "Factor to multiply playback speed by",
                    DefaultValue = "1",
                    IsRequired = false
                })
                .Build())
            .SubCommand(new CommandBuilder("seek", "Seek to a timestamp")
                .Option(new Option()
                {
                    LongName = "timestamp",
                    ShortName = 't',
                    Description = "Timestamp to start playing",
                    IsRequired = true
                })
                .Build())
            .SubCommand(new CommandBuilder("pause", "Pause media").Build())
            .SubCommand(new CommandBuilder("resume", "Resume media").Build())
            .SubCommand(new CommandBuilder("stop", "Stop media").Build())
            .SubCommand(new CommandBuilder("listen", "Listen to incoming events").Build())
            .SubCommand(new CommandBuilder("setvolume", "Set the volume")
                .Option(new Option()
                {
                    LongName = "volume",
                    ShortName = 'v',
                    Description = "Volume level (0-1)",
                    IsRequired = true
                })
                .Build())
            .SubCommand(new CommandBuilder("setspeed", "Set the playback speed")
                .Option(new Option()
                {
                    LongName = "speed",
                    ShortName = 's',
                    Description = "Factor to multiply playback speed by",
                    IsRequired = true
                })
                .Build())
            .Build();

        var matches = rootCommand.Parse(args);
        Console.WriteLine(matches);

        var host = matches.Value("host");
        var connectionType = matches.Value("connection_type");

        var portString = matches.Value("port") ?? connectionType switch
        {
            "tcp" => "46899",
            "ws" => "46898",
            _ => throw new Exception($"{connectionType} is not a valid connection type.")
        };

        //Setup connection

        switch (matches.SubCommand)
        {
            case "play":
            {
                var playMatches = matches.SubCommandMatch!;
                var url = playMatches.Value("url");
                var file = playMatches.Value("file");
                var content = playMatches.Value("content");
                if (new string?[] { url, file, content }.Count(v => v != null) != 1)
                    Console.WriteLine("Exactly one source should be specified.");

                //Execute play

                var mimeType = playMatches.Value("mime_type");
                var timestamp = playMatches.ValueAsDouble("timestamp");
                var speed = playMatches.ValueAsDouble("speed");
                
                break;
            }
        }

        //Cleanup
    }
}
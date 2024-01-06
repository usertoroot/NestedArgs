namespace NestedArgs.Tests;

[TestClass]
public class MainTests
{
    private Command _someRoot = new CommandBuilder("someroot", "description").Option(new Option
    {
        Description = "Some option",
        LongName = "someOption",
        ShortName = 's'
    }).Build();

    private Command _rootCommand = new CommandBuilder("fcast", "Control FCast Receiver through the terminal.")
        .Option(new Option()
        {
            LongName = "connection_type",
            ShortName = 'c',
            Description = "Type of connection: tcp or ws (websocket)",
            DefaultValue = "tcp"
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
        })
        .Option(new Option()
        {
            LongName = "dummy",
            ShortName = 'd',
            Description = "Dummy array test",
            AllowMultiple = true
        })
        .Option(new Option()
        {
            LongName = "encrypted",
            ShortName = 'e',
            Description = "Use encryption",
            TakesValue = false
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
                Description = "File content to play"
            })
            .Option(new Option()
            {
                LongName = "url",
                ShortName = 'u',
                Description = "URL to the content"
            })
            .Option(new Option()
            {
                LongName = "content",
                ShortName = 'c',
                Description = "The actual content"
            })
            .Option(new Option()
            {
                LongName = "timestamp",
                ShortName = 't',
                Description = "Timestamp to start playing",
                DefaultValue = "0"
            })
            .Option(new Option()
            {
                LongName = "speed",
                ShortName = 's',
                Description = "Factor to multiply playback speed by",
                DefaultValue = "1"
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

    private static IEnumerable<object[]> TestShouldExcept_TestData() 
    {
        yield return new[] { new string[] { "-host127.0.0.1", "Argument after long name without space should not be supported" } };
        yield return new[] { new string[] { } };
        yield return new[] { new string[] { "--host" } };
    }
    [TestMethod]
    [DynamicData(nameof(TestShouldExcept_TestData), DynamicDataSourceType.Method)]
    public void TestShouldExcept(string[] args)
    {
        Assert.ThrowsException<CommandException>(() =>
        {
            _rootCommand.ParseWithExceptions(args);
        });
    }

    private static IEnumerable<object[]> TestMatchPositive_TestData() 
    {
        yield return new object[] 
        { 
            new string[] { "-h127.0.0.1" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "-h", "127.0.0.1" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "-h=127.0.0.1" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "--host=127.0.0.1" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "--host", "127.0.0.1" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "--host", "127.0.0.1", "-e" }, 
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }), ("encrypted", null) } 
        };
        yield return new object[] 
        { 
            new string[] { "--host", "127.0.0.1", "-e", "-da=a", "-d=b=b", "-d", "c=c", "--dummy", "d=d", "--dummy=e=e" },
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }), ("encrypted", null), ("dummy", new[] { "a=a", "b=b", "c=c", "d=d", "e=e" }) } 
        };
        yield return new object[] 
        { 
            new string[] { "--host", "127.0.0.1", "-e", "-da=a" },
            new (string, string[]?)[] { ("host", new[] { "127.0.0.1" }), ("encrypted", null), ("dummy", new[] { "a=a" }) } 
        };
    }
    [TestMethod]
    [DynamicData(nameof(TestMatchPositive_TestData), DynamicDataSourceType.Method)]
    public void TestMatchPositive(string[] args, (string Name, string[]? Expected)[] expected)
    {
        var matches = _rootCommand.Parse(args);
        foreach (var e in expected)
        {
            Assert.IsTrue(matches.Has(e.Name));

            var option = _rootCommand.Options.First(o => o.LongName == e.Name);
            if (option.TakesValue)
            {
                if (option.AllowMultiple)
                    CollectionAssert.AreEquivalent(e.Expected, matches.Values(e.Name));
                else
                    Assert.AreEqual(e.Expected![0], matches.Value(e.Name));
            }
        }
    }

    [TestMethod]
    public void TestPlaySubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "play", "--mime_type", "video/mp4", "--url", "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", "-t", "10", "-s", "1.0" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.AreEqual("tcp", matches.Value("connection_type"));

        var playMatches = matches.SubCommandMatches("play")!;
        Assert.AreEqual("video/mp4", playMatches.Value("mime_type"));
        Assert.AreEqual("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", playMatches.Value("url"));
        Assert.AreEqual("10", playMatches.Value("timestamp"));
        Assert.AreEqual("1.0", playMatches.Value("speed"));
    }

    [TestMethod]
    public void TestPlayWithWebSocket()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "-c", "ws", "play", "--mime_type", "video/mp4", "--url", "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", "-t", "10" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.AreEqual("ws", matches.Value("connection_type"));

        var playMatches = matches.SubCommandMatches("play")!;
        Assert.AreEqual("video/mp4", playMatches.Value("mime_type"));
        Assert.AreEqual("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", playMatches.Value("url"));
        Assert.AreEqual("10", playMatches.Value("timestamp"));
    }

    [TestMethod]
    public void TestPlayWithFile()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "192.168.1.62", "play", "--mime_type", "video/mp4", "-f", "/home/koen/Downloads/BigBuckBunny.mp4" });
        Assert.AreEqual("192.168.1.62", matches.Value("host"));

        var playMatches = matches.SubCommandMatches("play")!;
        Assert.AreEqual("video/mp4", playMatches.Value("mime_type"));
        Assert.AreEqual("/home/koen/Downloads/BigBuckBunny.mp4", playMatches.Value("file"));
    }

    [TestMethod]
    public void TestPlayWithDASH()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "play", "--mime_type", "application/dash+xml", "--url", "https://dash.akamaized.net/digitalprimates/fraunhofer/480p_video/heaac_2_0_with_video/Sintel/sintel_480p_heaac2_0.mpd" });
        Assert.AreEqual("localhost", matches.Value("host"));

        var playMatches = matches.SubCommandMatches("play")!;
        Assert.AreEqual("application/dash+xml", playMatches.Value("mime_type"));
        Assert.AreEqual("https://dash.akamaized.net/digitalprimates/fraunhofer/480p_video/heaac_2_0_with_video/Sintel/sintel_480p_heaac2_0.mpd", playMatches.Value("url"));
    }

    [TestMethod]
    public void TestPauseSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "pause" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.IsNotNull(matches.SubCommandMatches("pause"));
    }

    [TestMethod]
    public void TestResumeSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "resume" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.IsNotNull(matches.SubCommandMatches("resume"));
    }

    [TestMethod]
    public void TestSeekSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "seek", "-t", "100" });
        Assert.AreEqual("localhost", matches.Value("host"));

        var seekMatches = matches.SubCommandMatches("seek")!;
        Assert.AreEqual("100", seekMatches.Value("timestamp"));
    }

    [TestMethod]
    public void TestListenSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "listen" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.IsNotNull(matches.SubCommandMatches("listen"));
    }

    [TestMethod]
    public void TestStopSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "stop" });
        Assert.AreEqual("localhost", matches.Value("host"));
        Assert.IsNotNull(matches.SubCommandMatches("stop"));
    }

    [TestMethod]
    public void TestSetVolumeSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "setvolume", "-v", "0.5" });
        Assert.AreEqual("localhost", matches.Value("host"));

        var setVolumeMatches = matches.SubCommandMatches("setvolume")!;
        Assert.AreEqual("0.5", setVolumeMatches.Value("volume"));
    }

    [TestMethod]
    public void TestSetSpeedSubcommand()
    {
        var matches = _rootCommand.Parse(new string[] { "-h", "localhost", "setspeed", "-s", "2.0" });
        Assert.AreEqual("localhost", matches.Value("host"));

        var setSpeedMatches = matches.SubCommandMatches("setspeed")!;
        Assert.AreEqual("2.0", setSpeedMatches.Value("speed"));
    }

    [TestMethod]
    public void TestValueAsSByte()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "127" });
        Assert.AreEqual((sbyte)127, matches.ValueAsSByte("someOption"));
    }

    [TestMethod]
    public void TestValueAsByte()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "255" });
        Assert.AreEqual((byte)255, matches.ValueAsByte("someOption"));
    }

    [TestMethod]
    public void TestValueAsUInt16()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "65535" });
        Assert.AreEqual((ushort)65535, matches.ValueAsUInt16("someOption"));
    }

    [TestMethod]
    public void TestValueAsUInt32()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "4294967295" });
        Assert.AreEqual(4294967295u, matches.ValueAsUInt32("someOption"));
    }

    [TestMethod]
    public void TestValueAsUInt64()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "18446744073709551615" });
        Assert.AreEqual(18446744073709551615ul, matches.ValueAsUInt64("someOption"));
    }

    [TestMethod]
    public void TestValueAsInt16()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "32767" });
        Assert.AreEqual((short)32767, matches.ValueAsInt16("someOption"));
    }

    [TestMethod]
    public void TestValueAsInt32()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "2147483647" });
        Assert.AreEqual(2147483647, matches.ValueAsInt32("someOption"));
    }

    [TestMethod]
    public void TestValueAsInt64()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "9223372036854775807" });
        Assert.AreEqual(9223372036854775807L, matches.ValueAsInt64("someOption"));
    }

    [TestMethod]
    public void TestValueAsFloat()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "3.14" });
        Assert.AreEqual(3.14f, matches.ValueAsFloat("someOption"));
    }

    [TestMethod]
    public void TestValueAsDouble()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "3.14159" });
        Assert.AreEqual(3.14159, matches.ValueAsDouble("someOption"));
    }

    [TestMethod]
    public void TestValueAsDecimal()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "79228162514264337593543950335" });
        Assert.AreEqual(79228162514264337593543950335m, matches.ValueAsDecimal("someOption"));
    }

    [TestMethod]
    public void TestValueAsBool()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "true" });
        Assert.AreEqual(true, matches.ValueAsBool("someOption"));
    }

    [TestMethod]
    public void TestValueAsDateTime()
    {
        var matches = _someRoot.Parse(new string[] { "--someOption", "2024-01-01" });
        Assert.AreEqual(new DateTime(2024, 1, 1), matches.ValueAsDateTime("someOption"));
    }
}
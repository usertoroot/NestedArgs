using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedArgs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NestedArgs.Tests
{
    [TestClass]
    public class MainTests
    {
        #region Parsing Options

        [TestMethod]
        public void TestParsingOptions()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", ShortName = 'h', Description = "Host", TakesValue = true })
                .Option(new Option { LongName = "encrypted", ShortName = 'e', Description = "Encrypted", TakesValue = false })
                .Option(new Option { LongName = "dummy", ShortName = 'd', Description = "Dummy", TakesValue = true, AllowMultiple = true })
                .Build();

            // Long option with space
            var result1 = cmd.Parse(new[] { "--host", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Success, result1.Status);
            Assert.AreEqual("127.0.0.1", result1.Matches!.Value("host"));

            // Long option with equals
            var result2 = cmd.Parse(new[] { "--host=127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Success, result2.Status);
            Assert.AreEqual("127.0.0.1", result2.Matches!.Value("host"));

            // Short option with space
            var result3 = cmd.Parse(new[] { "-h", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Success, result3.Status);
            Assert.AreEqual("127.0.0.1", result3.Matches!.Value("host"));

            // Short option with equals
            var result4 = cmd.Parse(new[] { "-h=127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Success, result4.Status);
            Assert.AreEqual("127.0.0.1", result4.Matches!.Value("host"));

            // Flag option long
            var result5 = cmd.Parse(new[] { "--encrypted" })!;
            Assert.AreEqual(ParseStatus.Success, result5.Status);
            Assert.IsTrue(result5.Matches!.Has("encrypted"));

            // Flag option short
            var result6 = cmd.Parse(new[] { "-e" })!;
            Assert.AreEqual(ParseStatus.Success, result6.Status);
            Assert.IsTrue(result6.Matches!.Has("encrypted"));

            // Multiple values with long option
            var result7 = cmd.Parse(new[] { "--dummy", "a", "--dummy", "b" })!;
            Assert.AreEqual(ParseStatus.Success, result7.Status);
            CollectionAssert.AreEqual(new[] { "a", "b" }, result7.Matches!.Values("dummy"));

            // Multiple values with short option
            var result8 = cmd.Parse(new[] { "-d", "a", "-d", "b" })!;
            Assert.AreEqual(ParseStatus.Success, result8.Status);
            CollectionAssert.AreEqual(new[] { "a", "b" }, result8.Matches!.Values("dummy"));
        }

        #endregion

        #region Required Options and Defaults

        [TestMethod]
        public void TestRequiredOptionsAndDefaults()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host", IsRequired = true })
                .Option(new Option { LongName = "port", Description = "Port", DefaultValue = "80" })
                .Build();

            // Required option provided
            var result1 = cmd.Parse(new[] { "--host", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Success, result1.Status);
            Assert.AreEqual("127.0.0.1", result1.Matches!.Value("host"));
            Assert.AreEqual("80", result1.Matches!.Value("port"));

            // Required option missing
            var result2 = cmd.Parse(new string[0])!;
            Assert.AreEqual(ParseStatus.Failure, result2.Status);
            Assert.IsTrue(result2.Error!.Message.Contains("host"));

            // Override default
            var result3 = cmd.Parse(new[] { "--host", "127.0.0.1", "--port", "8080" })!;
            Assert.AreEqual(ParseStatus.Success, result3.Status);
            Assert.AreEqual("8080", result3.Matches!.Value("port"));

            // Default with empty value
            var result4 = cmd.Parse(new[] { "--host", "127.0.0.1", "--port", "" })!;
            Assert.AreEqual(ParseStatus.Success, result4.Status);
            Assert.AreEqual("", result4.Matches!.Value("port"));
        }

        #endregion

        #region Subcommands

        [TestMethod]
        public void TestSubcommands()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host", IsRequired = true })
                .SubCommand(new CommandBuilder("play")
                    .Option(new Option { LongName = "url", Description = "URL", IsRequired = true })
                    .Build())
                .Build();

            // Basic subcommand parsing
            var result1 = cmd.Parse(new[] { "--host", "127.0.0.1", "play", "--url", "http://example.com" })!;
            Assert.AreEqual(ParseStatus.Success, result1.Status);
            Assert.AreEqual("127.0.0.1", result1.Matches!.Value("host"));
            var playMatches = result1.Matches!.SubCommandMatches("play");
            Assert.IsNotNull(playMatches);
            Assert.AreEqual("http://example.com", playMatches.Value("url"));

            // Subcommand with missing required option
            var result2 = cmd.Parse(new[] { "--host", "127.0.0.1", "play" })!;
            Assert.AreEqual(ParseStatus.Failure, result2.Status);
            Assert.IsTrue(result2.Error!.Message.Contains("url"));
        }

        #endregion

        #region Option Groups

        [TestMethod]
        public void TestOptionGroups_ExactlyOne()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host", IsRequired = true })
                .OptionGroup("source", "Media source", GroupConstraint.ExactlyOne, g => g
                    .Option(new Option { LongName = "file", Description = "File" })
                    .Option(new Option { LongName = "url", Description = "URL" }))
                .Build();

            // Zero options
            var result1 = cmd.Parse(new[] { "--host", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Failure, result1.Status);
            Assert.IsTrue(result1.Error!.Message.Contains("Exactly one"));

            // One option
            var result2 = cmd.Parse(new[] { "--host", "127.0.0.1", "--file", "a.txt" })!;
            Assert.AreEqual(ParseStatus.Success, result2.Status);
            Assert.AreEqual("a.txt", result2.Matches!.Value("file"));

            // Two options
            var result3 = cmd.Parse(new[] { "--host", "127.0.0.1", "--file", "a.txt", "--url", "http://x" })!;
            Assert.AreEqual(ParseStatus.Failure, result3.Status);
            Assert.IsTrue(result3.Error!.Message.Contains("Exactly one"));
        }

        [TestMethod]
        public void TestOptionGroupConstraints()
        {
            // Option with IsRequired in group
            try
            {
                new CommandBuilder("test")
                    .OptionGroup("source", "Source", GroupConstraint.ExactlyOne, g => g
                        .Option(new Option { LongName = "file", Description = "File", IsRequired = true }))
                    .Build();
                Assert.Fail("Expected CommandException for required option in group");
            }
            catch (CommandException ex)
            {
                Assert.IsTrue(ex.Message.Contains("cannot have IsRequired"));
            }

            // Option with DefaultValue in group
            try
            {
                new CommandBuilder("test")
                    .OptionGroup("source", "Source", GroupConstraint.ExactlyOne, g => g
                        .Option(new Option { LongName = "file", Description = "File", DefaultValue = "default" }))
                    .Build();
                Assert.Fail("Expected CommandException for default value in group");
            }
            catch (CommandException ex)
            {
                Assert.IsTrue(ex.Message.Contains("cannot have IsRequired"));
            }
        }

        #endregion

        #region Help Functionality

        [TestMethod]
        public void TestHelpFunctionality()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host" })
                .SubCommand(new CommandBuilder("play")
                    .Option(new Option { LongName = "url", Description = "URL" })
                    .Build())
                .Build();

            // Root-level help
            var result1 = cmd.Parse(new[] { "--help" })!;
            Assert.AreEqual(ParseStatus.HelpRequested, result1.Status);
            Assert.AreEqual(cmd, result1.HelpCommand);

            // Subcommand help
            var result2 = cmd.Parse(new[] { "play", "--help" })!;
            Assert.AreEqual(ParseStatus.HelpRequested, result2.Status);
            Assert.AreEqual(cmd.SubCommands["play"], result2.HelpCommand);
        }

        #endregion

        #region Type Conversions

        [TestMethod]
        public void TestTypeConversions()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "num", Description = "Number", TakesValue = true })
                .Option(new Option { LongName = "date", Description = "Date", TakesValue = true })
                .Build();

            // Valid conversions
            var result1 = cmd.Parse(new[] { "--num", "42", "--date", "2023-01-01" })!;
            Assert.AreEqual(ParseStatus.Success, result1.Status);
            Assert.AreEqual(42, result1.Matches!.ValueAsInt32("num"));
            Assert.AreEqual(42.0, result1.Matches!.ValueAsDouble("num"));
            Assert.AreEqual(new DateTime(2023, 1, 1), result1.Matches!.ValueAsDateTime("date"));

            // Invalid conversion
            var result2 = cmd.Parse(new[] { "--num", "abc" })!;
            Assert.AreEqual(ParseStatus.Success, result2.Status);
            Assert.IsNull(result2.Matches!.ValueAsInt32("num"));
        }

        #endregion

        #region Error Handling

        [TestMethod]
        public void TestErrorHandling()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host", IsRequired = true })
                .Option(new Option { LongName = "flag", Description = "Flag", TakesValue = false })
                .Option(new Option { LongName = "opt", Description = "Opt", TakesValue = true, AllowMultiple = false })
                .Build();

            // Missing required option
            var result1 = cmd.Parse(new string[0])!;
            Assert.AreEqual(ParseStatus.Failure, result1.Status);
            Assert.IsTrue(result1.Error!.Message.Contains("host"));

            // Unknown option
            var result2 = cmd.Parse(new[] { "--unknown" })!;
            Assert.AreEqual(ParseStatus.Failure, result2.Status);
            Assert.IsTrue(result2.Error!.Message.Contains("unknown"));

            // Value provided to flag
            var result3 = cmd.Parse(new[] { "--flag=value" })!;
            Assert.AreEqual(ParseStatus.Failure, result3.Status);
            Assert.IsTrue(result3.Error!.Message.Contains("does not take a value"));

            // Multiple occurrences when not allowed
            var result4 = cmd.Parse(new[] { "--opt", "a", "--opt", "b" })!;
            Assert.AreEqual(ParseStatus.Failure, result4.Status);
            Assert.IsTrue(result4.Error!.Message.Contains("provided more than once"));
        }

        [TestMethod]
        public void TestFuzzyMatching()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host" })
                .SubCommand(new CommandBuilder("play").Build())
                .Build();

            // Option typo
            var result1 = cmd.Parse(new[] { "--hots" })!;
            Assert.AreEqual(ParseStatus.Failure, result1.Status);
            Assert.IsTrue(result1.Error!.Message.Contains("host"));

            // Subcommand typo
            var result2 = cmd.Parse(new[] { "plya" })!;
            Assert.AreEqual(ParseStatus.Failure, result2.Status);
            Assert.IsTrue(result2.Error!.Message.Contains("play"));
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void TestEdgeCases()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host" })
                .Option(new Option { LongName = "a", Description = "a", ShortName = 'a', TakesValue = false })
                .Option(new Option { LongName = "b", Description = "b", ShortName = 'b', TakesValue = false })
                .Option(new Option { LongName = "c", Description = "c", ShortName = 'c', TakesValue = true })
                .Build();

            // Empty value
            var result1 = cmd.Parse(new[] { "--host=" })!;
            Assert.AreEqual(ParseStatus.Success, result1.Status);
            Assert.AreEqual("", result1.Matches!.Value("host"));

            // Value starting with dash
            var result2 = cmd.Parse(new[] { "--host", "-localhost" })!;
            Assert.AreEqual(ParseStatus.Success, result2.Status);
            Assert.AreEqual("-localhost", result2.Matches!.Value("host"));

            // Short option grouping
            var result3 = cmd.Parse(new[] { "-abc", "value" })!;
            Assert.AreEqual(ParseStatus.Success, result3.Status);
            Assert.IsTrue(result3.Matches!.Has("a"));
            Assert.IsTrue(result3.Matches!.Has("b"));
            Assert.AreEqual("value", result3.Matches!.Value("c"));

            // Case sensitivity
            var result4 = cmd.Parse(new[] { "--Host", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Failure, result4.Status);
            Assert.IsTrue(result4.Error!.Message.Contains("Host"));

            // Non-ASCII characters
            var result5 = cmd.Parse(new[] { "--host", "café" })!;
            Assert.AreEqual(ParseStatus.Success, result5.Status);
            Assert.AreEqual("café", result5.Matches!.Value("host"));

            // Very long value
            var longValue = new string('a', 10000);
            var result6 = cmd.Parse(new[] { "--host", longValue })!;
            Assert.AreEqual(ParseStatus.Success, result6.Status);
            Assert.AreEqual(longValue, result6.Matches!.Value("host"));
        }

        [TestMethod]
        public void TestOptionAfterSubcommand()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host" })
                .SubCommand(new CommandBuilder("play").Build())
                .Build();

            var result = cmd.Parse(new[] { "play", "--host", "127.0.0.1" })!;
            Assert.AreEqual(ParseStatus.Failure, result.Status);
            Assert.IsTrue(result.Error!.Message.Contains("--host"));
        }

        [TestMethod]
        public void TestGlobalOptionBeforeSubcommand()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "host", Description = "Host" })
                .SubCommand(new CommandBuilder("play")
                    .Option(new Option { LongName = "url", Description = "URL" })
                    .Build())
                .Build();

            var result = cmd.Parse(new[] { "--host", "127.0.0.1", "play", "--url", "http://example.com" })!;
            Assert.AreEqual(ParseStatus.Success, result.Status);
            Assert.AreEqual("127.0.0.1", result.Matches!.Value("host"));
            var playMatches = result.Matches!.SubCommandMatches("play");
            Assert.AreEqual("http://example.com", playMatches!.Value("url"));
        }

        [TestMethod]
        public void TestOptionNameConflict()
        {
            var cmd = new CommandBuilder("test")
                .Option(new Option { LongName = "opt", Description = "Global opt" })
                .SubCommand(new CommandBuilder("sub")
                    .Option(new Option { LongName = "opt", Description = "Sub opt" })
                    .Build())
                .Build();

            var result = cmd.Parse(new[] { "--opt", "global", "sub", "--opt", "sub" })!;
            Assert.AreEqual(ParseStatus.Success, result.Status);
            Assert.AreEqual("global", result.Matches!.Value("opt"));
            var subMatches = result.Matches!.SubCommandMatches("sub");
            Assert.AreEqual("sub", subMatches!.Value("opt"));
        }

        #endregion
    }
}
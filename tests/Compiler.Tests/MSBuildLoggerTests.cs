﻿namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NSubstitute;
    using Xunit;

    public class MSBuildLoggerTests
    {
        private readonly List<BuildErrorEventArgs> errors = new List<BuildErrorEventArgs>();
        private readonly MSBuildLogger logger;
        private readonly List<BuildMessageEventArgs> messages = new List<BuildMessageEventArgs>();

        private MSBuildLoggerTests()
        {
            IBuildEngine engine = Substitute.For<IBuildEngine>();
            engine.LogErrorEvent(Arg.Do<BuildErrorEventArgs>(this.errors.Add));
            engine.LogMessageEvent(Arg.Do<BuildMessageEventArgs>(this.messages.Add));

            this.logger = new MSBuildLogger(new TaskLoggingHelper(engine, "TestTask"));
        }

        public sealed class DebugTests : MSBuildLoggerTests
        {
            [Fact]
            public void ShouldLogAsLowImportance()
            {
                this.logger.Debug("Test {0}", "message");

                BuildMessageEventArgs message = this.messages.Single();
                message.Importance.Should().Be(MessageImportance.Low);
                message.Message.Should().Be("Test message");
            }
        }

        public sealed class ErrorTests : MSBuildLoggerTests
        {
            [Fact]
            public void ShouldLogExceptions()
            {
                this.logger.Error(new ArgumentException("Exception message"));

                BuildErrorEventArgs error = this.errors.Single();
                error.Message.Should().Be("Exception message");
            }

            [Fact]
            public void ShouldLogMessages()
            {
                this.logger.Error(
                    "Test {0}",
                    "message");

                BuildErrorEventArgs error = this.errors.Single();
                error.Message.Should().Be("Test message");
            }
        }

        public sealed class InfoTests : MSBuildLoggerTests
        {
            [Fact]
            public void ShouldLogAsNomralImportance()
            {
                this.logger.Info("Test {0}", "message");

                BuildMessageEventArgs message = this.messages.Single();
                message.Importance.Should().Be(MessageImportance.Normal);
                message.Message.Should().Be("Test message");
            }
        }
    }
}

Feature: Configuration
    In order to create flexible programs
    As a developer
    I want to be able to provide runtime configuration options

Scenario: Loading strongly typed values
    Given I have the following code
    """
    using System;
    using Autocrat.Abstractions;

    [Configuration]
    public class TestConfig
    {
        public int Integer { get; set; }
    }

    public class Initializer : IInitializer
    {
        private readonly TestConfig config;

        public Initializer(TestConfig config)
        {
            this.config = config;
        }

        public void OnConfigurationLoaded()
        {
            Console.WriteLine("{0}", this.config.Integer);
            Environment.Exit(0);
        }
    }
    """
    And I have the following configuration
    """
    {
        "integer": 123,
    }
    """
    When I compile the code
    And I run the native program
    Then it's output should contain "123"

Scenario: Loading subsections
    Given I have the following code
    """
    using System;
    using Autocrat.Abstractions;

    [Configuration]
    public class RootConfig
    {
        public SubConfig SectionName { get; set; }
    }

    public class SubConfig
    {
        public int Integer { get; set; }
    }

    public class Initializer : IInitializer
    {
        private readonly SubConfig config;

        public Initializer(SubConfig config)
        {
            this.config = config;
        }

        public void OnConfigurationLoaded()
        {
            Console.WriteLine("{0}", this.config.Integer);
            Environment.Exit(0);
        }
    }
    """
    And I have the following configuration
    """
    {
        "sectionName": {
            "integer": 123,
        },
    }
    """
    When I compile the code
    And I run the native program
    Then it's output should contain "123"

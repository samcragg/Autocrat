Feature: Compilation
    In order to create a fast program
    As a developer
    I want to be able to produce a native executable

Scenario: A basic program
    Given I have the following code
    """
    using System;
    using Autocrat.Abstractions;

    public class Initializer : IInitializer
    {
        public void OnConfigurationLoaded()
        {
            Console.WriteLine("Configuration loaded called");
            Environment.Exit(0);
        }
    }
    """
    When I compile the code
    And I run the native program
    Then it's output should contain "Configuration loaded called"

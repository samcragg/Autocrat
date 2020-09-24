Feature: ArgumentParsing
    In order to allow different runtime behaviour
    As a user
    I want to run the program with different command line arguments

Background:
    Given I have the following code
    """
    using System;
    using Autocrat.Abstractions;

    public class Initializer : IInitializer
    {
        public void OnConfigurationLoaded()
        {
            Environment.Exit(1);
        }
    }
    """

Scenario: Asking for help
    Given the project description is "My test program"
    When I compile the code
    And I run the native program with "--help"
    Then it's output should contain "My test program"

Scenario: Getting the version
    Given the project version is "2.3.4-alpha"
    When I compile the code
    And I run the native program with "--version"
    Then it's output should contain "2.3.4-alpha"

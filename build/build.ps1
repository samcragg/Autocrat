[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
    [switch]$DryRun,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

# Restore the tool
Invoke-Expression "dotnet tool restore"

# Build an array (not a string) of Cake arguments to be joined later
$cakeArguments = @()
if ($Script) { $cakeArguments += "`"$Script`"" }
if ($Target) { $cakeArguments += "--target=`"$Target`"" }
if ($Verbosity) { $cakeArguments += "--verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "--showdescription" }
if ($DryRun) { $cakeArguments += "--dryrun" }
$cakeArguments += $ScriptArgs

# Start Cake
Invoke-Expression "dotnet tool run dotnet-cake -- $($cakeArguments -join " ")"
exit $LASTEXITCODE

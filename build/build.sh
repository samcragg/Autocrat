#!/usr/bin/env bash

SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

# Restore the tool
dotnet tool restore

# Define default arguments
SCRIPT="build.cake"
CAKE_ARGUMENTS=()

# Parse arguments
for i in "$@"; do
    case $1 in
        -s|--script) SCRIPT="$2"; shift ;;
        --) shift; CAKE_ARGUMENTS+=("$@"); break ;;
        *) CAKE_ARGUMENTS+=("$1") ;;
    esac
    shift
done

# Start Cake
dotnet tool run dotnet-cake -- "$SCRIPT" $CAKE_ARGUMENTS

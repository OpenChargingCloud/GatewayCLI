#!/bin/bash
#
# Pull everything and build it.

set -e

cd "$(dirname "$0")"

git submodule foreach git pull
git pull
#dotnet build GatewayCLI.slnx --configuration Release
dotnet build GatewayCLI.slnx

#!/bin/bash
#
# Pull everything and build it.

set -e

cd "$(dirname "$0")"

git pull --ff-only
git submodule update --init --recursive
git submodule foreach git checkout master
git submodule foreach git pull
npm --prefix /home/ahzf/GatewayCLI/libs/Gateway/Gateway/Frontend ci
#dotnet build GatewayCLI.slnx --configuration Release
dotnet build GatewayCLI.slnx

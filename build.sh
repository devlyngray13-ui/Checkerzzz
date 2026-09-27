#!/usr/bin/env bash
set -e
dotnet build QuestFOV.csproj -c Release
echo
echo "BUILD COMPLETE:"
ls -lh bin/Release/net6.0/QuestFOV.dll

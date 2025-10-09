#!/bin/sh -f

PROJECT_FOLDER=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

# Run the tests
dotnet test \
    --settings "$PROJECT_FOLDER/BaseStationReader.Tests/mstest.runsettings" \
    -p:UseSharedCompilation=false \
    -nr:false

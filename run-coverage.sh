#!/bin/sh -f

# Per the following article:
#
# https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=linux
#
# Requires installation of the report generator:
#
# dotnet tool install -g dotnet-reportgenerator-globaltool

PROJECT_FOLDER=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
TEST_RESULTS_FOLDER_NAME="TestResults/"
TEST_RESULTS_FOLDER_PATH="$PROJECT_FOLDER/BaseStationReader.Tests/$TEST_RESULTS_FOLDER_NAME"
REPORT_FOLDER="$TEST_RESULTS_FOLDER_PATH/report"

echo ""
echo "Project folder           : $PROJECT_FOLDER"
echo "Test results folder name : $TEST_RESULTS_FOLDER_NAME"
echo "Test results folder path : $TEST_RESULTS_FOLDER_PATH"
echo "Cobertura report folder  : $REPORT_FOLDER"
echo ""

dotnet test "$PROJECT_FOLDER/BaseStationReader.sln" /p:CollectCoverage=true /p:CoverletOutput=$TEST_RESULTS_FOLDER_NAME /p:CoverletOutputFormat=cobertura
reportgenerator -reports:"$TEST_RESULTS_FOLDER_PATH/coverage.cobertura.xml" -targetdir:"$REPORT_FOLDER" -reporttypes:Html
open "$REPORT_FOLDER/index.html"
#!/bin/sh -f

PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
TODAY=$(date +"%Y-%m-%d")
JSON_FOLDER="$PROJECT_FOLDER/data/json/$TODAY"
LOOKUP_TOOL_FOLDER="$PROJECT_FOLDER/BaseStationReader.Lookup"
LOOKUP_TOOL_EXE="$LOOKUP_TOOL_FOLDER/bin/Debug/net9.0/BaseStationReader.Lookup"

echo
echo "Project Folder     : $PROJECT_FOLDER"
echo "JSON Folder        : $JSON_FOLDER"
echo "Lookup Tool Folder : $LOOKUP_TOOL_FOLDER"
echo "Lookup Tool Binary : $LOOKUP_TOOL_EXE"
echo "IATA List          : $1"
echo

# Make sure the input file exists
if [ ! -f "$1" ]; then
    echo "Input file '$1' does not exist"
    echo
    exit 1
fi

# Make sure the date folder for today exists
if [ ! -d "$JSON_FOLDER" ]; then
    mkdir -p "$JSON_FOLDER"
fi

# Change to the lookup tool folder and build it
cd "$LOOKUP_TOOL_FOLDER"
dotnet build
cd "$PROJECT_FOLDER"

# Check the built application exists
if [ ! -f "$LOOKUP_TOOL_EXE" ]; then
    echo "Lookup tool executable not found"
    echo
    exit 1
fi

# Iterate over the content of the IATA list
"$LOOKUP_TOOL_EXE" --export-schedule "$1" "$JSON_FOLDER"

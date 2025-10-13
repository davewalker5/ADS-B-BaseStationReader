#!/bin/sh -f

PROJECT_FOLDER=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
DATA_FOLDER="$PROJECT_FOLDER/data"
JSON_FOLDER="$DATA_FOLDER/json"
CSV_FILE="$DATA_FOLDER/flight_number_mappings.csv"

echo
echo "Project Folder : $PROJECT_FOLDER"
echo "Data Folder    : $DATA_FOLDER"
echo "JSON Folder    : $JSON_FOLDER"
echo "CSV File       : $CSV_FILE"
echo

# Build the CSV file
python create-flight-mapping-csv.py -i "$JSON_FOLDER" -o "$CSV_FILE"
if [ $? -ne 0 ]; then
    exit 1
fi

# Import the CSV file
cd "$PROJECT_FOLDER/BaseStationReader.Lookup"
dotnet run -- -im "$CSV_FILE"
cd "$PROJECT_FOLDER"

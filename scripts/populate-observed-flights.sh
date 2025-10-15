#!/bin/sh -f

PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
SQL_FOLDER="$PROJECT_FOLDER/sql/mapping"
CREATE_TABLE_QUERY="$SQL_FOLDER/create-observed-flight-table.sql"
EXPORT_QUERY="$SQL_FOLDER/export-observed-flights.sql"
CSV_FILE_PATH="$PROJECT_FOLDER/data/observed-flights.csv"

echo
echo "Project folder     : $PROJECT_FOLDER"
echo "SQL query folder   : $SQL_FOLDER"
echo "Create table query : $CREATE_TABLE_QUERY"
echo "Export query       : $EXPORT_QUERY"
echo "CSV file path      : $CSV_FILE_PATH"
echo

sqlite3 "$AIRCRAFT_TRACKER_DB" ".read '$CREATE_TABLE_QUERY'"
sqlite3 -header -csv "$FLIGHT_RECORDER_DB" ".read '$EXPORT_QUERY'" > "$CSV_FILE_PATH"
sqlite3 "$AIRCRAFT_TRACKER_DB" ".mode csv" ".import --csv --skip 1 '$CSV_FILE_PATH' OBSERVED_FLIGHT"
rm "$CSV_FILE_PATH"

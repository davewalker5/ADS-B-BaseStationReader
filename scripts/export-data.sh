#!/bin/sh -f

PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
DATA_FOLDER="$PROJECT_FOLDER/data"
SQL_FOLDER="$PROJECT_FOLDER/sql/queries"

sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-airlines.sql'" > "$DATA_FOLDER/airlines.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft-manufacturers.sql'" > "$DATA_FOLDER/manufacturers.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft-models.sql'" > "$DATA_FOLDER/models.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft.sql'" > "$DATA_FOLDER/aircraft.csv"

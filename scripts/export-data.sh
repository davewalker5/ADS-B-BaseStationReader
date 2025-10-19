#!/bin/sh -f

PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
DATA_FOLDER="$PROJECT_FOLDER/data/export"
SQL_FOLDER="$PROJECT_FOLDER/sql/queries"

mkdir -p "$DATA_FOLDER"

# Formatted output : Suitable for re-import using the lookup tool
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-airlines.sql'" > "$DATA_FOLDER/airlines.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft-manufacturers.sql'" > "$DATA_FOLDER/manufacturers.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft-models.sql'" > "$DATA_FOLDER/models.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" ".read '$SQL_FOLDER/list-aircraft.sql'" > "$DATA_FOLDER/aircraft.csv"

# Raw output : Curated data
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM FLIGHT_NUMBER_MAPPING" > "$DATA_FOLDER/flight-number-mappings.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM EXCLUDED_ADDRESS" > "$DATA_FOLDER/excluded-addresses.csv"

# Raw output : Tracking data
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM TRACKED_AIRCRAFT" > "$DATA_FOLDER/tracked-aircraft.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM POSITION" > "$DATA_FOLDER/tracked-aircraft-positions.csv"

# Raw output : Observed flights and sightings
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM FLIGHT" > "$DATA_FOLDER/observed-flights.csv"
sqlite3 -header -csv "$AIRCRAFT_TRACKER_DB" "SELECT * FROM SIGHTING" > "$DATA_FOLDER/observed-sightings.csv"

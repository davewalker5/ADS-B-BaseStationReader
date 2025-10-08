import os
import json
import csv
import argparse
from pprint import pprint as pp

# Keys for incoming fields in the scheule JSON files
IN_MOVEMENT_KEY = "movement"
IN_AIRPORT_KEY = "airport"
IN_AIRLINE_KEY = "airline"
IN_FLIGHT_NUMBER_KEY = "number"
IN_CALLSIGN_KEY = "callSign"
IN_AIRLINE_IATA_KEY = "iata"
IN_AIRLINE_ICAO_KEY = "icao"
IN_AIRLINE_NAME_KEY = "name"
IN_AIRPORT_IATA_KEY = "iata"
IN_AIRPORT_ICAO_KEY = "icao"
IN_AIRPORT_NAME_KEY = "name"

# Keys for outgoing fields on individual flight objects
OUT_FILENAME_KEY = "filename"
OUT_DIRECTION_KEY = "direction"
OUT_FLIGHT_NUMBER_KEY = "flight_iata"
OUT_CALLSIGN_KEY = "callsign"
OUT_AIRLINE_IATA_KEY = "airline_iata"
OUT_AIRLINE_ICAO_KEY = "airline_icao"
OUT_AIRLINE_NAME_KEY = "airline_name"
OUT_AIRPORT_IATA_KEY = "airport_iata"
OUT_AIRPORT_ICAO_KEY = "airport_icao"
OUT_AIRPORT_NAME_KEY = "airport_name"

# Argument parser configuration
TITLE = "Extract flight-to-callsign mappings from a set of JSON flight schedules"


def extract_flight(json):
    # Extract the members of the structure containing properties of interest
    movement = json.get(IN_MOVEMENT_KEY, {})
    airport = movement.get(IN_AIRPORT_KEY, {})
    airline = json.get(IN_AIRLINE_KEY, {})

    # Extract the flight properties
    number = json.get(IN_FLIGHT_NUMBER_KEY, "").replace(" ", "").strip()
    callsign = json.get(IN_CALLSIGN_KEY, "").replace(" ", "").strip()
    airline_iata = airline.get(IN_AIRLINE_IATA_KEY, "").replace(" ", "").strip()
    airline_icao = airline.get(IN_AIRLINE_ICAO_KEY, "").replace(" ", "").strip()
    airline_name = airline.get(IN_AIRLINE_NAME_KEY, "").replace(" ", "").strip()
    airport_iata = airport.get(IN_AIRPORT_IATA_KEY, "").replace(" ", "").strip()
    airport_icao = airport.get(IN_AIRPORT_ICAO_KEY, "").replace(" ", "").strip()
    airport_name = airport.get(IN_AIRPORT_NAME_KEY, "").replace(" ", "").strip()

    # We need all the properties in order to construct and return a valid flight
    if not number or not callsign or not airline_iata or not airline_icao:
        return None

    # Construct and return a flight dictionary
    return {
        OUT_CALLSIGN_KEY: callsign,
        OUT_FLIGHT_NUMBER_KEY: number,
        OUT_AIRLINE_IATA_KEY: airline_iata,
        OUT_AIRLINE_ICAO_KEY: airline_icao,
        OUT_AIRLINE_NAME_KEY: airline_name,
        OUT_AIRPORT_IATA_KEY: airport_iata,
        OUT_AIRPORT_ICAO_KEY: airport_icao,
        OUT_AIRPORT_NAME_KEY: airport_name,
    }


def extract_flights(filename, json, node):
    flights = []

    # The node should specify a list of flights (departures or arrivals)
    flight_list = json[node]
    if not flight_list:
        return flights

    # Iterate over the flight list
    for flight_dict in flight_list:
        # Extract the details for this flight and, if valid, add it to the list
        flight = extract_flight(flight_dict)
        if flight:
            flight[OUT_DIRECTION_KEY] = node.title()
            flight[OUT_FILENAME_KEY] = filename
            flights.append(flight)

    return flights


def load_flights_from_file(json_file):
    flights = []

    # Get the filename (without path) for reporting purposes
    filename = os.path.basename(json_file)

    # Open the file and load its content
    with open(json_file, "r") as file:
        content = json.load(file)
        if not content:
            print(f"Input file {json_file} is not a JSON file")
            return flights

    # Extract departure flight details
    departures = extract_flights(filename, content, "departures")
    if departures:
        flights = flights + departures

    # Extract arrival flight details
    arrivals = extract_flights(filename, content, "arrivals") or []
    if arrivals:
        flights = flights + arrivals

    print(f"{len(flights)} flights loaded from {filename}")
    return flights


def load_flights_from_files(json_files):
    flights = []

    # Check we have some JSON files
    if not json_files:
        return flights

    # Iterate over the schedule files
    for file in json_files:
        # Load the flights from this one and check there are some
        flights_from_file = load_flights_from_file(file)
        if flights_from_file:
            # There are, so append them to the list
            flights = flights + flights_from_file

    # Sort and de-duplicate based on callsign : The dictionary comprehension ensures duplicate
    # keys are overwritten and the sorted() function sorts the results
    initial_count = len(flights)
    flights = sorted({f["callsign"]: f for f in flights}.values(), key=lambda f: f["callsign"])

    # Report statistics on the flights retrieved
    unique_count = len(flights)
    removed_count = initial_count - unique_count
    print(f"{unique_count} unique flights loaded, {removed_count} non-unique flights removed")
    return flights


def find_json_files(folder):
    # Check the input folder exists
    if not os.path.exists(folder):
        print(f"Input folder {folder} does not exist")
        return None

    # Recursively walk the input folder
    json_files = []
    for root, _, files in os.walk(folder):
        # Get a list of JSON files
        matches = [os.path.join(root, f) for f in files if f.endswith(".json")]
        json_files = json_files + matches

    # Check we have some files to process
    if not json_files:
        print(f"No JSON files found in folder {folder}")

    print(f"{len(json_files)} schedule files found")
    return json_files


def write_flights_to_csv(flights, csv_file):
    # Check there are some flights to write
    if not flights:
        print("No flights to write to the CSV file")
        return

    # Determine the headers from the first flight object
    headers = flights[0].keys()

    # Open the CSV file for output and write the flights to it
    with open(csv_file, "w", newline="") as output_file:
        dict_writer = csv.DictWriter(output_file, headers)
        dict_writer.writeheader()
        dict_writer.writerows(flights)


def main():
    # Configure the command line parser and parse the arguments
    parser = argparse.ArgumentParser(description=TITLE)
    parser.add_argument("-i", "--input", help="Path to folder containing JSON schedule files")
    parser.add_argument("-o", "--output", help="Path to the output CSV file")
    args = parser.parse_args()

    # Compile a list of JSON schedule files in the input folder and its sub-folders
    files = find_json_files(args.input)

    # Load the flight details from the schedule files
    flights = load_flights_from_files(files)

    # Write the flight details to the output CSV file
    write_flights_to_csv(flights, args.output)


if __name__ == "__main__":
    main()

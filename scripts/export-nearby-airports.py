"""Export airports within a great-circle radius to CSV."""

from __future__ import annotations

import argparse
import csv
import math
import os
import sqlite3
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Sequence


EARTH_RADIUS_METRES = 6_371_000.0
METRES_PER_NAUTICAL_MILE = 1_852.0


class AirportExportError(Exception):
    """Describe an input or database error that prevents the export."""


@dataclass(frozen=True)
class AirportDistance:
    """Represent an airport and its distance from the supplied position."""

    iata: str
    icao: str
    name: str
    latitude: float
    longitude: float
    distance: float


def bounded_float(minimum: float, maximum: float):
    """Build an argparse converter for a bounded floating-point value."""

    def convert(value: str) -> float:
        try:
            number = float(value)
        except ValueError as error:
            raise argparse.ArgumentTypeError(f"'{value}' is not a number") from error
        if not math.isfinite(number) or not minimum <= number <= maximum:
            raise argparse.ArgumentTypeError(
                f"must be between {minimum:g} and {maximum:g}"
            )
        return number

    return convert


def positive_float(value: str) -> float:
    """Parse a finite, non-negative distance."""

    try:
        number = float(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError(f"'{value}' is not a number") from error
    if not math.isfinite(number) or number < 0:
        raise argparse.ArgumentTypeError("must be zero or greater")
    return number


def parse_arguments(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse command-line arguments."""

    parser = argparse.ArgumentParser()
    parser.add_argument("-la", "--latitude", type=bounded_float(-90, 90), required=True)
    parser.add_argument("-lo", "--longitude", type=bounded_float(-180, 180), required=True)
    parser.add_argument("-d", "--distance", type=positive_float, required=True,
                        help="Maximum distance from the position, in nautical miles")
    parser.add_argument("-db", "--database", type=Path, help="SQLite database")
    parser.add_argument("-o", "--output", type=Path, required=True, help="Destination CSV file")
    return parser.parse_args(arguments)


def resolve_database_path(argument_path: Path | None) -> Path:
    """Resolve an explicit database path or the project-standard environment variable."""

    configured = os.environ.get("AIRCRAFT_TRACKER_DB")
    path = argument_path or (Path(configured) if configured else None)
    if path is None:
        raise AirportExportError(
            "Specify --database or set the AIRCRAFT_TRACKER_DB environment variable."
        )
    resolved = path.expanduser().resolve()
    if not resolved.is_file():
        raise AirportExportError(f"Database does not exist: {resolved}")
    return resolved


def great_circle_distance_nautical_miles(
    latitude_1: float, longitude_1: float, latitude_2: float, longitude_2: float
) -> float:
    """Calculate the Haversine great-circle distance between two coordinates."""

    latitude_1_rad, latitude_2_rad = map(math.radians, (latitude_1, latitude_2))
    latitude_delta = latitude_2_rad - latitude_1_rad
    longitude_delta = math.radians(longitude_2 - longitude_1)
    haversine = (
        math.sin(latitude_delta / 2.0) ** 2
        + math.cos(latitude_1_rad)
        * math.cos(latitude_2_rad)
        * math.sin(longitude_delta / 2.0) ** 2
    )
    # Guard against tiny floating-point excursions above 1 at antipodal points.
    central_angle = 2.0 * math.asin(min(1.0, math.sqrt(haversine)))
    return central_angle * EARTH_RADIUS_METRES / METRES_PER_NAUTICAL_MILE


def find_airports(
    database_path: Path, latitude: float, longitude: float, maximum_distance: float
) -> list[AirportDistance]:
    """Read valid AIRPORT rows and return those inside the requested radius."""

    try:
        with sqlite3.connect(f"file:{database_path}?mode=ro", uri=True) as connection:
            rows = connection.execute(
                'SELECT IATA, ICAO, Name, Latitude, Longitude FROM AIRPORT '
                'WHERE Latitude IS NOT NULL AND Longitude IS NOT NULL'
            ).fetchall()
    except sqlite3.Error as error:
        raise AirportExportError(f"Unable to read AIRPORT: {error}") from error

    airports: list[AirportDistance] = []
    for iata, icao, name, airport_latitude, airport_longitude in rows:
        if not (-90 <= airport_latitude <= 90 and -180 <= airport_longitude <= 180):
            continue
        distance = great_circle_distance_nautical_miles(
            latitude, longitude, airport_latitude, airport_longitude
        )
        if distance <= maximum_distance:
            airports.append(
                AirportDistance(
                    iata or "", icao or "", name or "", airport_latitude,
                    airport_longitude, distance,
                )
            )
    return sorted(airports, key=lambda airport: (airport.distance, airport.name))


def write_csv(output_path: Path, airports: Sequence[AirportDistance]) -> None:
    """Write airport results to a UTF-8 CSV file."""

    try:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with output_path.open("w", newline="", encoding="utf-8") as output_file:
            writer = csv.writer(output_file)
            writer.writerow(["IATA", "ICAO", "Name", "Latitude", "Longitude", "Distance (nm)"])
            for airport in airports:
                writer.writerow(
                    [airport.iata, airport.icao, airport.name, airport.latitude,
                     airport.longitude, f"{airport.distance:.3f}"]
                )
    except OSError as error:
        raise AirportExportError(f"Unable to write CSV file: {error}") from error


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the airport export command."""

    args = parse_arguments(arguments)
    try:
        database_path = resolve_database_path(args.database)
        airports = find_airports(
            database_path, args.latitude, args.longitude, args.distance
        )
        write_csv(args.output, airports)
    except AirportExportError as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1

    print(f"Exported {len(airports)} airports to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

"""
Populate missing SQLite flight-path values from a CSV track
"""

from __future__ import annotations

import argparse
import csv
import math
import os
import sqlite3
import sys
from dataclasses import dataclass, replace
from datetime import datetime, timezone
from pathlib import Path
from typing import Sequence


EARTH_RADIUS_METRES = 6_371_000.0
METRES_PER_NAUTICAL_MILE = 1_852.0
REQUIRED_COLUMNS = {"Timestamp", "UTC", "Callsign", "Position", "Altitude"}


class PopulationError(Exception):
    """Describe an input or database problem that prevents safe population."""


@dataclass(frozen=True)
class CsvPosition:
    """Represent one validated position read from the source CSV file."""

    timestamp: datetime
    callsign: str
    latitude: float
    longitude: float
    altitude: float
    row_number: int


@dataclass(frozen=True)
class DatabasePosition:
    """Represent a candidate POSITION row and its current nullable values."""

    identifier: int
    timestamp: datetime
    altitude: float | None
    latitude: float | None
    longitude: float | None
    distance: float | None


def parse_arguments(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse command-line options.

    :param arguments: Optional argument sequence; defaults to process arguments.
    :return: Parsed command-line options.
    """
    # Keep the session and CSV positional because both are required for every run.
    parser = argparse.ArgumentParser()
    parser.add_argument("-s", "--session", type=int, help="SESSION.Id to update")
    parser.add_argument("-c", "--csv", type=Path, help="CSV file containing the flight path")
    parser.add_argument("-a", "--address", help="ICAO address, required when callsign lookup is inconclusive")
    parser.add_argument("-db", "--database",type=Path,help="SQLite database path")
    parser.add_argument("-dr", "--dry-run",action="store_true",
                        help="validate and report changes without committing them")
    return parser.parse_args(arguments)


def resolve_database_path(argument_path: Path | None) -> Path:
    """Resolve and validate the target database path.

    :param argument_path: Database path supplied on the command line, if any.
    :return: Existing SQLite database path.
    :raises PopulationError: If no usable database path is available.
    """
    # An explicit option takes precedence over the environment setting.
    raw_path = argument_path or (
        Path(os.environ["AIRCRAFT_TRACKER_DB"])
        if os.environ.get("AIRCRAFT_TRACKER_DB")
        else None
    )
    if raw_path is None:
        raise PopulationError(
            "Specify --database or set the AIRCRAFT_TRACKER_DB environment variable."
        )

    # Refuse to let sqlite silently create a database because of a mistyped path.
    database_path = raw_path.expanduser().resolve()
    if not database_path.is_file():
        raise PopulationError(f"Database does not exist: {database_path}")
    return database_path


def parse_csv_timestamp(row: dict[str, str], row_number: int) -> datetime:
    """Parse a CSV timestamp as a naive UTC clock value.

    :param row: CSV row keyed by column name.
    :param row_number: One-based file row number used in errors.
    :return: Naive UTC datetime ready for database time-basis alignment.
    :raises PopulationError: If neither timestamp representation is valid.
    """
    # Prefer the readable UTC value, retaining the Unix timestamp as a supported fallback.
    utc_text = (row.get("UTC") or "").strip()
    try:
        if utc_text:
            parsed = datetime.fromisoformat(utc_text.replace("Z", "+00:00"))
            if parsed.tzinfo is None:
                parsed = parsed.replace(tzinfo=timezone.utc)
        else:
            parsed = datetime.fromtimestamp(float(row["Timestamp"]), timezone.utc)
    except (KeyError, TypeError, ValueError) as error:
        raise PopulationError(f"CSV row {row_number} has an invalid timestamp.") from error

    # Retain the CSV's UTC clock value until the database range reveals its stored time basis.
    return parsed.astimezone(timezone.utc).replace(tzinfo=None)


def parse_position(value: str, row_number: int) -> tuple[float, float]:
    """Parse and validate a latitude/longitude CSV field.

    :param value: Comma-separated latitude and longitude text.
    :param row_number: One-based file row number used in errors.
    :return: Latitude and longitude as floating-point values.
    :raises PopulationError: If the coordinate is malformed or outside Earth bounds.
    """
    # Split only once so malformed extra components fail during numeric conversion.
    try:
        parts = [part.strip() for part in value.split(",")]
        if len(parts) != 2:
            raise ValueError
        latitude, longitude = (float(part) for part in parts)
    except (AttributeError, ValueError) as error:
        raise PopulationError(f"CSV row {row_number} has an invalid Position value.") from error
    if not (-90.0 <= latitude <= 90.0 and -180.0 <= longitude <= 180.0):
        raise PopulationError(f"CSV row {row_number} has coordinates outside valid bounds.")
    return latitude, longitude


def read_csv_positions(csv_path: Path) -> list[CsvPosition]:
    """Read and validate retrospective positions from a CSV file.

    :param csv_path: Source CSV path.
    :return: Validated positions in file order.
    :raises PopulationError: If the file or any required value is invalid.
    """
    # Validate the path before opening it to provide a concise diagnostic.
    if not csv_path.is_file():
        raise PopulationError(f"CSV file does not exist: {csv_path}")
    try:
        with csv_path.open("r", encoding="utf-8-sig", newline="") as source:
            reader = csv.DictReader(source)
            missing = REQUIRED_COLUMNS.difference(reader.fieldnames or [])
            if missing:
                raise PopulationError(f"CSV is missing required column(s): {', '.join(sorted(missing))}")

            positions: list[CsvPosition] = []
            for row_number, row in enumerate(reader, start=2):
                # Reject partial source rows rather than writing ambiguous data.
                callsign = (row.get("Callsign") or "").strip().upper()
                if not callsign:
                    raise PopulationError(f"CSV row {row_number} has no callsign.")
                latitude, longitude = parse_position(row.get("Position") or "", row_number)
                try:
                    altitude = float(row["Altitude"])
                except (KeyError, TypeError, ValueError) as error:
                    raise PopulationError(f"CSV row {row_number} has an invalid altitude.") from error
                positions.append(
                    CsvPosition(
                        parse_csv_timestamp(row, row_number),
                        callsign,
                        latitude,
                        longitude,
                        altitude,
                        row_number,
                    )
                )
    except OSError as error:
        raise PopulationError(f"Unable to read CSV file: {error}") from error

    # Aircraft inference is based on the first data row, as specified by the brief.
    if not positions:
        raise PopulationError("CSV contains no position records.")
    return positions


def validate_schema(connection: sqlite3.Connection) -> None:
    """Verify that the connected database has the required tracker tables.

    :param connection: Open SQLite connection.
    :return: None.
    :raises PopulationError: If required tables are absent.
    """
    # A schema check prevents cryptic SQL errors and accidental use of another SQLite file.
    rows = connection.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table'"
    ).fetchall()
    tables = {row[0].upper() for row in rows}
    missing = {"SESSION", "TRACKED_AIRCRAFT", "POSITION"}.difference(tables)
    if missing:
        raise PopulationError(f"Database is missing required table(s): {', '.join(sorted(missing))}")


def identify_address(
    connection: sqlite3.Connection,
    session_id: int,
    callsign: str,
    supplied_address: str | None,
) -> str:
    """Identify and validate an aircraft address within a session.

    :param connection: Open SQLite connection.
    :param session_id: Target observation session identifier.
    :param callsign: Callsign from the first CSV record.
    :param supplied_address: Optional command-line ICAO address.
    :return: Normalized address present in the session.
    :raises PopulationError: If the session or aircraft cannot be identified safely.
    """
    # Confirm the session independently so an empty session has a useful error.
    if connection.execute("SELECT 1 FROM SESSION WHERE Id = ?", (session_id,)).fetchone() is None:
        raise PopulationError(f"Session {session_id} does not exist.")

    if supplied_address:
        # An explicit address still has to belong to the selected session.
        address = supplied_address.strip().upper()
        match = connection.execute(
            """SELECT DISTINCT UPPER(TRIM(Address))
               FROM TRACKED_AIRCRAFT
               WHERE SessionId = ? AND UPPER(TRIM(Address)) = ?""",
            (session_id, address),
        ).fetchone()
        if match is None:
            raise PopulationError(f"Aircraft address {address} was not found in session {session_id}.")
        return match[0]

    # Multiple lifecycle segments with the same address collapse to one candidate.
    matches = connection.execute(
        """SELECT DISTINCT UPPER(TRIM(Address))
           FROM TRACKED_AIRCRAFT
           WHERE SessionId = ? AND UPPER(TRIM(Callsign)) = ?
           ORDER BY UPPER(TRIM(Address))""",
        (session_id, callsign.upper()),
    ).fetchall()
    if len(matches) == 1:
        return matches[0][0]
    if not matches:
        raise PopulationError(
            f"No aircraft in session {session_id} has callsign {callsign}; supply --address."
        )
    addresses = ", ".join(row[0] for row in matches)
    raise PopulationError(
        f"Callsign {callsign} maps to multiple addresses ({addresses}); supply --address."
    )


def parse_database_timestamp(value: str, position_id: int) -> datetime:
    """Parse a SQLite POSITION timestamp.

    :param value: SQLite datetime text.
    :param position_id: Position identifier used in errors.
    :return: Naive datetime suitable for local-time comparison.
    :raises PopulationError: If the stored value is not parseable.
    """
    # EF Core stores these local DateTime values as ISO-compatible text.
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (AttributeError, ValueError) as error:
        raise PopulationError(f"POSITION {position_id} has an invalid Timestamp: {value}") from error
    if parsed.tzinfo is not None:
        parsed = parsed.astimezone().replace(tzinfo=None)
    return parsed


def load_database_positions(
    connection: sqlite3.Connection, session_id: int, address: str
) -> list[DatabasePosition]:
    """Load positions across every matching aircraft lifecycle segment.

    :param connection: Open SQLite connection.
    :param session_id: Target observation session identifier.
    :param address: Normalized ICAO aircraft address.
    :return: Candidate database positions ordered by timestamp and identifier.
    :raises PopulationError: If no positions exist for the aircraft.
    """
    # Join through TRACKED_AIRCRAFT to include every same-address segment in the session.
    rows = connection.execute(
        """SELECT p.Id, p.Timestamp, p.Altitude, p.Latitude, p.Longitude, p.Distance
           FROM POSITION AS p
           INNER JOIN TRACKED_AIRCRAFT AS ta ON ta.Id = p.AircraftId
           WHERE ta.SessionId = ? AND UPPER(TRIM(ta.Address)) = ?
           ORDER BY p.Timestamp, p.Id""",
        (session_id, address),
    ).fetchall()
    positions = [
        DatabasePosition(row[0], parse_database_timestamp(row[1], row[0]), *row[2:])
        for row in rows
    ]
    if not positions:
        raise PopulationError(
            f"No POSITION records were found for aircraft {address} in session {session_id}."
        )
    return positions


def map_positions(
    csv_positions: Sequence[CsvPosition], database_positions: Sequence[DatabasePosition]
) -> dict[int, CsvPosition]:
    """Map CSV rows to nearest database positions, resolving collisions by proximity.

    :param csv_positions: Validated source positions.
    :param database_positions: Candidate POSITION records.
    :return: Mapping from POSITION identifier to its closest CSV row.
    """
    # Retain only the closest CSV row when several source rows select one POSITION record.
    mappings: dict[int, CsvPosition] = {}
    differences: dict[int, float] = {}
    for csv_position in csv_positions:
        closest = min(
            database_positions,
            key=lambda position: (
                abs((position.timestamp - csv_position.timestamp).total_seconds()),
                position.identifier,
            ),
        )
        difference = abs((closest.timestamp - csv_position.timestamp).total_seconds())
        if closest.identifier not in differences or difference < differences[closest.identifier]:
            mappings[closest.identifier] = csv_position
            differences[closest.identifier] = difference
    return mappings


def restrict_csv_positions_to_database_range(
    csv_positions: Sequence[CsvPosition],
    database_positions: Sequence[DatabasePosition],
) -> list[CsvPosition]:
    """Align and keep CSV records within the database position timestamp range.

    :param csv_positions: Validated source positions.
    :param database_positions: Candidate POSITION records ordered by timestamp.
    :return: CSV positions whose timestamps lie within the inclusive database range.
    """
    # SQLite does not retain DateTime.Kind, and tracker hosts may store either local or UTC clocks.
    first_timestamp = database_positions[0].timestamp
    last_timestamp = database_positions[-1].timestamp
    local_positions = [
        replace(
            position,
            timestamp=position.timestamp.replace(tzinfo=timezone.utc).astimezone().replace(
                tzinfo=None
            ),
        )
        for position in csv_positions
    ]

    # Select the interpretation with the greatest inclusive overlap; prefer UTC on a tie.
    utc_overlap = sum(
        first_timestamp <= position.timestamp <= last_timestamp for position in csv_positions
    )
    local_overlap = sum(
        first_timestamp <= position.timestamp <= last_timestamp for position in local_positions
    )
    aligned_positions = local_positions if local_overlap > utc_overlap else csv_positions
    return [
        position
        for position in aligned_positions
        if first_timestamp <= position.timestamp <= last_timestamp
    ]


def great_circle_distance_nautical_miles(
    latitude1: float, longitude1: float, latitude2: float, longitude2: float
) -> float:
    """Calculate great-circle distance between two coordinates in nautical miles.

    :param latitude1: Origin latitude in degrees.
    :param longitude1: Origin longitude in degrees.
    :param latitude2: Destination latitude in degrees.
    :param longitude2: Destination longitude in degrees.
    :return: Great-circle distance in nautical miles.
    """
    # Use the haversine formula with the repository's shared mean Earth radius.
    phi1, phi2 = math.radians(latitude1), math.radians(latitude2)
    delta_phi = math.radians(latitude2 - latitude1)
    delta_lambda = math.radians(longitude2 - longitude1)
    haversine = (
        math.sin(delta_phi / 2.0) ** 2
        + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda / 2.0) ** 2
    )
    central_angle = 2.0 * math.atan2(math.sqrt(haversine), math.sqrt(1.0 - haversine))
    return central_angle * EARTH_RADIUS_METRES / METRES_PER_NAUTICAL_MILE


def run(options: argparse.Namespace) -> tuple[str, int, int, int]:
    """Execute retrospective population in one transaction.

    :param options: Parsed command-line options.
    :return: Address, mapping count, changed row count and changed field count.
    """
    # Validate external inputs before taking a write lock on the database.
    database_path = resolve_database_path(options.database)
    csv_positions = read_csv_positions(options.csv)
    connection = sqlite3.connect(database_path)
    try:
        validate_schema(connection)
        connection.execute("BEGIN IMMEDIATE")
        address = identify_address(
            connection, options.session, csv_positions[0].callsign, options.address
        )
        database_positions = load_database_positions(connection, options.session, address)
        eligible_csv_positions = restrict_csv_positions_to_database_range(
            csv_positions, database_positions
        )
        mappings = map_positions(eligible_csv_positions, database_positions)

        # Snapshot original nullable values before applying updates for accurate reporting.
        originals = {
            position.identifier: (
                position.altitude,
                position.latitude,
                position.longitude,
                position.distance,
            )
            for position in database_positions
        }
        receiver = connection.execute(
            "SELECT ReceiverLatitude, ReceiverLongitude FROM SESSION WHERE Id = ?",
            (options.session,),
        ).fetchone()
        if receiver is None or receiver[0] is None or receiver[1] is None:
            raise PopulationError(f"Session {options.session} has no complete receiver position.")

        changed_rows = 0
        changed_fields = 0
        for position_id, source in mappings.items():
            # Populate only NULL columns and calculate distance in nautical miles.
            distance = great_circle_distance_nautical_miles(
                float(receiver[0]), float(receiver[1]), source.latitude, source.longitude
            )
            old_values = originals[position_id]
            new_values = (source.altitude, source.latitude, source.longitude, distance)
            changed = sum(
                old is None and new is not None
                for old, new in zip(old_values, new_values, strict=True)
            )
            if not changed:
                continue
            connection.execute(
                """UPDATE POSITION
                   SET Altitude = COALESCE(Altitude, ?),
                       Latitude = COALESCE(Latitude, ?),
                       Longitude = COALESCE(Longitude, ?),
                       Distance = COALESCE(Distance, ?)
                   WHERE Id = ?""",
                (*new_values, position_id),
            )
            changed_rows += 1
            changed_fields += changed

        # A dry run exercises the full mapping and update path, then discards it atomically.
        if options.dry_run:
            connection.rollback()
        else:
            connection.commit()
        return address, len(mappings), changed_rows, changed_fields
    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the command-line program and render a concise result.

    :param arguments: Optional argument sequence for testing.
    :return: Process exit status.
    """
    # Convert expected operational failures into user-facing messages without tracebacks.
    try:
        options = parse_arguments(arguments)
        address, mappings, rows, fields = run(options)
        mode = "Would update" if options.dry_run else "Updated"
        print(
            f"Aircraft {address}: mapped {mappings} POSITION record(s). "
            f"{mode} {rows} row(s) and {fields} NULL field(s)."
        )
        return 0
    except (PopulationError, sqlite3.Error) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())

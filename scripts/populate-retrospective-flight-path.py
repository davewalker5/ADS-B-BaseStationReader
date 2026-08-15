"""Populate missing SQLite flight-path values from a KML track."""

from __future__ import annotations

import argparse
import math
import os
import re
import sqlite3
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, replace
from datetime import datetime, timezone
from pathlib import Path
from typing import Sequence


EARTH_RADIUS_METRES = 6_371_000.0
METRES_PER_NAUTICAL_MILE = 1_852.0
METRES_PER_FOOT = 0.3048
ALTITUDE_PATTERN = re.compile(r"Altitude:</b></span>\s*<span>([\d,]+(?:\.\d+)?)\s*ft", re.I)


class PopulationError(Exception):
    """Describe an input or database problem that prevents safe population."""


@dataclass(frozen=True)
class KmlPosition:
    """Represent one validated position read from the source KML file."""

    timestamp: datetime
    callsign: str
    latitude: float
    longitude: float
    altitude: float
    placemark_number: int


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
    # Keep the existing named options while changing the source format to KML.
    parser = argparse.ArgumentParser()
    parser.add_argument("-s", "--session", type=int, required=True, help="SESSION.Id to update")
    parser.add_argument(
        "-k", "--kml", type=Path, required=True, help="KML file containing the flight path"
    )
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


def parse_kml_timestamp(value: str, placemark_number: int) -> datetime:
    """Parse a KML timestamp as a naive UTC clock value.

    :param value: KML TimeStamp/when value.
    :param placemark_number: One-based Placemark number used in errors.
    :return: Naive UTC datetime ready for database time-basis alignment.
    :raises PopulationError: If the timestamp is missing, invalid or lacks a time zone.
    """
    # KML timestamps must carry an offset so matching never assumes a source time zone.
    try:
        parsed = datetime.fromisoformat(value.strip().replace("Z", "+00:00"))
    except (AttributeError, ValueError) as error:
        raise PopulationError(
            f"KML Placemark {placemark_number} has an invalid timestamp."
        ) from error
    if parsed.tzinfo is None:
        raise PopulationError(
            f"KML Placemark {placemark_number} timestamp has no UTC offset."
        )
    return parsed.astimezone(timezone.utc).replace(tzinfo=None)


def parse_kml_coordinates(value: str, placemark_number: int) -> tuple[float, float, float | None]:
    """Parse and validate a KML Point coordinate.

    :param value: Comma-separated longitude, latitude and optional altitude text.
    :param placemark_number: One-based Placemark number used in errors.
    :return: Latitude, longitude and optional altitude in metres.
    :raises PopulationError: If the coordinate is malformed or outside Earth bounds.
    """
    # KML stores coordinate components in longitude, latitude, altitude order.
    try:
        parts = [part.strip() for part in value.strip().split(",")]
        if len(parts) not in (2, 3):
            raise ValueError
        longitude = float(parts[0])
        latitude = float(parts[1])
        altitude_metres = float(parts[2]) if len(parts) == 3 else None
    except (AttributeError, ValueError) as error:
        raise PopulationError(
            f"KML Placemark {placemark_number} has invalid Point coordinates."
        ) from error
    if not (-90.0 <= latitude <= 90.0 and -180.0 <= longitude <= 180.0):
        raise PopulationError(
            f"KML Placemark {placemark_number} has coordinates outside valid bounds."
        )
    return latitude, longitude, altitude_metres


def parse_kml_callsign(document_name: str) -> str:
    """Extract the callsign from a KML document name.

    :param document_name: Text from the KML Document/name element.
    :return: Normalized callsign.
    :raises PopulationError: If the document name contains no callsign.
    """
    # FlightRadar24 names documents as flight/callsign, with '-' for an absent flight number.
    callsign = document_name.rsplit("/", 1)[-1].strip().upper()
    if not callsign:
        raise PopulationError("KML Document/name does not contain a callsign.")
    return callsign


def parse_kml_altitude(
    description: str, altitude_metres: float | None, placemark_number: int
) -> float:
    """Read altitude in feet from a KML Placemark.

    :param description: Placemark description markup.
    :param altitude_metres: Optional altitude from Point coordinates.
    :param placemark_number: One-based Placemark number used in errors.
    :return: Altitude in feet.
    :raises PopulationError: If no altitude is available.
    """
    # Prefer the displayed feet value and fall back to KML's standard metres coordinate.
    match = ALTITUDE_PATTERN.search(description or "")
    if match:
        return float(match.group(1).replace(",", ""))
    if altitude_metres is not None:
        return altitude_metres / METRES_PER_FOOT
    raise PopulationError(f"KML Placemark {placemark_number} has no altitude.")


def read_kml_positions(kml_path: Path) -> list[KmlPosition]:
    """Read and validate retrospective positions from a KML file.

    :param kml_path: Source KML path.
    :return: Validated positions in file order.
    :raises PopulationError: If the file or any required value is invalid.
    """
    # Validate the path before parsing it to provide a concise diagnostic.
    if not kml_path.is_file():
        raise PopulationError(f"KML file does not exist: {kml_path}")
    try:
        tree = ET.parse(kml_path)
    except ET.ParseError as error:
        raise PopulationError(f"KML is not valid XML: {error}") from error
    except OSError as error:
        raise PopulationError(f"Unable to read KML file: {error}") from error

    # Read the aircraft callsign once from the document-level metadata.
    document_name = tree.find(".//{*}Document/{*}name")
    if document_name is None or not document_name.text:
        raise PopulationError("KML has no Document/name callsign.")
    callsign = parse_kml_callsign(document_name.text)

    positions: list[KmlPosition] = []
    for placemark_number, placemark in enumerate(tree.findall(".//{*}Placemark"), start=1):
        # Only timestamped Point placemarks represent individual flight observations.
        when = placemark.find("./{*}TimeStamp/{*}when")
        coordinates = placemark.find("./{*}Point/{*}coordinates")
        if when is None or coordinates is None:
            continue
        latitude, longitude, altitude_metres = parse_kml_coordinates(
            coordinates.text or "", placemark_number
        )
        description = placemark.find("./{*}description")
        positions.append(
            KmlPosition(
                parse_kml_timestamp(when.text or "", placemark_number),
                callsign,
                latitude,
                longitude,
                parse_kml_altitude(
                    description.text if description is not None and description.text else "",
                    altitude_metres,
                    placemark_number,
                ),
                placemark_number,
            )
        )

    # Aircraft inference uses the document callsign and requires at least one usable point.
    if not positions:
        raise PopulationError("KML contains no timestamped Point position records.")
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
    :param callsign: Callsign from the KML document name.
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
    kml_positions: Sequence[KmlPosition], database_positions: Sequence[DatabasePosition]
) -> dict[int, KmlPosition]:
    """Map KML points to nearest database positions, resolving collisions by proximity.

    :param kml_positions: Validated source positions.
    :param database_positions: Candidate POSITION records.
    :return: Mapping from POSITION identifier to its closest KML point.
    """
    # Retain only the closest KML point when several source points select one POSITION record.
    mappings: dict[int, KmlPosition] = {}
    differences: dict[int, float] = {}
    for kml_position in kml_positions:
        closest = min(
            database_positions,
            key=lambda position: (
                abs((position.timestamp - kml_position.timestamp).total_seconds()),
                position.identifier,
            ),
        )
        difference = abs((closest.timestamp - kml_position.timestamp).total_seconds())
        if closest.identifier not in differences or difference < differences[closest.identifier]:
            mappings[closest.identifier] = kml_position
            differences[closest.identifier] = difference
    return mappings


def restrict_kml_positions_to_database_range(
    kml_positions: Sequence[KmlPosition],
    database_positions: Sequence[DatabasePosition],
) -> list[KmlPosition]:
    """Align and keep KML points within the database position timestamp range.

    :param kml_positions: Validated source positions.
    :param database_positions: Candidate POSITION records ordered by timestamp.
    :return: KML positions whose timestamps lie within the inclusive database range.
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
        for position in kml_positions
    ]

    # Select the interpretation with the greatest inclusive overlap; prefer UTC on a tie.
    utc_overlap = sum(
        first_timestamp <= position.timestamp <= last_timestamp for position in kml_positions
    )
    local_overlap = sum(
        first_timestamp <= position.timestamp <= last_timestamp for position in local_positions
    )
    aligned_positions = local_positions if local_overlap > utc_overlap else kml_positions
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
    kml_positions = read_kml_positions(options.kml)
    connection = sqlite3.connect(database_path)
    try:
        validate_schema(connection)
        connection.execute("BEGIN IMMEDIATE")
        address = identify_address(
            connection, options.session, kml_positions[0].callsign, options.address
        )
        database_positions = load_database_positions(connection, options.session, address)
        eligible_kml_positions = restrict_kml_positions_to_database_range(
            kml_positions, database_positions
        )
        mappings = map_positions(eligible_kml_positions, database_positions)

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

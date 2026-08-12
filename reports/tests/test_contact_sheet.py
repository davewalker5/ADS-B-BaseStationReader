"""Tests for the aircraft contact-sheet generator."""

from __future__ import annotations

import csv
import sqlite3
import tempfile
import unittest
from contextlib import closing
from pathlib import Path

import sys

# Import the report module directly without requiring reports to be an installed package.
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from contact_sheet import (  # noqa: E402
    AircraftRequest,
    generate_contact_sheets,
    load_flight_path,
    read_requests,
)


class ContactSheetTests(unittest.TestCase):
    """Verify CSV parsing, database loading, captions, and pagination."""

    def setUp(self) -> None:
        """Create a minimal tracker database for each test.

        :return: None.
        """
        # A temporary directory keeps generated reports and databases isolated.
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.database_path = self.root / "tracker.db"
        with closing(sqlite3.connect(self.database_path)) as connection:
            connection.executescript(
                """
                CREATE TABLE TRACKED_AIRCRAFT (
                    Id INTEGER PRIMARY KEY,
                    SessionId INTEGER NOT NULL,
                    Address TEXT NOT NULL,
                    Callsign TEXT NULL
                );
                CREATE TABLE POSITION (
                    Id INTEGER PRIMARY KEY,
                    AircraftId INTEGER NOT NULL,
                    Latitude REAL,
                    Longitude REAL,
                    Altitude REAL,
                    Timestamp TEXT NOT NULL
                );
                INSERT INTO TRACKED_AIRCRAFT VALUES (1, 7, 'ABC123', ' ba123 ');
                INSERT INTO TRACKED_AIRCRAFT VALUES (2, 7, 'DEF456', NULL);
                INSERT INTO POSITION VALUES (1, 1, 51.0, -0.2, 1000, '2026-01-01T10:00:00');
                INSERT INTO POSITION VALUES (2, 1, 51.1, -0.1, 2000, '2026-01-01T10:01:00');
                """
            )

    def tearDown(self) -> None:
        """Remove the temporary test directory.

        :return: None.
        """
        # Explicit cleanup avoids retaining generated PNG files after the suite finishes.
        self.temporary_directory.cleanup()

    def test_read_requests_normalises_addresses(self) -> None:
        """Read valid input and capitalise ICAO addresses.

        :return: None.
        """
        # Use the CSV module to mirror files produced by spreadsheet applications.
        csv_path = self.root / "input.csv"
        with csv_path.open("w", encoding="utf-8", newline="") as csv_file:
            writer = csv.writer(csv_file)
            writer.writerow(["Address", "Session ID"])
            writer.writerow(["abc123", "7"])

        self.assertEqual(read_requests(csv_path), [AircraftRequest("ABC123", 7)])

    def test_load_flight_path_normalises_callsign(self) -> None:
        """Load positions in order and capitalise the observed callsign.

        :return: None.
        """
        # Query the fixture through the same read-only shape used by the generator.
        with closing(sqlite3.connect(self.database_path)) as connection:
            path = load_flight_path(connection, AircraftRequest("ABC123", 7))

        self.assertEqual(path.callsign, "BA123")
        self.assertEqual(path.latitudes.tolist(), [51.0, 51.1])
        self.assertEqual(path.altitudes.tolist(), [304.8, 609.6])

    def test_load_flight_path_uses_none_without_position_data(self) -> None:
        """Use NONE when no positioned record supplies a callsign.

        :return: None.
        """
        # An empty position result is still rendered as a labelled contact-sheet cell.
        with closing(sqlite3.connect(self.database_path)) as connection:
            path = load_flight_path(connection, AircraftRequest("DEF456", 7))

        self.assertEqual(path.callsign, "NONE")
        self.assertEqual(path.latitudes.size, 0)

    def test_generate_contact_sheets_paginates(self) -> None:
        """Create additional PNG pages when the grid capacity is exceeded.

        :return: None.
        """
        # A one-cell grid makes page naming and pagination easy to assert.
        output_directory = self.root / "output"
        paths = generate_contact_sheets(
            [AircraftRequest("ABC123", 7), AircraftRequest("DEF456", 7)],
            self.database_path,
            output_directory,
            rows=1,
            columns=1,
        )

        self.assertEqual([path.name for path in paths], ["contact-sheet-7-1.png", "contact-sheet-7-2.png"])
        self.assertTrue(all(path.stat().st_size > 0 for path in paths))


if __name__ == "__main__":
    # unittest provides a dependency-free entry point for the reporting environment.
    unittest.main()

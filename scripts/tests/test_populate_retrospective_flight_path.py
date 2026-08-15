"""Tests for retrospective flight-path population."""

from __future__ import annotations

import importlib.util
import sqlite3
import sys
import tempfile
import unittest
from datetime import datetime
from pathlib import Path


SCRIPT_PATH = Path(__file__).parents[1] / "populate-retrospective-flight-path.py"
SPEC = importlib.util.spec_from_file_location("retrospective_flight_path", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class RetrospectiveFlightPathTest(unittest.TestCase):
    """Exercise mapping, inference, preservation and transaction behaviour."""

    def setUp(self) -> None:
        """Create an isolated tracker-shaped database and KML file.

        :return: None.
        """
        # Use on-disk fixtures because the command opens its own SQLite connection.
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.database = self.root / "tracker.db"
        self.kml_path = self.root / "track.kml"
        connection = sqlite3.connect(self.database)
        connection.executescript(
            """
            CREATE TABLE SESSION (
                Id INTEGER PRIMARY KEY,
                ReceiverLatitude REAL,
                ReceiverLongitude REAL
            );
            CREATE TABLE TRACKED_AIRCRAFT (
                Id INTEGER PRIMARY KEY,
                SessionId INTEGER,
                Address TEXT,
                Callsign TEXT
            );
            CREATE TABLE POSITION (
                Id INTEGER PRIMARY KEY,
                AircraftId INTEGER,
                Timestamp TEXT,
                Altitude REAL,
                Latitude REAL,
                Longitude REAL,
                Distance REAL
            );
            INSERT INTO SESSION VALUES (7, 51.5, -1.0);
            INSERT INTO TRACKED_AIRCRAFT VALUES (10, 7, 'abc123', 'TEST1');
            INSERT INTO TRACKED_AIRCRAFT VALUES (11, 7, 'ABC123', 'TEST1');
            INSERT INTO POSITION VALUES (100, 10, '2026-01-15 10:00:00', NULL, NULL, NULL, NULL);
            INSERT INTO POSITION VALUES (101, 11, '2026-01-15 10:00:10', 9999, NULL, NULL, 42);
            """
        )
        connection.commit()
        connection.close()
        self.kml_path.write_text(
            """<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <name>-/TEST1</name>
    <Folder>
      <name>Route</name>
      <Placemark>
        <description><![CDATA[<span><b>Altitude:</b></span> <span>1,000 ft</span>]]></description>
        <TimeStamp><when>2026-01-15T10:00:00+00:00</when></TimeStamp>
        <Point><coordinates>-1.1,51.6,304.8</coordinates></Point>
      </Placemark>
      <Placemark>
        <TimeStamp><when>2026-01-15T10:00:10Z</when></TimeStamp>
        <Point><coordinates>-1.2,51.7,609.6</coordinates></Point>
      </Placemark>
    </Folder>
  </Document>
</kml>
""",
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        """Remove temporary test files.

        :return: None.
        """
        # Release all fixture resources after each test.
        self.temporary_directory.cleanup()

    def options(self, dry_run: bool = False):
        """Build command options for the fixture.

        :param dry_run: Whether updates should be rolled back.
        :return: Parsed command options.
        """
        # Parse the public CLI rather than constructing its internal namespace.
        arguments = [
            "--session",
            "7",
            "--kml",
            str(self.kml_path),
            "--database",
            str(self.database),
        ]
        if dry_run:
            arguments.append("--dry-run")
        return MODULE.parse_arguments(arguments)

    def test_populates_nulls_across_aircraft_segments(self) -> None:
        """Populate both lifecycle segments while retaining existing fields.

        :return: None.
        """
        # The inferred normalized address must cover both TRACKED_AIRCRAFT IDs.
        address, mappings, rows, fields = MODULE.run(self.options())
        self.assertEqual((address, mappings, rows, fields), ("ABC123", 2, 2, 6))
        connection = sqlite3.connect(self.database)
        first = connection.execute(
            "SELECT Altitude, Latitude, Longitude, Distance FROM POSITION WHERE Id = 100"
        ).fetchone()
        second = connection.execute(
            "SELECT Altitude, Latitude, Longitude, Distance FROM POSITION WHERE Id = 101"
        ).fetchone()
        connection.close()
        self.assertEqual(first[:3], (1000, 51.6, -1.1))
        self.assertGreater(first[3], 0)
        self.assertEqual(second[:3], (9999, 51.7, -1.2))
        self.assertEqual(second[3], 42)

    def test_dry_run_rolls_back_changes(self) -> None:
        """Report dry-run updates without persisting them.

        :return: None.
        """
        # A full dry run should calculate the same changes but leave NULL values intact.
        _, _, rows, fields = MODULE.run(self.options(dry_run=True))
        self.assertEqual((rows, fields), (2, 6))
        connection = sqlite3.connect(self.database)
        value = connection.execute("SELECT Altitude FROM POSITION WHERE Id = 100").fetchone()[0]
        connection.close()
        self.assertIsNone(value)

    def test_collision_uses_closest_kml_point(self) -> None:
        """Resolve two KML mappings to one position using the closest point.

        :return: None.
        """
        # The source at ten seconds is closer to the database point at eleven seconds.
        sources = MODULE.read_kml_positions(self.kml_path)
        target = MODULE.DatabasePosition(1, datetime(2026, 1, 15, 10, 0, 11), None, None, None, None)
        mapping = MODULE.map_positions(sources, [target])
        self.assertEqual(mapping[1].altitude, 2000)

    def test_restricts_kml_points_to_database_timestamp_range(self) -> None:
        """Exclude KML points before the first and after the last database position.

        :return: None.
        """
        # Both exact boundary timestamps remain eligible; surrounding records do not.
        sources = MODULE.read_kml_positions(self.kml_path)
        before = MODULE.KmlPosition(
            datetime(2026, 1, 15, 9, 59, 59), "TEST1", 51.0, -1.0, 500, 4
        )
        after = MODULE.KmlPosition(
            datetime(2026, 1, 15, 10, 0, 11), "TEST1", 52.0, -2.0, 3000, 5
        )
        targets = [
            MODULE.DatabasePosition(
                1, datetime(2026, 1, 15, 10, 0, 0), None, None, None, None
            ),
            MODULE.DatabasePosition(
                2, datetime(2026, 1, 15, 10, 0, 10), None, None, None, None
            ),
        ]
        eligible = MODULE.restrict_kml_positions_to_database_range(
            [before, *sources, after], targets
        )
        self.assertEqual(eligible, sources)

    def test_selects_utc_clock_when_local_conversion_has_no_overlap(self) -> None:
        """Use raw UTC clock values when they match the database's stored timestamps.

        :return: None.
        """
        # This reproduces a BST run against positions whose naive values follow a UTC clock.
        source = MODULE.KmlPosition(
            datetime(2026, 8, 15, 11, 0, 5), "TEST1", 51.0, -1.0, 1000, 2
        )
        targets = [
            MODULE.DatabasePosition(
                1, datetime(2026, 8, 15, 11, 0, 0), None, None, None, None
            ),
            MODULE.DatabasePosition(
                2, datetime(2026, 8, 15, 11, 0, 10), None, None, None, None
            ),
        ]
        eligible = MODULE.restrict_kml_positions_to_database_range([source], targets)
        self.assertEqual(eligible, [source])

    def test_ambiguous_callsign_requires_address(self) -> None:
        """Reject callsigns associated with distinct addresses.

        :return: None.
        """
        # Add another address under the same session and callsign to force ambiguity.
        connection = sqlite3.connect(self.database)
        connection.execute("INSERT INTO TRACKED_AIRCRAFT VALUES (12, 7, 'DEF456', 'TEST1')")
        connection.commit()
        with self.assertRaisesRegex(MODULE.PopulationError, "multiple addresses"):
            MODULE.identify_address(connection, 7, "TEST1", None)
        connection.close()


if __name__ == "__main__":
    unittest.main()

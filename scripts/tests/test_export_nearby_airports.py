"""Tests for the nearby-airport CSV export."""

from __future__ import annotations

import csv
import importlib.util
import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "export-nearby-airports.py"
SPEC = importlib.util.spec_from_file_location("export_nearby_airports", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class ExportNearbyAirportsTest(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        root = Path(self.temporary_directory.name)
        self.database = root / "airports.db"
        self.output = root / "result.csv"
        with sqlite3.connect(self.database) as connection:
            connection.executescript(
                """
                CREATE TABLE AIRPORT (
                    IATA TEXT, ICAO TEXT, Name TEXT,
                    Latitude REAL, Longitude REAL
                );
                INSERT INTO AIRPORT VALUES ('AAA', 'EAAA', 'At origin', 0, 0);
                INSERT INTO AIRPORT VALUES ('BBB', 'EBBB', 'One NM east', 0, 0.016655436);
                INSERT INTO AIRPORT VALUES ('CCC', 'ECCC', 'Too far', 0, 1);
                INSERT INTO AIRPORT VALUES ('BAD', 'EBAD', 'Invalid', 100, 0);
                INSERT INTO AIRPORT VALUES ('NUL', 'ENUL', 'Missing', NULL, NULL);
                """
            )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_exports_only_airports_inside_radius_nearest_first(self) -> None:
        result = MODULE.main(
            ["--latitude", "0", "--longitude", "0", "--distance", "1.1",
             "--database", str(self.database), "--output", str(self.output)]
        )
        self.assertEqual(result, 0)
        with self.output.open(newline="", encoding="utf-8") as input_file:
            rows = list(csv.reader(input_file))
        self.assertEqual(
            rows[0], ["IATA", "ICAO", "Name", "Latitude", "Longitude", "Distance (nm)"]
        )
        self.assertEqual([row[0] for row in rows[1:]], ["AAA", "BBB"])
        self.assertEqual(rows[1][-1], "0.000")
        self.assertAlmostEqual(float(rows[2][-1]), 1.0, places=2)

    def test_distance_calculation_handles_antipodal_points(self) -> None:
        distance = MODULE.great_circle_distance_nautical_miles(0, 0, 0, 180)
        self.assertAlmostEqual(distance, 10807.3, places=0)

    def test_rejects_out_of_range_command_line_coordinates(self) -> None:
        with self.assertRaises(SystemExit):
            MODULE.parse_arguments(
                ["--latitude", "91", "--longitude", "0", "--distance", "1",
                 "--output", str(self.output)]
            )


if __name__ == "__main__":
    unittest.main()

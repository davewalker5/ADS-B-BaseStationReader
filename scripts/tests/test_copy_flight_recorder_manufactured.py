"""Tests for copying manufacture years between aircraft databases."""

from __future__ import annotations

import importlib.util
import sqlite3
import sys
import tempfile
import unittest
from io import StringIO
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "copy-flight-recorder-manufactured.py"
SPEC = importlib.util.spec_from_file_location("copy_flight_recorder_manufactured", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class CopyFlightRecorderManufacturedTest(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        root = Path(self.temporary_directory.name)
        self.source = root / "recorder.db"
        self.target = root / "tracker.db"
        with sqlite3.connect(self.source) as connection:
            connection.executescript(
                """
                CREATE TABLE flights (registration TEXT, manufactured INTEGER);
                INSERT INTO flights VALUES ('G-NEW', 2012);
                INSERT INTO flights VALUES ('G-KEEP', 2001);
                INSERT INTO flights VALUES ('G-BABY', 2026);
                INSERT INTO flights VALUES ('G-BLANK', NULL);
                INSERT INTO flights VALUES ('G-MISSING', 1999);
                """
            )
        with sqlite3.connect(self.target) as connection:
            connection.executescript(
                """
                CREATE TABLE AIRCRAFT (
                    Id INTEGER PRIMARY KEY, Registration TEXT, Manufactured INTEGER
                );
                CREATE INDEX IX_AIRCRAFT_Registration ON AIRCRAFT (Registration);
                INSERT INTO AIRCRAFT VALUES (1, 'G-NEW', NULL);
                INSERT INTO AIRCRAFT VALUES (2, 'G-KEEP', 1998);
                INSERT INTO AIRCRAFT VALUES (3, 'G-BABY', NULL);
                INSERT INTO AIRCRAFT VALUES (4, 'G-BLANK', NULL);
                """
            )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_copies_only_eligible_manufacture_years(self) -> None:
        result = MODULE.copy_manufactured(self.source, self.target, current_year=2026)
        self.assertEqual(result, MODULE.CopyResult(5, 1, 1, 1, 1, 1))
        with sqlite3.connect(self.target) as connection:
            values = connection.execute(
                "SELECT Registration, Manufactured FROM AIRCRAFT ORDER BY Id"
            ).fetchall()
        self.assertEqual(
            values,
            [("G-NEW", 2012), ("G-KEEP", 1998), ("G-BABY", None), ("G-BLANK", None)],
        )

    def test_progress_bar_reaches_one_hundred_percent(self) -> None:
        output = StringIO()
        bar = MODULE.ProgressBar(output=output, width=10)
        MODULE.copy_manufactured(self.source, self.target, bar.update, current_year=2026)
        self.assertIn("[##########] 100.0% (5/5) updated 1", output.getvalue())

    def test_rejects_an_ambiguous_source_schema(self) -> None:
        with sqlite3.connect(self.source) as connection:
            connection.execute("CREATE TABLE other (registration TEXT, manufactured INTEGER)")
        with self.assertRaisesRegex(MODULE.CopyError, "More than one flight recorder"):
            MODULE.copy_manufactured(self.source, self.target)


if __name__ == "__main__":
    unittest.main()

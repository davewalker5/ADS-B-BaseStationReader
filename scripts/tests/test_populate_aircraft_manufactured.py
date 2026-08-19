"""Tests for FAA manufacture-year population."""

from __future__ import annotations

import importlib.util
import sqlite3
import sys
import tempfile
import unittest
from io import StringIO
from pathlib import Path


SCRIPT_PATH = Path(__file__).parents[1] / "populate-aircraft-manufactured.py"
SPEC = importlib.util.spec_from_file_location("populate_aircraft_manufactured", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class PopulateAircraftManufacturedTest(unittest.TestCase):
    """Exercise matching, preservation, updating, and rollback."""

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.database = self.root / "tracker.db"
        self.master = self.root / "MASTER.txt"
        with sqlite3.connect(self.database) as connection:
            connection.executescript(
                """
                CREATE TABLE AIRCRAFT (
                    Id INTEGER PRIMARY KEY, Registration TEXT, Manufactured INTEGER
                );
                INSERT INTO AIRCRAFT VALUES (1, 'N100', NULL);
                INSERT INTO AIRCRAFT VALUES (2, 'N10001', 1928);
                INSERT INTO AIRCRAFT VALUES (3, 'n10006 ', 1950);
                """
            )
        self.master.write_text(
            "N-NUMBER,YEAR MFR,NAME\n"
            "100  ,1940,One\n"
            "10000,    ,Two\n"
            "10001,1928,Three\n"
            "10004,2013,Four\n"
            "10006,1955,Five\n",
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_populates_null_and_different_years(self) -> None:
        result = MODULE.populate(self.master, self.database)
        self.assertEqual(result, MODULE.PopulationResult(5, 1, 1, 1, 2))
        with sqlite3.connect(self.database) as connection:
            values = connection.execute(
                "SELECT Id, Manufactured FROM AIRCRAFT ORDER BY Id"
            ).fetchall()
        self.assertEqual(values, [(1, 1940), (2, 1928), (3, 1955)])

    def test_invalid_year_rolls_back_all_updates(self) -> None:
        self.master.write_text(
            "N-NUMBER,YEAR MFR\n100,1940\n10006,unknown\n", encoding="utf-8"
        )
        with self.assertRaises(MODULE.PopulationError):
            MODULE.populate(self.master, self.database)
        with sqlite3.connect(self.database) as connection:
            value = connection.execute(
                "SELECT Manufactured FROM AIRCRAFT WHERE Id = 1"
            ).fetchone()[0]
        self.assertIsNone(value)

    def test_requires_expected_headers(self) -> None:
        self.master.write_text("N-NUMBER,NAME\n100,One\n", encoding="utf-8")
        with self.assertRaisesRegex(MODULE.PopulationError, "YEAR MFR"):
            MODULE.populate(self.master, self.database)

    def test_progress_bar_reaches_one_hundred_percent(self) -> None:
        output = StringIO()
        progress_bar = MODULE.ProgressBar(output=output, width=10)
        result = MODULE.populate(self.master, self.database, progress_bar.update)
        rendered = output.getvalue()
        self.assertIn("[##########] 100.0% (5/5) updated 2", rendered)
        self.assertTrue(rendered.endswith("\n"))


if __name__ == "__main__":
    unittest.main()

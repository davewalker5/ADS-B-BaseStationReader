"""Tests for creating an observation-log CSV."""

from __future__ import annotations

import csv
from datetime import date
import importlib.util
from pathlib import Path
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).parents[1] / "create-observation-log.py"
SPEC = importlib.util.spec_from_file_location("create_observation_log", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class CreateObservationLogTest(unittest.TestCase):
    def test_includes_selected_days_and_numbers_weeks_from_sunday(self) -> None:
        rows = list(MODULE.observation_rows(date(2026, 8, 30), 1))

        self.assertEqual(
            rows[:6],
            [
                {"Date": "30/08/2026", "Day": "Sun", "Week number": 1},
                {"Date": "31/08/2026", "Day": "Mon", "Week number": 1},
                {"Date": "02/09/2026", "Day": "Wed", "Week number": 1},
                {"Date": "04/09/2026", "Day": "Fri", "Week number": 1},
                {"Date": "05/09/2026", "Day": "Sat", "Week number": 1},
                {"Date": "06/09/2026", "Day": "Sun", "Week number": 2},
            ],
        )
        self.assertNotIn("Tue", {row["Day"] for row in rows})
        self.assertNotIn("Thu", {row["Day"] for row in rows})

    def test_non_sunday_start_date_is_in_week_one(self) -> None:
        rows = list(MODULE.observation_rows(date(2026, 9, 2), 1))

        self.assertEqual(rows[0], {"Date": "02/09/2026", "Day": "Wed", "Week number": 1})
        sunday = next(row for row in rows if row["Day"] == "Sun")
        self.assertEqual(sunday["Week number"], 2)

    def test_writes_expected_csv_headers(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            output = Path(temporary_directory) / "log.csv"
            count = MODULE.create_observation_log(output, date(2026, 8, 30), 1)

            with output.open(newline="", encoding="utf-8") as source:
                reader = csv.DictReader(source)
                rows = list(reader)

        self.assertEqual(reader.fieldnames, ["Date", "Day", "Week number"])
        self.assertEqual(count, len(rows))
        self.assertEqual(rows[0]["Date"], "30/08/2026")

    def test_add_months_clamps_month_end(self) -> None:
        self.assertEqual(MODULE.add_months(date(2025, 1, 31), 1), date(2025, 2, 28))


if __name__ == "__main__":
    unittest.main()

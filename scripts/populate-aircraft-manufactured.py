"""
Populate AIRCRAFT manufacture years from an FAA MASTER.txt file
"""

from __future__ import annotations

import argparse
import csv
import sqlite3
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Sequence, TextIO


class PopulationError(Exception):
    """Describe an input or database problem that prevents the import."""


@dataclass(frozen=True)
class PopulationResult:
    """Summarize how the FAA records were applied to the database."""

    processed: int = 0
    blank_year: int = 0
    not_found: int = 0
    unchanged: int = 0
    updated: int = 0


ProgressCallback = Callable[[PopulationResult, int], None]


class ProgressBar:
    """Render import progress on a single terminal line."""

    def __init__(self, output: TextIO = sys.stderr, width: int = 30) -> None:
        self.output = output
        self.width = width
        self.last_refresh = 0.0

    def update(self, result: PopulationResult, total: int) -> None:
        """Refresh periodically, and always render the first and last records."""
        now = time.monotonic()
        if result.processed not in (1, total) and now - self.last_refresh < 0.1:
            return
        self.last_refresh = now
        fraction = result.processed / total if total else 1.0
        completed = min(self.width, int(fraction * self.width))
        bar = "#" * completed + "-" * (self.width - completed)
        ending = "\n" if result.processed >= total else ""
        print(
            f"\r[{bar}] {fraction:6.1%} "
            f"({result.processed:,}/{total:,}) updated {result.updated:,}",
            end=ending,
            file=self.output,
            flush=True,
        )


def parse_arguments(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "-m", "--master", type=Path, required=True,
        help="Path to the FAA MASTER.txt file",
    )
    parser.add_argument(
        "-db", "--database", type=Path, required=True,
        help="Path to the tracker SQLite database",
    )
    return parser.parse_args(arguments)


def validate_file(path: Path, description: str) -> Path:
    """Return an existing regular file without allowing SQLite to create one."""
    resolved = path.expanduser().resolve()
    if not resolved.is_file():
        raise PopulationError(f"{description} does not exist: {resolved}")
    return resolved


def validate_schema(connection: sqlite3.Connection) -> None:
    """Verify that AIRCRAFT has the columns required by the import."""
    columns = {
        row[1].upper() for row in connection.execute('PRAGMA table_info("AIRCRAFT")')
    }
    missing = {"REGISTRATION", "MANUFACTURED"}.difference(columns)
    if missing:
        raise PopulationError(
            "AIRCRAFT is missing required column(s): " + ", ".join(sorted(missing))
        )


def parse_year(value: str, row_number: int) -> int | None:
    """Convert a non-blank FAA manufacture year to an integer."""
    stripped = value.strip()
    if not stripped:
        return None
    if len(stripped) != 4 or not stripped.isdigit():
        raise PopulationError(
            f"Invalid YEAR MFR at MASTER.txt row {row_number}: {value!r}"
        )
    return int(stripped)


def count_records(master_path: Path) -> int:
    """Count CSV data records so progress can be displayed as a percentage."""
    try:
        with master_path.open("r", encoding="utf-8-sig", newline="") as source:
            reader = csv.reader(source)
            next(reader, None)
            return sum(1 for _ in reader)
    except OSError as error:
        raise PopulationError(f"Unable to read MASTER.txt: {error}") from error


def populate(
    master_path: Path,
    database_path: Path,
    progress: ProgressCallback | None = None,
) -> PopulationResult:
    """Apply FAA manufacture years in a single database transaction."""
    master_path = validate_file(master_path, "MASTER.txt file")
    database_path = validate_file(database_path, "Tracker database")
    total_records = count_records(master_path) if progress else 0
    result = PopulationResult()

    try:
        source = master_path.open("r", encoding="utf-8-sig", newline="")
    except OSError as error:
        raise PopulationError(f"Unable to read MASTER.txt: {error}") from error

    with source, sqlite3.connect(database_path) as connection:
        validate_schema(connection)
        reader = csv.DictReader(source)
        required_headers = {"N-NUMBER", "YEAR MFR"}
        headers = set(reader.fieldnames or [])
        missing_headers = required_headers.difference(headers)
        if missing_headers:
            raise PopulationError(
                "MASTER.txt is missing required header(s): "
                + ", ".join(sorted(missing_headers))
            )

        connection.execute("BEGIN IMMEDIATE")
        try:
            for row_number, row in enumerate(reader, start=2):
                result = PopulationResult(
                    processed=result.processed + 1,
                    blank_year=result.blank_year,
                    not_found=result.not_found,
                    unchanged=result.unchanged,
                    updated=result.updated,
                )
                year = parse_year(row["YEAR MFR"], row_number)
                if year is None:
                    result = PopulationResult(
                        result.processed, result.blank_year + 1,
                        result.not_found, result.unchanged, result.updated
                    )
                    if progress:
                        progress(result, total_records)
                    continue

                n_number = row["N-NUMBER"].strip().upper()
                if not n_number:
                    raise PopulationError(
                        f"YEAR MFR is set but N-NUMBER is blank at MASTER.txt row {row_number}"
                    )
                registration = "N" + n_number
                rows = connection.execute(
                    """SELECT Id, Manufactured FROM AIRCRAFT
                       WHERE UPPER(TRIM(Registration)) = ?""",
                    (registration,),
                ).fetchall()
                if not rows:
                    result = PopulationResult(
                        result.processed, result.blank_year,
                        result.not_found + 1, result.unchanged, result.updated
                    )
                    if progress:
                        progress(result, total_records)
                    continue

                changed_ids = [identifier for identifier, current_year in rows if current_year != year]
                if not changed_ids:
                    result = PopulationResult(
                        result.processed, result.blank_year,
                        result.not_found, result.unchanged + 1, result.updated
                    )
                    if progress:
                        progress(result, total_records)
                    continue

                connection.executemany(
                    "UPDATE AIRCRAFT SET Manufactured = ? WHERE Id = ?",
                    ((year, identifier) for identifier in changed_ids),
                )
                result = PopulationResult(
                    result.processed, result.blank_year, result.not_found,
                    result.unchanged, result.updated + len(changed_ids)
                )
                if progress:
                    progress(result, total_records)
            if progress and total_records == 0:
                progress(result, total_records)
            connection.commit()
        except Exception:
            connection.rollback()
            raise

    return result


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the importer and print its summary."""
    options = parse_arguments(arguments)
    try:
        print("Counting MASTER.txt records...", file=sys.stderr)
        progress_bar = ProgressBar()
        result = populate(options.master, options.database, progress_bar.update)
    except (PopulationError, csv.Error, sqlite3.Error, OSError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1

    print(
        "Import complete - "
        f"processed {result.processed}, updated {result.updated}, "
        f"unchanged {result.unchanged}, not found {result.not_found}, "
        f"blank year {result.blank_year}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

"""Copy missing aircraft manufacture years between SQLite databases."""

from __future__ import annotations

import os
import sqlite3
import sys
import time
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Callable, TextIO


class CopyError(Exception):
    """Describe a configuration or database problem that prevents the copy."""


@dataclass(frozen=True)
class CopyResult:
    """Summarize how flight-recorder records were handled."""

    processed: int = 0
    updated: int = 0
    already_populated: int = 0
    age_zero: int = 0
    no_manufactured: int = 0
    not_found: int = 0


ProgressCallback = Callable[[CopyResult, int], None]


class ProgressBar:
    """Render copy progress on one terminal line."""

    def __init__(self, output: TextIO = sys.stderr, width: int = 30) -> None:
        self.output = output
        self.width = width
        self.last_refresh = 0.0

    def update(self, result: CopyResult, total: int) -> None:
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


def database_path(variable: str) -> Path:
    """Read and validate a database path from an environment variable."""
    value = os.environ.get(variable)
    if not value:
        raise CopyError(f"{variable} is not set")
    path = Path(value).expanduser().resolve()
    if not path.is_file():
        raise CopyError(f"{variable} does not identify an existing file: {path}")
    return path


def quote_identifier(identifier: str) -> str:
    """Quote an SQLite identifier discovered from the database schema."""
    return '"' + identifier.replace('"', '""') + '"'


def find_table(
    connection: sqlite3.Connection,
    registration_column: str,
    manufactured_column: str,
    description: str,
) -> str:
    """Find the sole user table containing the two expected columns."""
    matches: list[str] = []
    tables = connection.execute(
        "SELECT name FROM sqlite_schema "
        "WHERE type = 'table' AND name NOT LIKE 'sqlite_%'"
    ).fetchall()
    for (table,) in tables:
        columns = {
            row[1]
            for row in connection.execute(
                f"PRAGMA table_info({quote_identifier(table)})"
            )
        }
        if {registration_column, manufactured_column}.issubset(columns):
            matches.append(table)
    if not matches:
        raise CopyError(
            f"No {description} table contains columns "
            f"{registration_column!r} and {manufactured_column!r}"
        )
    if len(matches) > 1:
        raise CopyError(
            f"More than one {description} table has the required columns: "
            + ", ".join(sorted(matches))
        )
    return matches[0]


def copy_manufactured(
    flight_recorder_path: Path,
    aircraft_tracker_path: Path,
    progress: ProgressCallback | None = None,
    current_year: int | None = None,
) -> CopyResult:
    """Copy manufacture years by iterating over the smaller recorder database."""
    if flight_recorder_path.resolve() == aircraft_tracker_path.resolve():
        raise CopyError("The flight recorder and aircraft tracker must be different files")

    year_now = current_year if current_year is not None else date.today().year
    result = CopyResult()
    with (
        sqlite3.connect(f"file:{flight_recorder_path.resolve()}?mode=ro", uri=True) as source,
        sqlite3.connect(aircraft_tracker_path.resolve()) as target,
    ):
        source_table = find_table(source, "registration", "manufactured", "flight recorder")
        target_table = find_table(target, "Registration", "Manufactured", "aircraft tracker")
        source_table_sql = quote_identifier(source_table)
        target_table_sql = quote_identifier(target_table)
        total = source.execute(f"SELECT COUNT(*) FROM {source_table_sql}").fetchone()[0]

        target.execute("BEGIN IMMEDIATE")
        try:
            rows = source.execute(
                f'SELECT "registration", "manufactured" FROM {source_table_sql}'
            )
            for registration, manufactured in rows:
                result = CopyResult(
                    processed=result.processed + 1,
                    updated=result.updated,
                    already_populated=result.already_populated,
                    age_zero=result.age_zero,
                    no_manufactured=result.no_manufactured,
                    not_found=result.not_found,
                )

                if manufactured is None:
                    result = CopyResult(**{**result.__dict__, "no_manufactured": result.no_manufactured + 1})
                else:
                    try:
                        manufactured_year = int(manufactured)
                    except (TypeError, ValueError) as error:
                        raise CopyError(
                            f"Invalid manufactured value for registration {registration!r}: "
                            f"{manufactured!r}"
                        ) from error

                    if manufactured_year == year_now:
                        result = CopyResult(**{**result.__dict__, "age_zero": result.age_zero + 1})
                    else:
                        matches = target.execute(
                            f'SELECT "Manufactured" FROM {target_table_sql} '
                            'WHERE "Registration" = ?',
                            (registration,),
                        ).fetchall()
                        if not matches:
                            result = CopyResult(**{**result.__dict__, "not_found": result.not_found + 1})
                        else:
                            populated = sum(value is not None for (value,) in matches)
                            missing = len(matches) - populated
                            if populated:
                                result = CopyResult(
                                    **{**result.__dict__, "already_populated": result.already_populated + populated}
                                )
                            if missing:
                                cursor = target.execute(
                                    f'UPDATE {target_table_sql} SET "Manufactured" = ? '
                                    'WHERE "Registration" = ? AND "Manufactured" IS NULL',
                                    (manufactured_year, registration),
                                )
                                result = CopyResult(
                                    **{**result.__dict__, "updated": result.updated + cursor.rowcount}
                                )
                if progress:
                    progress(result, total)

            if progress and total == 0:
                progress(result, total)
            target.commit()
        except Exception:
            target.rollback()
            raise
    return result


def main() -> int:
    """Copy manufacture years using database paths from the environment."""
    try:
        result = copy_manufactured(
            database_path("FLIGHT_RECORDER_DB"),
            database_path("AIRCRAFT_TRACKER_DB"),
            ProgressBar().update,
        )
    except (CopyError, sqlite3.Error, OSError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1

    print(
        "Copy complete - "
        f"processed {result.processed}, updated {result.updated}, "
        f"already populated {result.already_populated}, age zero {result.age_zero}, "
        f"no manufacture year {result.no_manufactured}, not found {result.not_found}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

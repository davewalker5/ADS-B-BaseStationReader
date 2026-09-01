"""Convert a Mictronics ICAO24 mapping file to an aircraft import CSV."""

from __future__ import annotations

import argparse
import csv
import os
from pathlib import Path
import re
import sqlite3
import sys


OUTPUT_HEADERS = (
    "Address",
    "Registration",
    "IATA",
    "ICAO",
    "Manufactured",
    "Provenance",
)
ADDRESS_PATTERN = re.compile(r"^[0-9A-F]{6}$")


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Convert a Mictronics ICAO 24-bit address mapping to the aircraft "
            "CSV import format, retaining only model ICAO codes known to the database."
        )
    )
    parser.add_argument(
        "-i",
        "--input",
        required=True,
        type=Path,
        help="path to the input Mictronics ICAO 24-bit address file",
    )
    parser.add_argument(
        "-o",
        "--output",
        required=True,
        type=Path,
        help="path to the output CSV file",
    )
    return parser.parse_args()


def database_path_from_environment() -> Path:
    value = os.environ.get("AIRCRAFT_TRACKER_DB", "").strip()
    if not value:
        raise ValueError("AIRCRAFT_TRACKER_DB is not set")

    # Accept either the path used by the repository scripts or a .NET-style
    # SQLite connection string such as "Data Source=/path/database.db;Pooling=True".
    parts = [part.strip() for part in value.split(";") if part.strip()]
    for part in parts:
        key, separator, candidate = part.partition("=")
        if separator and key.strip().casefold() in {"data source", "datasource", "filename"}:
            value = candidate.strip().strip('"\'')
            break

    path = Path(value).expanduser()
    if not path.is_file():
        raise ValueError(f"database does not exist: {path}")
    return path.resolve()


def load_model_icao_codes(database_path: Path) -> set[str]:
    database_uri = f"{database_path.as_uri()}?mode=ro"
    with sqlite3.connect(database_uri, uri=True) as connection:
        rows = connection.execute(
            'SELECT ICAO FROM MODEL WHERE ICAO IS NOT NULL AND TRIM(ICAO) <> ""'
        )
        return {str(row[0]).strip().upper() for row in rows}


def convert(input_path: Path, output_path: Path, model_codes: set[str]) -> tuple[int, int]:
    read_count = 0
    written_count = 0

    with input_path.open("r", encoding="utf-8-sig", errors="replace", newline="") as source:
        with output_path.open("w", encoding="utf-8", newline="") as destination:
            writer = csv.DictWriter(destination, fieldnames=OUTPUT_HEADERS)
            writer.writeheader()

            for fields in csv.reader(source, delimiter="\t"):
                if not fields or all(not field.strip() for field in fields):
                    continue
                read_count += 1

                # Mictronics records are tab-delimited. A whitespace fallback
                # makes the converter tolerant of copies with tabs expanded.
                if len(fields) < 3:
                    fields = fields[0].split(maxsplit=3)
                if len(fields) < 3:
                    continue

                address = fields[0].strip().upper()
                registration = fields[1].strip()
                model_icao = fields[2].strip().upper()

                if not ADDRESS_PATTERN.fullmatch(address) or model_icao not in model_codes:
                    continue

                writer.writerow(
                    {
                        "Address": address,
                        "Registration": registration,
                        "IATA": "",
                        "ICAO": model_icao,
                        "Manufactured": "",
                        "Provenance": "",
                    }
                )
                written_count += 1

    return read_count, written_count


def main() -> int:
    arguments = parse_arguments()
    try:
        database_path = database_path_from_environment()
        model_codes = load_model_icao_codes(database_path)
        read_count, written_count = convert(arguments.input, arguments.output, model_codes)
    except (OSError, sqlite3.Error, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    print(
        f"Wrote {written_count} of {read_count} input records to {arguments.output} "
        f"using {len(model_codes)} known model ICAO codes."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

"""
Create a CSV session log template for selected days of the week
"""

from __future__ import annotations

import argparse
import calendar
import csv
from datetime import date, timedelta
from pathlib import Path
import sys
from typing import Iterator


OUTPUT_HEADERS = ("Date", "Day", "Week number")
INCLUDED_WEEKDAYS = {0, 2, 4, 5, 6}  # Mon, Wed, Fri, Sat, Sun


def positive_integer(value: str) -> int:
    """Return value as a positive integer for use by argparse."""
    try:
        number = int(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("must be an integer") from error
    if number < 1:
        raise argparse.ArgumentTypeError("must be at least 1")
    return number


def parse_date(value: str) -> date:
    """Parse an ISO-format command-line date."""
    try:
        return date.fromisoformat(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("must be a valid date in YYYY-MM-DD format") from error


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("-s", "--start_date", type=parse_date, required=True,
                        help="first date covered, in YYYY-MM-DD format")
    parser.add_argument("-m", "--months", type=positive_integer, required=True,
                        help="number of calendar months to cover")
    parser.add_argument("-o", "--output", type=Path, required=True, help="output CSV path")
    return parser.parse_args()


def add_months(value: date, months: int) -> date:
    """Add calendar months, clamping the day to the destination month."""
    month_index = value.month - 1 + months
    year = value.year + month_index // 12
    month = month_index % 12 + 1
    day = min(value.day, calendar.monthrange(year, month)[1])
    return date(year, month, day)


def observation_rows(start_date: date, months: int) -> Iterator[dict[str, object]]:
    """Yield selected dates from start_date up to, but not including, the end date."""
    end_date = add_months(start_date, months)
    # Python numbers Monday as zero, so this finds the Sunday starting the
    # week that contains start_date.
    first_week_sunday = start_date - timedelta(days=(start_date.weekday() + 1) % 7)

    current_date = start_date
    while current_date < end_date:
        if current_date.weekday() in INCLUDED_WEEKDAYS:
            week_number = (current_date - first_week_sunday).days // 7 + 1
            yield {
                "Date": current_date.strftime("%d/%m/%Y"),
                "Day": current_date.strftime("%a"),
                "Week number": week_number,
            }
        current_date += timedelta(days=1)


def create_observation_log(
    output_path: Path, start_date: date, months: int
) -> int:
    """Write an observation log and return the number of data rows written."""
    rows = list(observation_rows(start_date, months))
    with output_path.open("w", encoding="utf-8", newline="") as destination:
        writer = csv.DictWriter(destination, fieldnames=OUTPUT_HEADERS)
        writer.writeheader()
        writer.writerows(rows)
    return len(rows)


def main() -> int:
    arguments = parse_arguments()
    try:
        row_count = create_observation_log(
            arguments.output, arguments.start_date, arguments.months
        )
    except (OSError, OverflowError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    print(f"Wrote {row_count} rows to {arguments.output}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

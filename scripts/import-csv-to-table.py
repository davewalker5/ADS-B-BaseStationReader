import argparse
import csv
import sqlite3
import sys
from pathlib import Path


def get_table_columns(conn: sqlite3.Connection, table: str) -> list[str]:
    cursor = conn.execute(f'PRAGMA table_info("{table}")')
    rows = cursor.fetchall()

    if not rows:
        raise ValueError(f"Table '{table}' does not exist.")

    return [row[1] for row in rows]


def import_csv(
    csv_path: Path,
    db_path: Path,
    table: str,
    ignore_duplicates: bool = False,
) -> tuple[int, int]:
    inserted = 0
    skipped = 0

    with csv_path.open("r", newline="", encoding="utf-8-sig") as csv_file:
        reader = csv.DictReader(csv_file)

        if not reader.fieldnames:
            raise ValueError("CSV file has no header row.")

        csv_columns = reader.fieldnames

        if "Id" not in csv_columns:
            raise ValueError("CSV file must contain an 'Id' column.")

        with sqlite3.connect(db_path) as conn:
            table_columns = get_table_columns(conn, table)

            unknown_columns = [
                column
                for column in csv_columns
                if column not in table_columns
            ]

            if unknown_columns:
                raise ValueError(
                    "CSV contains columns not present in the target table: "
                    + ", ".join(unknown_columns)
                )

            quoted_columns = ", ".join(
                f'"{column}"' for column in csv_columns
            )

            placeholders = ", ".join(
                f":{column}" for column in csv_columns
            )

            if ignore_duplicates:
                insert_sql = (
                    f'INSERT OR IGNORE INTO "{table}" '
                    f"({quoted_columns}) VALUES ({placeholders})"
                )
            else:
                insert_sql = (
                    f'INSERT INTO "{table}" '
                    f"({quoted_columns}) VALUES ({placeholders})"
                )

            try:
                for row_number, row in enumerate(reader, start=2):
                    before = conn.total_changes

                    try:
                        conn.execute(insert_sql, row)
                    except sqlite3.IntegrityError as exc:
                        raise ValueError(
                            f"Import failed at CSV row {row_number}: {exc}"
                        ) from exc

                    if conn.total_changes > before:
                        inserted += 1
                    else:
                        skipped += 1

            except Exception:
                conn.rollback()
                raise

            conn.commit()

    return inserted, skipped


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("-c", "--csv", type=Path, help="Path to the CSV file")
    parser.add_argument("-db", "--database", type=Path, help="Path to the SQLite database")
    parser.add_argument("-t", "--table", help="Target table name")
    parser.add_argument("-id", "--ignore-duplicates",action="store_true", 
        help=(
            "Skip rows that violate a UNIQUE or PRIMARY KEY constraint "
            "instead of aborting the import"
        )
    )
    args = parser.parse_args()

    if not args.csv.is_file():
        print(f"Error: CSV file not found: {args.csv}", file=sys.stderr)
        return 1

    if not args.database.is_file():
        print(
            f"Error: database file not found: {args.database}",
            file=sys.stderr,
        )
        return 1

    try:
        inserted, skipped = import_csv(
            csv_path=args.csv,
            db_path=args.database,
            table=args.table,
            ignore_duplicates=args.ignore_duplicates,
        )

    except (sqlite3.Error, ValueError, OSError) as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    print(f"Import complete - inserted {inserted} rows, skipped {skipped} duplicates.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
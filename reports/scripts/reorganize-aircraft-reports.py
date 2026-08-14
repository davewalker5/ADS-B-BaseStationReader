#!/usr/bin/env python3
"""Reorganize aircraft reports into per-session folders."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


REPORT_NAME = re.compile(
    r"^(?P<address>[0-9A-Fa-f]{6})-(?P<callsign>.+)-(?P<session>[1-9][0-9]*)$"
)
DEFAULT_REPORTS_DIR = Path(__file__).resolve().parent.parent.parent / "data/reports/aircraft"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Move <address>-<callsign>-<session> report folders to "
            "<session>/<address>-<callsign>."
        )
    )
    parser.add_argument(
        "--reports-dir",
        type=Path,
        default=DEFAULT_REPORTS_DIR,
        help=f"aircraft reports folder (default: {DEFAULT_REPORTS_DIR})",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="show the moves without changing any files",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    reports_dir = args.reports_dir.expanduser().resolve()

    if not reports_dir.is_dir():
        print(f"Error: reports folder not found: {reports_dir}", file=sys.stderr)
        return 2

    moves: list[tuple[Path, Path]] = []
    skipped: list[Path] = []

    for source in sorted(reports_dir.iterdir()):
        if not source.is_dir():
            continue

        match = REPORT_NAME.fullmatch(source.name)
        if match is None:
            # This also ignores session folders created by an earlier run.
            skipped.append(source)
            continue

        aircraft_name = f"{match['address']}-{match['callsign']}"
        destination = reports_dir / match["session"] / aircraft_name
        moves.append((source, destination))

    collisions = [(source, destination) for source, destination in moves if destination.exists()]
    if collisions:
        for source, destination in collisions:
            print(
                f"Error: cannot move {source.name}; destination already exists: {destination}",
                file=sys.stderr,
            )
        print("No folders were moved.", file=sys.stderr)
        return 1

    action = "Would move" if args.dry_run else "Moving"
    for source, destination in moves:
        print(f"{action}: {source} -> {destination}")
        if not args.dry_run:
            destination.parent.mkdir(parents=True, exist_ok=True)
            source.rename(destination)

    suffix = " (dry run)" if args.dry_run else ""
    print(f"{len(moves)} folder(s) {'matched' if args.dry_run else 'moved'}{suffix}.")
    if skipped:
        print(f"Ignored {len(skipped)} folder(s) that did not match the expected name.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

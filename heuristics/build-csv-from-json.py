import argparse
import csv
import json
from pathlib import Path

def normalize_for_key(s: str) -> str:
    """Normalize a string for de-duplication: strip, collapse spaces, uppercase."""
    if s is None:
        return ""
    return " ".join(str(s).split()).upper()

def read_json(path: Path):
    """Read a json file"""
    try:
        with path.open("r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"Warning: could not read {path}: {e}")
        return None

def extract_entries(payload: dict):
    """Yield dicts with number, callsign, iata, icao from departures and arrivals."""
    if not isinstance(payload, list):
        return

    for rec in payload:
        if not isinstance(rec, dict):
            continue

        number = rec["number"]
        call_sign = rec["callsign"]
        iata = rec["iata"]
        icao = rec["icao"]

        # Discard if call sign or airline ICAO is missing/blank
        if not call_sign or not str(call_sign).strip() or not icao or not str(icao).strip():
            continue

        yield {
            "number": number,
            "callsign": str(call_sign).strip(),
            "airline_iata": (str(iata).strip() if iata is not None else ""),
            "airline_icao": (str(icao).strip() if icao is not None else ""),
        }

def main():
    parser = argparse.ArgumentParser(
        description="Extract unique flight (number, callsign, airline IATA/ICAO) from schedule JSON files."
    )
    parser.add_argument("folder", help="Folder containing .json files")
    parser.add_argument(
        "-o", "--output",
        default="callsigns.csv",
        help="Output CSV path (default: flights_callsigns.csv)"
    )
    args = parser.parse_args()

    folder = Path(args.folder)
    if not folder.is_dir():
        raise SystemExit(f"Not a directory: {folder}")

    seen = set()
    rows = []

    json_files = sorted(folder.glob("*.json"))
    if not json_files:
        print(f"No .json files found in {folder}")
    for path in json_files:
        data = read_json(path)
        if data is None:
            continue
        for entry in extract_entries(data):
            # Build a case/space-normalized key for deduplication
            key = (
                normalize_for_key(entry["number"]),
                normalize_for_key(entry["callsign"]),
                normalize_for_key(entry["airline_iata"]),
                normalize_for_key(entry["airline_icao"]),
            )
            if key in seen:
                continue
            seen.add(key)
            rows.append(entry)

    # Write CSV
    out_path = Path(args.output)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=["number", "callsign", "airline_iata", "airline_icao"]
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"Wrote {len(rows)} unique rows to {out_path}")

if __name__ == "__main__":
    main()
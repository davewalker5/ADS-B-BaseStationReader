"""
Build a callsign→IATA flight heuristic model from static schedules.

Input CSV columns (required):
    number,callsign,airline_iata,airline_icao

- `callsign`     : ICAO flight ID as seen in ADS-B (e.g., EXS3CM, BAW2777)
- `airline_icao` : Airline ICAO (e.g., EXS, BAW)
- `airline_iata` : Airline IATA (e.g., LS, BA)
- `number`       : The IATA flight designator or digits. Script tolerates:
                    - full (e.g., LS239, BA2777, CA856)
                    - digits-only (e.g., 239, 2777, 856)

Outputs (CSV files in --outdir):
    confirmed_mappings.csv   : exact callsign → IATA flight mapping (ground truth pairs)
    num_suffix_rules.csv     : learned (num, suffix) → digits rules (with support/purity)
    suffix_delta_rules.csv   : learned suffix → delta rules (digits = num + delta)
    airline_constants.csv    : airline-level constants and identity rate

Usage:
    python build-flight-number-model.py --in schedules.csv --outdir ./model

Thresholds (defaults chosen conservatively; adjust as needed):
    --ns-min-support 3     : min occurrences to accept (num,suffix) rule
    --ns-min-purity  0.90  : min purity to accept (num,suffix) rule
    --sd-min-support 5     : min occurrences to accept suffix→delta rule
    --sd-min-purity  0.85  : min purity to accept suffix→delta rule
    --delta-min-purity 0.90: airline-wide constant delta acceptance
"""

import argparse
import csv
import os
import re
from collections import Counter, defaultdict
from typing import Dict, Iterable, List, Optional, Tuple

# --- Default model parameters ------------------------------------------------

DEFAULT_NS_MIN_SUPPORT = 3
DEFAULT_NS_MIN_PURITY = 0.90
DEFAULT_SD_MIN_SUPPORT = 5
DEFAULT_SD_MIN_PURITY = 0.85
DEFAULT_DELTA_MIN_PURITY = 0.90

# --- Regex parsers -----------------------------------------------------------

RX_CALLSIGN = re.compile(r"^([A-Z]{3})(\d+)([A-Z]*)$")   # e.g., EXS3CM
RX_IATA_FLT = re.compile(r"^([A-Z0-9]{2})(\d+)[A-Z]?$")  # e.g., LS239, BA2777, CA856
RX_DIGITS   = re.compile(r"^\d+$")                       # e.g., 239


def parse_callsign(s: str) -> Optional[Tuple[str, int, str]]:
    m = RX_CALLSIGN.match((s or "").strip().upper())
    if not m:
        return None
    icao, num, suffix = m.groups()
    try:
        return icao, int(num), suffix
    except ValueError:
        return None


def parse_iata_number(number: str, airline_iata: str) -> Optional[Tuple[str, int, str]]:
    """
    Returns (iata_airline, digits, full_designator) or None.
    Accepts either full 'XX123' or digits-only '123' (then uses airline_iata).
    """
    raw = (number or "").strip().upper()
    iata = (airline_iata or "").strip().upper()

    # Full designator 'XX123'
    m = RX_IATA_FLT.match(raw)
    if m:
        iata_from_num, d = m.groups()
        try:
            digits = int(d)
        except ValueError:
            return None
        return iata_from_num, digits, f"{iata_from_num}{digits}"

    # Digits-only, combine with airline_iata
    if RX_DIGITS.match(raw) and len(iata) in (2, 3):  # tolerate 3-char IATA edge-cases
        try:
            digits = int(raw)
        except ValueError:
            return None
        return iata, digits, f"{iata}{digits}"

    return None


# --- Data structures ---------------------------------------------------------

class FlightRow:
    __slots__ = ("icao", "iata", "callsign", "num", "suffix", "digits", "full_iata")

    def __init__(self, icao: str, iata: str, callsign: str, num: int, suffix: str, digits: int, full_iata: str):
        self.icao = icao
        self.iata = iata
        self.callsign = callsign
        self.num = num          # numeric part of ICAO callsign
        self.suffix = suffix    # trailing letters on callsign
        self.digits = digits    # numeric part of IATA flight
        self.full_iata = full_iata  # full IATA flight designator, e.g., LS239


# --- Helpers ----------------------------------------------------------------

def majority(values: List[int]) -> Tuple[int, float]:
    """Return (majority_value, purity)."""
    if not values:
        return 0, 0.0
    c = Counter(values)
    val, cnt = c.most_common(1)[0]
    purity = cnt / len(values)
    return val, purity


def infer_constant_prefix(pairs: Iterable[Tuple[int, int]]) -> Optional[str]:
    """
    If digits look like: digits_str == PREFIX + num_str with the same PREFIX across all rows,
    return that PREFIX. Else None.
    """
    prefixes = set()
    for num, digits in pairs:
        ns = str(num)
        ds = str(digits)
        if not ds.endswith(ns):
            return None
        prefixes.add(ds[:len(ds)-len(ns)])
    return list(prefixes)[0] if len(prefixes) == 1 else None


# --- Training ----------------------------------------------------------------

def train(rows: List[FlightRow],
          ns_min_support: int,
          ns_min_purity: float,
          sd_min_support: int,
          sd_min_purity: float,
          delta_min_purity: float):
    """
    Returns dicts ready to emit to CSVs:
        exact_list
        num_suffix_rules
        suffix_delta_rules
        airline_constants
    """

    # Group by (airline_icao, airline_iata)
    groups: Dict[Tuple[str, str], List[FlightRow]] = defaultdict(list)
    for r in rows:
        groups[(r.icao, r.iata)].append(r)

    exact_list: List[Dict[str, str]] = []
    num_suffix_rules: List[Dict[str, str]] = []
    suffix_delta_rules: List[Dict[str, str]] = []
    airline_constants: List[Dict[str, str]] = []

    for (icao, iata), g in groups.items():
        # 1) exact pairs
        # If multiple full_iata per callsign appear, we still record last seen;
        # you can dedup externally if needed.
        for r in g:
            exact_list.append({
                "airline_icao": icao,
                "airline_iata": iata,
                "callsign": r.callsign,
                "iata_flight": r.full_iata,
                "digits": str(r.digits),
            })

        # 2) (num, suffix) → digits
        by_num_suffix: Dict[Tuple[int, str], List[int]] = defaultdict(list)
        for r in g:
            by_num_suffix[(r.num, r.suffix)].append(r.digits)

        ns_good = ns_total = 0
        for (num, suffix), digits_list in by_num_suffix.items():
            ns_total += 1
            maj, purity = majority(digits_list)
            if purity >= ns_min_purity and len(digits_list) >= ns_min_support:
                num_suffix_rules.append({
                    "airline_icao": icao,
                    "airline_iata": iata,
                    "num": str(num),
                    "suffix": suffix,
                    "digits": str(maj),
                    "support": str(len(digits_list)),
                    "purity": f"{purity:.4f}",
                })
                ns_good += 1

        # 3) suffix → delta (digits - num)
        by_suffix_delta: Dict[str, List[int]] = defaultdict(list)
        for r in g:
            if r.suffix:
                by_suffix_delta[r.suffix].append(r.digits - r.num)

        for suffix, deltas in by_suffix_delta.items():
            maj_delta, purity = majority(deltas)
            if purity >= sd_min_purity and len(deltas) >= sd_min_support:
                suffix_delta_rules.append({
                    "airline_icao": icao,
                    "airline_iata": iata,
                    "suffix": suffix,
                    "delta": str(maj_delta),
                    "support": str(len(deltas)),
                    "purity": f"{purity:.4f}",
                })

        # 4) airline-level constants
        deltas_all = [r.digits - r.num for r in g]
        const_delta, delta_purity = majority(deltas_all)
        const_prefix = infer_constant_prefix([(r.num, r.digits) for r in g])
        identity_rate = sum(1 for r in g if r.digits == r.num) / len(g) if g else 0.0

        airline_constants.append({
            "airline_icao": icao,
            "airline_iata": iata,
            "constant_delta": str(const_delta) if delta_purity >= delta_min_purity else "",
            "constant_delta_purity": f"{delta_purity:.4f}",
            "constant_prefix": const_prefix or "",
            "identity_rate": f"{identity_rate:.4f}",
            # Optional metadata
            "rows_seen": str(len(g)),
            "ns_rules_learned": str(ns_good),
            "ns_rules_candidates": str(ns_total),
        })

    return exact_list, num_suffix_rules, suffix_delta_rules, airline_constants


# --- I/O ---------------------------------------------------------------------

def read_input_csv(path: str) -> List[FlightRow]:
    rows: List[FlightRow] = []
    bad = 0

    with open(path, newline="", encoding="utf-8") as f:
        rdr = csv.DictReader(f)
        required = {"number", "callsign", "airline_iata", "airline_icao"}
        missing = required - set((h or "").strip() for h in rdr.fieldnames or [])
        if missing:
            raise SystemExit(f"ERROR: Input CSV missing required columns: {', '.join(sorted(missing))}")

        for i, rec in enumerate(rdr, start=2):
            try:
                call = (rec.get("callsign") or "").strip().upper()
                a_icao = (rec.get("airline_icao") or "").strip().upper()
                a_iata = (rec.get("airline_iata") or "").strip().upper()
                number = (rec.get("number") or "").strip().upper()

                # Parse callsign
                pc = parse_callsign(call)
                if not pc:
                    bad += 1
                    continue
                icao, num, suffix = pc

                # Airline sanity: prefer explicit airline columns, but let real callsign ICAO win if mismatch is obvious
                if a_icao and a_icao != icao:
                    # Keep the callsign ICAO (it reflects ATC reality, can differ due to wet-lease/codeshare)
                    pass

                # Parse IATA designator/digits
                pn = parse_iata_number(number, a_iata)
                if not pn:
                    bad += 1
                    continue
                iata, digits, full_iata = pn

                # If airline_iata present and conflicts with parsed flight's IATA, keep parsed
                rows.append(FlightRow(icao=icao, iata=iata, callsign=call,
                                      num=num, suffix=suffix, digits=digits, full_iata=full_iata))
            except Exception:
                bad += 1
                continue

    if bad:
        print(f"NOTE: Skipped {bad} row(s) due to parse/validation issues.")
    return rows


def write_csv(path: str, rows: List[Dict[str, str]], fieldnames: List[str]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=fieldnames)
        w.writeheader()
        for r in rows:
            w.writerow({k: r.get(k, "") for k in fieldnames})


# --- Main --------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Build a callsign→IATA flight heuristic model from schedules.")
    ap.add_argument("--in", dest="in_csv", required=True, help="Input CSV (number,callsign,airline_iata,airline_icao)")
    ap.add_argument("--outdir", required=True, help="Output directory for CSV model files")
    ap.add_argument("--ns-min-support", type=int, default=DEFAULT_NS_MIN_SUPPORT, help="Min support for (num,suffix) rule")
    ap.add_argument("--ns-min-purity", type=float, default=DEFAULT_NS_MIN_PURITY, help="Min purity for (num,suffix) rule")
    ap.add_argument("--sd-min-support", type=int, default=DEFAULT_SD_MIN_SUPPORT, help="Min support for suffix→delta rule")
    ap.add_argument("--sd-min-purity", type=float, default=DEFAULT_SD_MIN_PURITY, help="Min purity for suffix→delta rule")
    ap.add_argument("--delta-min-purity", type=float, default=DEFAULT_DELTA_MIN_PURITY, help="Min purity for constant delta")

    args = ap.parse_args()

    rows = read_input_csv(args.in_csv)

    exact, ns_rules, sd_rules, airline_consts = train(
        rows,
        ns_min_support=args.ns_min_support,
        ns_min_purity=args.ns_min_purity,
        sd_min_support=args.sd_min_support,
        sd_min_purity=args.sd_min_purity,
        delta_min_purity=args.delta_min_purity,
    )

    write_csv(
        os.path.join(args.outdir, "confirmed_mappings.csv"),
        exact,
        ["airline_icao", "airline_iata", "callsign", "iata_flight", "digits"],
    )

    write_csv(
        os.path.join(args.outdir, "num_suffix_rules.csv"),
        ns_rules,
        ["airline_icao", "airline_iata", "num", "suffix", "digits", "support", "purity"],
    )

    write_csv(
        os.path.join(args.outdir, "suffix_delta_rules.csv"),
        sd_rules,
        ["airline_icao", "airline_iata", "suffix", "delta", "support", "purity"],
    )

    write_csv(
        os.path.join(args.outdir, "airline_constants.csv"),
        airline_consts,
        ["airline_icao", "airline_iata", "constant_delta", "constant_delta_purity",
         "constant_prefix", "identity_rate", "rows_seen", "ns_rules_learned", "ns_rules_candidates"],
    )

    print(f"Model CSVs written to: {args.outdir}")
    print("Files:")
    for name in ("confirmed_mappings.csv", "num_suffix_rules.csv", "suffix_delta_rules.csv", "airline_constants.csv"):
        print("  -", os.path.join(args.outdir, name))


if __name__ == "__main__":
    main()

#!/bin/bash

set -u

usage() {
    # Keep wrapper help focused on environment setup; Python owns the option details.
    echo "Usage: $(basename "$0") --input <input.csv> [--orientation portrait|landscape] [--rows N] [--columns N] [--mapbox-token TOKEN]" >&2
}

if [[ $# -eq 0 ]]; then
    usage
    exit 2
fi

# Resolve paths relative to this script so the generator can be launched anywhere.
reports_root=$( cd "$( dirname "$0" )/.." && pwd )
python="$reports_root/venv/bin/python"
export MPLCONFIGDIR="${TMPDIR:-/tmp}/ads-b-contact-sheet-matplotlib"

if [[ ! -x "$python" ]]; then
    echo "Error: reporting environment not found; run reports/make_venv.sh first." >&2
    exit 1
fi

# Pass every contact-sheet option through unchanged to the Python command.
exec "$python" "$reports_root/scripts/contact_sheet.py" "$@"

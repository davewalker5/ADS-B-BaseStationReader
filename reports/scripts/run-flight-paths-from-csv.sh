#!/bin/bash

set -u

usage() {
    echo "Usage: $(basename "$0") <input.csv>" >&2
}

trim() {
    local value="$1"

    value="${value#\"}"
    value="${value%\"}"
    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"
    printf '%s' "$value"
}

if [[ $# -eq 1 && ( "$1" == "-h" || "$1" == "--help" ) ]]; then
    usage
    exit 0
fi

if [[ $# -ne 1 ]]; then
    usage
    exit 2
fi

input_csv="$1"

if [[ ! -f "$input_csv" ]]; then
    echo "Error: CSV file not found: $input_csv" >&2
    exit 2
fi

# Resolve all report paths relative to this script, so it can be run anywhere.
reports_root=$( cd "$( dirname "$0" )/.." && pwd )
project_root=$(cd "$reports_root/.." && pwd)
notebook_dir="$reports_root/notebooks/aircraft"
notebook_name="plot-flight-path.ipynb"
papermill="$reports_root/venv/bin/papermill"

if [[ ! -f "$notebook_dir/$notebook_name" ]]; then
    echo "Error: notebook not found: $notebook_dir/$notebook_name" >&2
    exit 1
fi

if [[ ! -x "$papermill" ]]; then
    echo "Error: papermill not found or not executable: $papermill" >&2
    exit 1
fi

line_number=0
run_count=0

while IFS=, read -r address session_id extra || [[ -n "${address}${session_id}${extra}" ]]; do
    line_number=$((line_number + 1))

    # Remove carriage returns used by Windows-style CSV files.
    address=${address%$'\r'}
    session_id=${session_id%$'\r'}
    extra=${extra%$'\r'}

    if [[ $line_number -eq 1 ]]; then
        # Also tolerate a UTF-8 byte-order mark at the start of the file.
        address=${address#$'\xef\xbb\xbf'}
        if [[ "$(trim "$address")" != "Address" || "$(trim "$session_id")" != "Session ID" || -n "$extra" ]]; then
            echo 'Error: expected CSV header: Address,Session ID' >&2
            exit 2
        fi
        continue
    fi

    address=$(trim "$address")
    session_id=$(trim "$session_id")
    extra=$(trim "$extra")

    # Ignore empty rows, but reject incomplete or additional columns.
    if [[ -z "$address" && -z "$session_id" && -z "$extra" ]]; then
        continue
    fi

    if [[ -z "$address" || -z "$session_id" || -n "$extra" ]]; then
        echo "Error: invalid row $line_number; expected Address,Session ID" >&2
        exit 2
    fi

    if [[ ! "$address" =~ ^[[:xdigit:]]{6}$ ]]; then
        echo "Error: invalid aircraft address on row $line_number: $address" >&2
        exit 2
    fi

    if [[ ! "$session_id" =~ ^[1-9][0-9]*$ ]]; then
        echo "Error: invalid session ID on row $line_number: $session_id" >&2
        exit 2
    fi

    address=$(printf '%s' "$address" | tr '[:lower:]' '[:upper:]')
    output_folder="$project_root/data/reports/aircraft"
    echo "Running $notebook_name for aircraft $address, session $session_id"

    if ! (
        cd "$notebook_dir" || exit 1
        export REPORT_OUTPUT_FOLDER="$output_folder"
        # Papermill treats /dev/null as a valid output, but warns because it has
        # no filename extension. Hide only that warning; retain all others.
        export PYTHONWARNINGS="ignore:the file is not specified with any extension:UserWarning:papermill.iorw${PYTHONWARNINGS:+,$PYTHONWARNINGS}"
        "$papermill" \
            --parameters session_id "$session_id" \
            --parameters_raw aircraft_address "$address" \
            "$notebook_name" /dev/null
    ); then
        echo "Error: notebook failed for aircraft $address, session $session_id" >&2
        exit 1
    fi

    run_count=$((run_count + 1))
done < "$input_csv"

if [[ $line_number -eq 0 ]]; then
    echo "Error: CSV file is empty: $input_csv" >&2
    exit 2
fi

echo "Completed $run_count flight-path notebook run(s)."

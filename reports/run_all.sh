#!/bin/bash -f

usage() {
    cat <<EOF
Usage: $(basename "$0") [--session <session>] [--address <address>]

Run aggregate notebooks when no arguments are supplied, session notebooks when
--session is supplied, or aircraft notebooks when both options are supplied.
EOF
}

session_id=""
aircraft_address=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --session)
            if [[ $# -lt 2 || -z "$2" ]]; then
                echo "Error: --session requires a value." >&2
                usage >&2
                exit 2
            fi
            session_id="$2"
            shift 2
            ;;
        --address)
            if [[ $# -lt 2 || -z "$2" ]]; then
                echo "Error: --address requires a value." >&2
                usage >&2
                exit 2
            fi
            aircraft_address="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Error: unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -n "$session_id" && ! "$session_id" =~ ^[1-9][0-9]*$ ]]; then
    echo "Error: --session must be a positive integer." >&2
    exit 2
fi

if [[ -n "$aircraft_address" && ! "$aircraft_address" =~ ^[[:xdigit:]]{6}$ ]]; then
    echo "Error: --address must be a six-digit hexadecimal ICAO address." >&2
    exit 2
fi

if [[ -n "$aircraft_address" && -z "$session_id" ]]; then
    echo "Error: --address can only be used with --session." >&2
    exit 2
fi

# macOS ships Bash 3.2, which does not support ${variable^^} expansion.
aircraft_address_upper=$(printf '%s' "$aircraft_address" | tr '[:lower:]' '[:upper:]')

# Resolve paths relative to this script so it can be launched from anywhere.
export REPORTS_ROOT
REPORTS_ROOT=$(cd "$(dirname "$0")" && pwd)
export PROJECT_ROOT
PROJECT_ROOT=$(cd "$REPORTS_ROOT/.." && pwd)
. "$REPORTS_ROOT/venv/bin/activate"

# Suppress warnings about the output file extension.
export PYTHONWARNINGS="ignore"

# Notebook basenames that should never be executed by this runner.
declare -a exclusions=(
    "database.ipynb"
    "export.ipynb"
    "pathutils.ipynb"
    "report-header.ipynb"
)

is_excluded() {
    local filename="$1"
    local excluded

    for excluded in "${exclusions[@]}"; do
        if [[ "$filename" == "$excluded" ]]; then
            return 0
        fi
    done

    return 1
}

run_notebooks() {
    local notebook_group="$1"
    local output_folder="$2"
    shift 2

    local notebook_root="$REPORTS_ROOT/notebooks/$notebook_group"
    local file
    local filename
    local notebook_dir

    if [[ ! -d "$notebook_root" ]]; then
        echo "Error: notebook folder not found: $notebook_root" >&2
        return 1
    fi

    # The shared notebook helper creates this folder and directs every report
    # export into it.
    export REPORT_OUTPUT_FOLDER="$output_folder"

    while IFS= read -r -d '' file; do
        filename=$(basename -- "$file")

        if is_excluded "$filename"; then
            continue
        fi

        notebook_dir=$(dirname -- "$file")
        echo "Running $notebook_group/$filename"

        # Run from the notebook's folder so its relative paths resolve there.
        if ! (
            cd "$notebook_dir" || exit 1
            papermill "$@" "$filename" /dev/null
        ); then
            echo "Error: notebook failed: $file" >&2
            return 1
        fi
    done < <(find "$notebook_root" -type f -name '*.ipynb' -print0 | sort -z)
}

if [[ -n "$aircraft_address" ]]; then
    run_notebooks "aircraft" \
        "$PROJECT_ROOT/data/reports/aircraft/$aircraft_address_upper/$session_id" \
        --parameters session_id "$session_id" \
        --parameters_raw aircraft_address "$aircraft_address_upper"
elif [[ -n "$session_id" ]]; then
    run_notebooks "session" \
        "$PROJECT_ROOT/data/reports/session/$session_id" \
        --parameters session_id "$session_id"
else
    run_notebooks "aggregate" "$PROJECT_ROOT/data/reports/aggregated"
fi

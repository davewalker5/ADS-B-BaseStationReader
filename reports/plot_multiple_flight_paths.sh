#!/bin/bash -f

if [[ $# -ne 2 ]]; then
    echo Usage: plot_flight_path.sh FILEPATH API-KEY
    exit 1
fi

# Activate the virtual environment
export REPORTS_ROOT=$( cd "$( dirname "$0" )" && pwd )
. $REPORTS_ROOT/venv/bin/activate

# Change to the notebooks folder
cd "$REPORTS_ROOT/notebooks"

# Ignore warnings
export PYTHONWARNINGS="ignore"

# Iterate ove the input file, which should have one address per line, and plot the
# corresponding flight path
while IFS= read -r line; do
    papermill plot-flight-path.ipynb /dev/null -p aircraft_address "$line" -p token "$2"
done < "$1"

cd "$REPORTS_ROOT"

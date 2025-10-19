#!/bin/bash -f

if [[ $# -ne 2 ]]; then
    echo Usage: plot_flight_path.sh ADDRESS API-KEY
    exit 1
fi

# Activate the virtual environment
export REPORTS_ROOT=$( cd "$( dirname "$0" )" && pwd )
. $REPORTS_ROOT/venv/bin/activate

# Change to the notebooks folder
cd "$REPORTS_ROOT/notebooks"

export PYTHONWARNINGS="ignore"
papermill plot-flight-path.ipynb /dev/null -p aircraft_address "$1" -p token "$2"

cd "$REPORTS_ROOT"

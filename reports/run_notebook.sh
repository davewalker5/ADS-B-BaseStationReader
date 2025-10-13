#!/bin/bash -f

if [[ $# -ne 1 ]]; then
    echo Usage: run_notebook.sh NOTEBOOK
    exit 1
fi

# Activate the virtual environment
export REPORTS_ROOT=$( cd "$( dirname "$0" )" && pwd )
. $REPORTS_ROOT/venv/bin/activate

# Change to the notebooks folder
cd "$REPORTS_ROOT/notebooks"

export PYTHONWARNINGS="ignore"
papermill "$1" /dev/null

cd "$REPORTS_ROOT"

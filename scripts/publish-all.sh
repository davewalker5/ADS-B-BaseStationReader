#!/usr/bin/env bash

if [[ $# -ne 1 ]]; then
    echo Usage: $(basename "$0") .NET-RID
    exit 1
fi


PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
. "$PROJECT_FOLDER/scripts/publish-sim.sh" "$1"
. "$PROJECT_FOLDER/scripts/publish-lookup.sh" "$1"
. "$PROJECT_FOLDER/scripts/publish-terminal.sh" "$1"
. "$PROJECT_FOLDER/scripts/publish-replayer.sh" "$1"

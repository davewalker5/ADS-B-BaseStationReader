#!/usr/bin/env bash

echo "Unique ICAO addresses:" && \
grep "Handled event for aircraft" "$1" | awk '{print $(NF-3)}' | sort -u | tee /dev/tty | wc -l

# ADS-B-BaseStationReader

[![Build Status](https://github.com/davewalker5/ADS-B-BaseStationReader/workflows/.NET%20Core%20CI%20Build/badge.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/actions)
[![GitHub issues](https://img.shields.io/github/issues/davewalker5/ADS-B-BaseStationReader)](https://github.com/davewalker5/ADS-B-BaseStationReader/issues)
[![Coverage Status](https://coveralls.io/repos/github/davewalker5/ADS-B-BaseStationReader/badge.svg?branch=main)](https://coveralls.io/github/davewalker5/ADS-B-BaseStationReader?branch=main)
[![Releases](https://img.shields.io/github/v/release/davewalker5/ADS-B-BaseStationReader.svg?include_prereleases)](https://github.com/davewalker5/ADS-B-BaseStationReader/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/blob/master/LICENSE)
[![Language](https://img.shields.io/badge/language-c%23-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/)
[![Language](https://img.shields.io/badge/database-SQLite-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/)
[![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/davewalker5/ADS-B-BaseStationReader)](https://github.com/davewalker5/ADS-B-BaseStationReader/)

## About

![Application Schematic](Diagrams/application-schematic.png)

- An RTL2832/R820T2 USB Dongle is plugged into the dump1090 host machine
- The host is running the [dump1090-mutability](https://github.com/adsb-related-code/dump1090-mutability) service to decode the data from the dongle
- One of the outputs is a decoded stream of messages in "[Basestation](http://woodair.net/sbs/article/barebones42_socket_data.htm)" format, that is exposed on a TCP port on the Pi
- This stream is read by the MessageReader, that exposes an event used to notify subscribers when a new message arrives
- The AircraftTracker subscribes to these events and passes each new message to the message parsers to have the information it contains extracted into an aircraft tracking object
- The AircraftTracker enqueues each new tracking object for asynchronous writing to the SQLite database
- It also exposes events to notify subscribers when aircraft are added, updated and removed
- The ContinuousWriter processes pending requests from the queue, in strictly serial order
- External API integrations are supported for automatic/manual lookup of:
  - Live and historical flight details
  - Airline details
  - Aircraft details
  - METAR reporting

## Getting Started

Please see the [Wiki](https://github.com/davewalker5/ADS-B-BaseStationReader/wiki) for configuration details and the user guide.

## Authors

- **Dave Walker** - _Initial work_ - [LinkedIn](https://www.linkedin.com/in/davewalker5/)

## Feedback

To file issues or suggestions, please use the [Issues](https://github.com/davewalker5/ADS-B-BaseStationReader/issues) page for this project on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

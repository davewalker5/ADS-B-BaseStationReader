# ADS-B BaseStation Reader

[![Build Status](https://github.com/davewalker5/ADS-B-BaseStationReader/workflows/.NET%20Core%20CI%20Build/badge.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/actions)
[![GitHub issues](https://img.shields.io/github/issues/davewalker5/ADS-B-BaseStationReader)](https://github.com/davewalker5/ADS-B-BaseStationReader/issues)
[![Coverage Status](https://coveralls.io/repos/github/davewalker5/ADS-B-BaseStationReader/badge.svg?branch=main)](https://coveralls.io/github/davewalker5/ADS-B-BaseStationReader?branch=main)
[![Releases](https://img.shields.io/github/v/release/davewalker5/ADS-B-BaseStationReader.svg?include_prereleases)](https://github.com/davewalker5/ADS-B-BaseStationReader/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/blob/master/LICENSE)
[![Language](https://img.shields.io/badge/language-c%23-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/)
[![Language](https://img.shields.io/badge/database-SQLite-blue.svg)](https://github.com/davewalker5/ADS-B-BaseStationReader/)
[![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/davewalker5/ADS-B-BaseStationReader)](https://github.com/davewalker5/ADS-B-BaseStationReader/)

## About

![ADS-B BaseStation Reader Observation Session](Diagrams/0200-live-tracker-tracking.png)

**ADS-B BaseStation Reader** is a local-first aircraft observation workspace that turns ADS-B signals received directly by the user into useful, inspectable records of what was observed.

The application consumes decoded messages in BaseStation format, typically supplied by `dump1090` connected to an RTL-SDR receiver. It organises each tracking run as an observation session, maintains a live view of aircraft currently being received, stores observations and position histories locally in SQLite, and provides an integrated browser-based UI supporting live observation, investigation, contextual aviation information, reference-data management and post-session analysis. Historical observations can be explored further using a companion suite of Jupyter notebooks that provide reporting and visual analysis across multiple observation sessions.

Core tracking does not depend on a commercial flight-tracking service. External APIs are optional and are used only for transient ancillary lookup, schedule, weather and enrichment workflows.

## Features

The project currently supports the following groups of features.

### Observation and Live Tracking

- Session-based observation from a BaseStation-compatible TCP message feed
- Session preparation with a name, optional notes, receiver endpoint and tracking-profile selection
- Configurable receiver location, altitude and distance limits, and aircraft-behaviour filters
- Live aircraft telemetry, lifecycle state and identification coverage
- Receiver-centred radar and accumulated position-density visualisation
- Read-only live and completed-session summaries

### Persistence, History and Analysis

- Local SQLite storage of observation sessions, aircraft records and optional position histories
- Optional persistence and later replay of position-density snapshots
- Durable, file-backed serial writing with deferred flushing and a standalone Spool Replayer
- Historical session and tracking-record browsing with filters and detailed record inspection
- Post-session analysis, 2D and 3D flight-path visualisation, and session management
- A read-only Jupyter reporting suite covering sessions, aircraft, flights, positions, density development, reference-data coverage and temporal activity

### Lookup and Operational Context

- Aircraft and flight lookup using local reference data first
- Optional transient external lookups without persisting returned business data
- Airport schedules, route visualisation, and METAR and TAF weather
- Context-preserving links between live aircraft, historical records, schedules, routes and weather

### Reference Data and Governance

- Searchable and editable local records for aircraft, airlines, airports, flights, manufacturers and aircraft models
- CSV import for each supported reference-data type
- First-class provenance recording for imported and manually maintained reference data
- Aircraft-address and callsign exclusion management
- Licence-conscious separation of direct observations, derived values, persistent local reference data and transient external context

### Applications, Integration and Deployment

- Integrated browser-based Tracker Hub for observation, investigation and reference-data workflows
- SignalR live updates for the integrated UI and compatible external clients
- Console tracker for lightweight, headless and small-screen environments
- ADS-B simulator for development and testing without live radio traffic
- Command-line lookup and data-import tooling
- Docker support for containerised deployment

## Local-First Observation

ADS-B BaseStation Reader deliberately distinguishes between three kinds of information:

### Observation

Data received directly from the ADS-B message stream, such as:

- ICAO 24-bit aircraft address
- Callsign
- Position
- Altitude
- Speed
- Heading
- Vertical rate

### Interpretation

Information derived locally from those observations, including:

- Distance from the receiver
- Climbing, descending or level flight
- Tracking-profile inclusion
- Aircraft lifecycle state
- Flight-path history

### Context

Operational information used to support an observing session, including airport schedules, route visualisation and weather.

Context helps explain what is being observed but is intentionally treated as transient information. Unlike locally curated reference data, it is not considered part of the observation record and is not used to populate the application's reference database.

### Enrichment

Additional reference information used to make an observation more recognisable, such as:

- Aircraft registration
- Manufacturer and model
- Airline
- Flight number
- Origin and destination
- Airport information

Locally managed reference data is preferred wherever possible. External API responses are treated as transient and are not used to populate or update the local reference database.

> [!IMPORTANT]
> Only import or retain data that you are permitted to store and reuse.
>
> The availability of an import, lookup or storage feature does not imply that data from a particular source may legally be persisted, transformed, redistributed or reused. Check the applicable licence and terms of service before importing or retaining third-party data.

## Data Provenance

Imported reference data is associated with a provenance record identifying its source, dataset, version and licence.

This allows locally stored reference information to remain traceable to the dataset from which it originated and helps preserve the distinction between:

- Directly observed ADS-B data
- Derived information
- Locally curated reference data
- Externally sourced enrichment

> [!IMPORTANT]
> Only import or retain data that you are permitted to store and reuse.
>
> The availability of an import, lookup or storage feature does not imply that data from a particular source may legally be persisted, transformed, redistributed or reused. Check the applicable licence and terms of service before importing or retaining third-party data.

## Integrated UI

The Tracker Hub hosts the main browser-based interface and brings the project's operational workflows together in one application.

The interface is organised around the natural workflow of an aircraft observer:

### Observation Workflow

The Live Tracker brings the active observation workflow together in five tabs:

> Session &rarr; Tracking &harr; Radar &harr; Position Density &rarr; Summary

- **Session** — set the receiver host and port, select a tracking profile, review the effective receiver and tracking limits, add optional notes, and start the session
- **Tracking** — monitor the live aircraft collection, inspect current telemetry, and move directly to Lookup or historical records
- **Radar** — view currently positioned aircraft by range and bearing from the receiver
- **Position Density** — view the accumulated geographical distribution of recorded aircraft positions during the current session, with denser areas highlighted spatially
- **Summary** — review the persisted session context, tracking activity, identification coverage, and notable observations

Each session records its name and notes, the receiver host and port, a snapshot of the effective tracking profile, and the aircraft records created during that run. Receiver details initially default to the values configured in `appsettings` and then retain the last-used values until Tracker Hub is restarted. Observing parameters become fixed when tracking starts; after the session has stopped, its name and notes can be updated through the Database Session Editor. When tracking stops, outstanding observations may be flushed immediately or retained in the durable spool for later replay before the completed summary is displayed.

### Observation and Investigation

The primary observation tools used while tracking aircraft:

- Session-based Live Tracking
- Integrated receiver-centred Radar
- Aircraft and Flight Lookup
- Historical Database Browser with Sessions, Tracking Records and Session Editor tabs
- Historical aircraft details and flight analysis

These views focus on aircraft currently being observed or previously recorded, allowing observations to be inspected, identified and analysed.

The Database opens on the **Sessions** tab. Sessions can be filtered using the recent-session selector or an unrestricted start-date range. Each row exposes its recorded context, including receiver host and port, provides session notes in a popup, opens the same analysis shown by the Live Tracker Summary tab, and links directly to the associated tracking records—even when the session is older than the recent-session dropdown.

When no observation session is active, each result also provides **Edit** and **Delete** actions. Edit opens the session in **Session Editor**, which shows the fixed receiver and tracking-profile context while allowing its name and notes to be updated and saved. A session can be deleted either from the results table or the editor after confirming the action; its tracked-aircraft records, position histories and position-density snapshots are deleted with it. The editor and both actions are unavailable during an active session. The **Tracking Records** tab retains aircraft, callsign, session and telemetry filtering, along with links to detailed historical records.

### Operational Context

The **Lookup** area groups supporting aviation information into a single tabbed workspace:

- Aircraft and Flights
- Schedule
- Route
- Weather

Links between these tools preserve their context—for example, a schedule result can be opened as a route or weather lookup. Links from Live Tracker and Radar continue to open Aircraft and Flights with the relevant aircraft details populated. These workflows help explain what is being observed while remaining conceptually separate from the tracking data itself.

### Reference Data Management

The **Reference Data** area provides tools for maintaining the locally curated information used by the application:

- Import
- Provenance
- Data Management:
  - Aircraft
  - Airlines
  - Airports
  - Flights
  - Manufacturers
  - Models

The data management tabs allow search, editing, deletion and addition of reference data records. Each record remains linked to a provenance record describing its source. Reference data records remain searchable during live tracking, but cannot be added, changed or deleted until the session ends.

This organisation reinforces the distinction between observed ADS-B data, transient contextual information and locally managed enrichment data.

The same underlying tracking core can also be used through the console tracker or exposed headlessly through SignalR.

## Historical Reporting

In addition to the integrated operational interface, ADS-B BaseStation Reader includes a companion historical reporting suite implemented using Jupyter notebooks.

These reports analyse accumulated observation data across completed sessions without modifying the application's SQLite database.

The reporting suite currently includes:

- Session Overview
- Aircraft Activity
- Callsign & Flight Activity
- Position & Flight Path Analysis
- Position Density Replay
- Reference Data Coverage
- Temporal Activity
- Aircraft Age Overview
- Aircraft Age by Type and Model
- Manufacturer and Era Analysis
- Observation Session Population Comparison
- Interesting Aircraft Explorer
- Aircraft Age Versus Observation Frequency

Together these reports help answer questions such as:

- Which aircraft are observed most frequently?
- Which flights and callsigns regularly appear?
- When is local airspace busiest?
- Where are aircraft most commonly observed?
- How complete is the local aviation reference database?
- How have observation patterns changed over time?
- How did position density develop during a particular session?
- How old are the observed aircraft, and which types and manufacturers contain the oldest examples?
- Which unusual or infrequently observed aircraft merit further investigation?

Unlike the integrated UI, which focuses on operational awareness during an active observation session, the reporting suite is intended for historical analysis and exploration across many completed sessions.

## Deployment

ADS-B BaseStation Reader supports several deployment styles:

### Docker

The integrated UI is available as a containerised application and can connect to a BaseStation feed running:

- On the Docker host
- In another container
- On another machine on the network

### Console

The console tracker provides a lightweight live display suitable for:

- Raspberry Pi deployments
- Small displays
- Terminal-based monitoring
- Headless or low-resource environments

## Getting Started

Full configuration details, deployment guidance and user documentation are available in the [project Wiki](https://github.com/davewalker5/ADS-B-BaseStationReader/wiki).

For Docker deployment, see the documentation for the `davewalker5/adsbtracker` image on [Docker Hub](https://hub.docker.com/repository/docker/davewalker5/adsbtracker).

For advice on recommended settings to use while observing, please see the [Observing with the Persistent Queue](https://github.com/davewalker5/ADS-B-BaseStationReader/wiki/Observing-with-the-Persistent-Queue) section of the Wiki.

## Authors

- **Dave Walker** - *Initial work* -

## Feedback

To file issues or suggestions, please use the [Issues](https://github.com/davewalker5/ADS-B-BaseStationReader/issues) page for this project on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

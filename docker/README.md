# adsbtracker

_adsbtracker_ is a containerised version of the ADS-B Tracker integrated UI, part of the [ADS-B BaseStation Reader](https://github.com/davewalker5/ADS-B-BaseStationReader) project.

![Live Tracker](https://raw.githubusercontent.com/davewalker5/ADS-B-BaseStationReader/main/Diagrams/001-live-tracker.png)

The integrated UI provides a browser-based interface for receiving, monitoring, storing, and analysing aircraft observations from a BaseStation-format ADS-B message feed, typically supplied by `dump1090`.

It supports:

- Live aircraft tracking with configurable receiver location and tracking profiles
- Local SQLite storage of aircraft observations and position histories
- Historical observation browsing and flight-path visualisation
- Aircraft, flight, airline, airport, manufacturer, and model reference data
- CSV-based reference-data import with provenance tracking
- Aircraft and flight lookup using local reference data and optional external API services
- Airport schedule and METAR/TAF weather lookup
- SignalR-based live aircraft updates for compatible external clients

ADS-B Tracker is designed as a **local-first personal aircraft observation system**. Core tracking uses ADS-B data received directly from your own BaseStation-compatible feed and does not depend on third-party flight-tracking services. External APIs are optional and are used only for ancillary lookup and enrichment features.

Reference and enrichment data should only be imported or retained where the licence and terms of the source permit storage and reuse. The application records provenance for imported reference datasets so that locally stored data can be traced back to its source.

Full documentation is available on the [project Wiki](https://github.com/davewalker5/ADS-B-BaseStationReader/wiki) in the GitHub repository.

## Getting Started

### Prerequisites

Docker must be installed on the host system.

- Windows
- macOS
- Linux

### Usage

While it is possible to run the image from the docker command line, it is **strongly** recommended that it is run using Docker Compose. The following is an example Docker Compose file:

```yaml
services:
  adsbtracker:
    container_name: adsbtracker
    image: davewalker5/adsbtracker:latest
    platform: linux/amd64
    restart: always
    ports:
      # Map the UI port in the container (5000) to an available local host port (8104)
      - "8104:5000"
    environment:
      # Host and port for the BaseStation message feed
      ApplicationSettings__Host: host.docker.internal
      ApplicationSettings__Port: 30003
      # Receiver co-ordinates
      ApplicationSettings__ReceiverLatitude: 51.470020
      ApplicationSettings__ReceiverLongitude: -0.454295
      # API keys for external API integrations
      ApplicationSettings__ApiServices__0__Key: put-your-aerodatabox-api-key-here
      ApplicationSettings__ApiServices__1__Key: put-your-airlabs-api-key-here
      ApplicationSettings__ApiServices__2__Key: put-your-checkwx-api-key-here
      ApplicationSettings__ApiServices__3__Key: put-your-skylink-api-key-here
      Mapbox__AccessToken: put-your-mapbox-api-key-here
      # In-container host and port for the SignalR hub
      WebUi__SignalRHubUrl: http://127.0.0.1:5000/hubs/aircraft
      # In-container folder where the tracking profiles are stored
      WebUI__TrackingProfilesPath: /var/opt/adsbtrackingprofiles/
      # In-container path to the tracking database
      ConnectionStrings__BaseStationReaderDB: "Data Source=/var/opt/adsbtracker/aircrafttracker.db;Default Timeout=5;Pooling=True"
    volumes:
      # Map the host folder where the database will be stored
      - /path/to/adsb/tracking/database/folder:/var/opt/adsbtracker/
      # Map the host folder where tracking profiles are stored
      - /path/to/tracking/profiles/folder:/var/opt/adsbtrackingprofiles/
```

- This should be saved as _docker-compose.yml_ in a convenient folder and modified to suit
- The service keys for API integrations can be omitted if not available or not required
- This will not affect the tracking capabilities of the application
- It merely means that the ancillary pages that use those integrations will simply return no results
- In the case of Mapbox, the flightpath renderer will not render the ground map under the flightpath

#### Running the Application

From the folder containing the _docker-compose.yml_ file:

```bash
docker compose --project-directory . up -d
```

Once running, open:

http://localhost:8104

to access the ADS-B tracking web interface.

## Find Us

- [ADS-B BaseStation Reader on GitHub](https://github.com/davewalker5/ADS-B-BaseStationReader)

## Versioning

For the versions available, see the [tags on this repository](https://github.com/davewalker5/ADS-B-BaseStationReader/tags).

## Authors

- **Dave Walker** - _Initial work_ -

See also the list of [contributors](https://github.com/davewalker5/ADS-B-BaseStationReader/contributors) who
participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/davewalker5/ADS-B-BaseStationReader/blob/master/LICENSE) file for details.

FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime

ARG VERSION

COPY adsbtracker-${VERSION}.0 /opt/adsbtracker

ENV AIRCRAFT_TRACKER_DB=/var/opt/adsbtracker/aircrafttracker.db

WORKDIR /opt/adsbtracker
ENTRYPOINT [ "./BaseStationReader.TrackerHub" ]

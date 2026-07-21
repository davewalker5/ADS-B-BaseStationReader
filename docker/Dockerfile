FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime

ARG VERSION

COPY adsbtracker-${VERSION}.0 /opt/adsbtracker

WORKDIR /opt/adsbtracker
ENTRYPOINT [ "./BaseStationReader.TrackerHub" ]

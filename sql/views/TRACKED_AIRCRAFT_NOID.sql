CREATE VIEW TRACKED_AIRCRAFT_NOID AS
SELECT  Address,
        Callsign,
        Squawk,
        Altitude,
        GroundSpeed,
        Track,
        Latitude,
        Longitude,
        Distance,
        VerticalRate,
        FirstSeen,
        LastSeen,
        Messages,
        Status
FROM    TRACKED_AIRCRAFT;

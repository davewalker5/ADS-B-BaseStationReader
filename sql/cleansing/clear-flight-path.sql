UPDATE  POSITION
SET     Altitude = NULL,
        Latitude = NULL,
        Longitude = NULL,
        Distance = NULL
WHERE   AircraftId IN (
            SELECT ta.Id
            FROM TRACKED_AIRCRAFT AS ta
            WHERE ta.SessionId = 0
            AND ta.Callsign = ''
        );
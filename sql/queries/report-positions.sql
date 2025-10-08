PRAGMA journal_mode=WAL;

SELECT      a.Address,
            a.Callsign,
            p.Altitude,
            p.Latitude,
            p.Longitude,
            p.Distance, 
            p.Timestamp
FROM        TRACKED_AIRCRAFT a
INNER JOIN  POSITION p on p.AircraftId = a.Id
WHERE       a.Address = ''
ORDER BY    p.Timestamp ASC;

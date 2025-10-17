SELECT      ta.Address,
            ta.Callsign,
            p.Altitude,
            p.Latitude,
            p.Longitude,
            p.Distance, 
            p.Timestamp
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  POSITION p on p.AircraftId = ta.Id
WHERE       ta.Address = ''
ORDER BY    p.Timestamp ASC;

SELECT      s.Id,
            s.Name,
            s.StartedAtUtc,
            ta.Address,
            ta.Callsign,
            p.Altitude,
            p.Latitude,
            p.Longitude,
            p.Distance, 
            p.Timestamp
FROM        SESSION s
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
INNER JOIN  POSITION p on p.AircraftId = ta.Id
WHERE       ta.Address = ''
AND         s.Id = 0
ORDER BY    p.Timestamp ASC;

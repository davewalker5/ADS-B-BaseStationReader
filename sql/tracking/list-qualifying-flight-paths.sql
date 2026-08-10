SELECT      s.Id,
            s.Name,
            s.StartedAtUtc,
            ta.Address,
            ta.Callsign,
            MIN( p.Altitude ) AS "Minumum_Altitude",
            MAX( p.Altitude ) AS "Maxumum_Altitude",
            MAX( p.Altitude ) - MIN( p.Altitude ) AS "Altitude_Change",
            COUNT( p.Id ) AS "Points"
FROM        SESSION s
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
INNER JOIN  POSITION p on p.AircraftId = ta.Id
WHERE       s.Id = 28
AND         p.Latitude IS NOT NULL
AND         p.Longitude IS NOT NULL
AND         p.Altitude IS NOT NULL
GROUP BY    ta.Address,
            s.Id
HAVING      MAX( p.Altitude ) - MIN( p.Altitude ) > 1000
AND         COUNT( p.Id ) >= 200
ORDER BY    COUNT( p.Id ) DESC,
            MAX( p.Altitude ) - MIN( p.Altitude ) DESC;

SELECT      ta.Address,
            ta.Callsign,
            MIN( p.Timestamp ) AS "Started",
            MAX( p.Timestamp ) AS "Ended",
            COUNT( p.Id ) AS "Points"
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  POSITION p on p.AircraftId = ta.Id
GROUP BY    ta.Address,
            ta.Callsign
ORDER BY    COUNT( p.Id ) DESC;

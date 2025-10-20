SELECT      ta.Address,
            ta.Callsign,
            MIN( p.Altitude ) AS "Minimum_Altitude",
            MAX( p.Altitude ) AS "Maximum_Altitude",
            MIN( p.Timestamp ) AS "Started",
            MAX( p.Timestamp ) AS "Ended",
            COUNT( p.Id ) AS "Points"
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  POSITION p on p.AircraftId = ta.Id
WHERE       ta.LastSeen BETWEEN '2025-10-19 00:00:00' AND '2025-10-20 00:00:00'
GROUP BY    ta.Address,
            ta.Callsign
HAVING      COUNT( p.Id ) > 50 AND ( MAX( p.Altitude ) - MIN( p.Altitude )) > 1000
ORDER BY    COUNT( p.Id ) DESC;

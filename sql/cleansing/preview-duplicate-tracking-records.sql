SELECT      ta.Address,
            ta.Callsign,
            ta.Status,
            COUNT( ta.Id ) AS "Count"
FROM        TRACKED_AIRCRAFT ta
WHERE       ta.SessionId IN (
    SELECT  MAX( Id )
    FROM    SESSION
)
GROUP BY    ta.Address,
            ta.Callsign,
            ta.Status
HAVING      COUNT( ta.Id ) > 1
ORDER BY    COUNT( ta.Id ) DESC;
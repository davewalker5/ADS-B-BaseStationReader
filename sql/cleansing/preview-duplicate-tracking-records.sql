SELECT      ta.Address,
            ta.Callsign,
            ta.Status,
            strftime("%Y-%m-%d", ta.LastSeen) AS "Date",
            COUNT( ta.Id ) AS "Count"
FROM        TRACKED_AIRCRAFT ta
GROUP BY    ta.Address,
            ta.Callsign,
            ta.Status,
            ta.LastSeen
HAVING      COUNT( ta.Id ) > 1
ORDER BY    ta.Address ASC;
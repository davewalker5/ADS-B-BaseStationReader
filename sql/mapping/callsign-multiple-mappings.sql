SELECT      fnm.Callsign, COUNT( fnm.Id ) AS "Count"
FROM        FLIGHT_NUMBER_MAPPING fnm
GROUP BY    fnm.Callsign
HAVING      COUNT( fnm.Id ) > 1
ORDER BY    fnm.Callsign;
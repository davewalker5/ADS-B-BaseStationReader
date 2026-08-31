WITH callsign_airline AS (
    SELECT TRIM(tracked.Callsign) AS Callsign,
           MIN(sighting.AirlineId) AS AirlineId
    FROM SIGHTING sighting
    INNER JOIN TRACKED_AIRCRAFT tracked ON tracked.Id = sighting.Id
    WHERE tracked.Callsign IS NOT NULL
      AND TRIM(tracked.Callsign) <> ''
    GROUP BY TRIM(tracked.Callsign)
)
SELECT COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       COALESCE(NULLIF(f.IATA, ''), 'Unresolved') AS "Flight",
       COALESCE(NULLIF(al.Name, ''), 'Unresolved') AS "Airline",
       COALESCE(NULLIF(pr.Source, ''), 'No local reference') AS "Provenance Source",
       COUNT(DISTINCT ta.Id) AS "Observations",
       COUNT(DISTINCT ta.SessionId) AS "Sessions",
       CASE WHEN f.Id IS NULL THEN 0 ELSE 1 END AS "Flight Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN FLIGHT f ON f.Id = (
    SELECT candidate.Id
    FROM FLIGHT candidate
    WHERE candidate.Callsign = TRIM(ta.Callsign)
    ORDER BY candidate.Id
    LIMIT 1)
LEFT JOIN callsign_airline resolution ON resolution.Callsign = TRIM(ta.Callsign)
LEFT JOIN AIRLINE al ON al.Id = COALESCE(f.AirlineId, resolution.AirlineId)
LEFT JOIN PROVENANCE pr ON pr.Id = f.ProvenanceId
GROUP BY COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign'), f.Id, f.IATA, al.Name, pr.Source
ORDER BY "Observations" DESC, "Callsign";

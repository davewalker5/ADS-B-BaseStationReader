WITH callsign_airline AS (
    SELECT TRIM(tracked.Callsign) AS Callsign,
           MIN(sighting.AirlineId) AS AirlineId
    FROM SIGHTING sighting
    INNER JOIN TRACKED_AIRCRAFT tracked ON tracked.Id = sighting.Id
    WHERE tracked.Callsign IS NOT NULL
      AND TRIM(tracked.Callsign) <> ''
    GROUP BY TRIM(tracked.Callsign)
)
SELECT COALESCE(NULLIF(TRIM(tracked.Callsign), ''), 'No callsign') AS "Callsign",
       COALESCE(NULLIF(airline.Name, ''), 'Unresolved') AS "Airline",
       COALESCE(NULLIF(provenance.Source, ''), 'No local reference') AS "Provenance Source",
       COUNT(DISTINCT tracked.Id) AS "Observations",
       COUNT(DISTINCT tracked.SessionId) AS "Sessions",
       CASE WHEN airline.Id IS NULL THEN 0 ELSE 1 END AS "Airline Identified"
FROM TRACKED_AIRCRAFT tracked
LEFT JOIN callsign_airline resolution ON resolution.Callsign = TRIM(tracked.Callsign)
LEFT JOIN AIRLINE airline ON airline.Id = resolution.AirlineId
LEFT JOIN PROVENANCE provenance ON provenance.Id = airline.ProvenanceId
GROUP BY COALESCE(NULLIF(TRIM(tracked.Callsign), ''), 'No callsign'),
         airline.Id, airline.Name, provenance.Source
ORDER BY "Observations" DESC, "Callsign";

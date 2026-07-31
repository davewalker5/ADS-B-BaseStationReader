SELECT COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       COALESCE(NULLIF(f.IATA, ''), 'Unresolved') AS "Flight",
       COALESCE(NULLIF(al.Name, ''), 'Unresolved') AS "Airline",
       COALESCE(NULLIF(pr.Source, ''), 'No local reference') AS "Provenance Source",
       COUNT(DISTINCT ta.Id) AS "Observations",
       COUNT(DISTINCT ta.SessionId) AS "Sessions",
       CASE WHEN f.Id IS NULL THEN 0 ELSE 1 END AS "Flight Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN FLIGHT f ON f.Callsign = TRIM(ta.Callsign)
LEFT JOIN AIRLINE al ON al.Id = f.AirlineId
LEFT JOIN PROVENANCE pr ON pr.Id = f.ProvenanceId
GROUP BY COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign'), f.Id, f.IATA, al.Name, pr.Source
ORDER BY "Observations" DESC, "Callsign";

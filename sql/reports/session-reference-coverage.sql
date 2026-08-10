SELECT 'Aircraft' AS "Reference Type",
       ta.Address AS "Observed Value",
       CASE WHEN aircraft.Id IS NULL THEN 0 ELSE 1 END AS "Identified",
       COALESCE(NULLIF(provenance.Source, ''), 'No local reference') AS "Provenance Source"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN AIRCRAFT aircraft ON aircraft.Address = ta.Address
LEFT JOIN PROVENANCE provenance ON provenance.Id = aircraft.ProvenanceId
WHERE ta.SessionId = $session_id
GROUP BY ta.Address, aircraft.Id, provenance.Source
UNION ALL
SELECT 'Flight',
       TRIM(ta.Callsign),
       CASE WHEN flight.Id IS NULL THEN 0 ELSE 1 END,
       COALESCE(NULLIF(provenance.Source, ''), 'No local reference')
FROM TRACKED_AIRCRAFT ta
LEFT JOIN FLIGHT flight ON flight.Callsign = TRIM(ta.Callsign)
LEFT JOIN PROVENANCE provenance ON provenance.Id = flight.ProvenanceId
WHERE ta.SessionId = $session_id
  AND NULLIF(TRIM(ta.Callsign), '') IS NOT NULL
GROUP BY TRIM(ta.Callsign), flight.Id, provenance.Source
ORDER BY "Reference Type", "Observed Value";

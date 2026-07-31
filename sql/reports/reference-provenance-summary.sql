SELECT 'Aircraft' AS "Reference Type",
       COALESCE(NULLIF(pr.Source, ''), 'No local reference') AS "Provenance Source",
       COUNT(DISTINCT ta.Address) AS "Referenced Items",
       COUNT(DISTINCT ta.Id) AS "Observations"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT JOIN PROVENANCE pr ON pr.Id = a.ProvenanceId
GROUP BY COALESCE(NULLIF(pr.Source, ''), 'No local reference')
UNION ALL
SELECT 'Flight',
       COALESCE(NULLIF(pr.Source, ''), 'No local reference'),
       COUNT(DISTINCT NULLIF(TRIM(ta.Callsign), '')),
       COUNT(DISTINCT ta.Id)
FROM TRACKED_AIRCRAFT ta
LEFT JOIN FLIGHT f ON f.Callsign = TRIM(ta.Callsign)
LEFT JOIN PROVENANCE pr ON pr.Id = f.ProvenanceId
GROUP BY COALESCE(NULLIF(pr.Source, ''), 'No local reference')
ORDER BY "Reference Type", "Observations" DESC;

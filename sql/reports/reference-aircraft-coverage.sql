SELECT ta.Address AS "Address",
       COALESCE(NULLIF(a.Registration, ''), 'Unidentified') AS "Registration",
       COALESCE(NULLIF(m.Name, ''), NULLIF(m.ICAO, ''), 'Missing') AS "Model",
       COALESCE(NULLIF(ma.Name, ''), 'Missing') AS "Manufacturer",
       COALESCE(NULLIF(pr.Source, ''), 'No local reference') AS "Provenance Source",
       COUNT(DISTINCT ta.Id) AS "Observations",
       COUNT(DISTINCT ta.SessionId) AS "Sessions",
       CASE WHEN a.Id IS NULL THEN 0 ELSE 1 END AS "Aircraft Identified",
       CASE WHEN m.Id IS NULL THEN 0 ELSE 1 END AS "Model Identified",
       CASE WHEN ma.Id IS NULL THEN 0 ELSE 1 END AS "Manufacturer Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT JOIN MODEL m ON m.Id = a.ModelId
LEFT JOIN MANUFACTURER ma ON ma.Id = m.ManufacturerId
LEFT JOIN PROVENANCE pr ON pr.Id = a.ProvenanceId
GROUP BY ta.Address, a.Registration, m.Id, m.Name, m.ICAO, ma.Id, ma.Name, a.Id, pr.Source
ORDER BY "Observations" DESC, ta.Address;

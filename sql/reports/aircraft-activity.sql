SELECT ta.Address AS "Address",
       COALESCE(NULLIF(a.Registration, ''), 'Unidentified') AS "Registration",
       COALESCE(NULLIF(m.Name, ''), NULLIF(m.ICAO, ''), 'Unknown') AS "Aircraft Type",
       COALESCE(NULLIF(ma.Name, ''), 'Unknown') AS "Manufacturer",
       CASE
           WHEN NULLIF(TRIM(ta.Callsign), '') IS NULL THEN 'No callsign'
           WHEN al.Id IS NULL THEN 'Unresolved callsign'
           ELSE COALESCE(NULLIF(al.Name, ''), 'Unnamed airline')
       END AS "Operator",
       MIN(ta.FirstSeen) AS "First Observation",
       MAX(ta.LastSeen) AS "Most Recent Observation",
       COUNT(DISTINCT ta.SessionId) AS "Sessions",
       COUNT(DISTINCT ta.Id) AS "Observations",
       SUM(CASE WHEN a.Id IS NULL THEN 0 ELSE 1 END) > 0 AS "Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT JOIN MODEL m ON m.Id = a.ModelId
LEFT JOIN MANUFACTURER ma ON ma.Id = m.ManufacturerId
LEFT JOIN SIGHTING s ON s.Id = ta.Id
LEFT JOIN AIRLINE al ON al.Id = s.AirlineId
GROUP BY ta.Address, a.Registration, m.Name, m.ICAO, ma.Name,
         CASE
             WHEN NULLIF(TRIM(ta.Callsign), '') IS NULL THEN 'No callsign'
             WHEN al.Id IS NULL THEN 'Unresolved callsign'
             ELSE COALESCE(NULLIF(al.Name, ''), 'Unnamed airline')
         END
ORDER BY "Observations" DESC, ta.Address;

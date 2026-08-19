SELECT ta.Id AS "Observation Id",
       ta.Address AS "Address",
       COALESCE(NULLIF(a.Registration, ''), 'Unidentified') AS "Registration",
       a.Manufactured AS "Manufacture Year",
       COALESCE(NULLIF(m.ICAO, ''), 'Unknown') AS "ICAO Type",
       COALESCE(NULLIF(m.Name, ''), 'Unknown') AS "Model",
       COALESCE(NULLIF(manufacturer.Name, ''), 'Unknown') AS "Manufacturer",
       ta.SessionId AS "Session Id",
       'Session ' || ta.SessionId AS "Session Name",
       s.ProfileName AS "Session Type",
       s.StartedAtUtc AS "Session Started At UTC",
       ta.FirstSeen AS "First Observation",
       ta.LastSeen AS "Last Observation",
       CASE
           WHEN a.Manufactured BETWEEN 1900 AND CAST(STRFTIME('%Y', ta.FirstSeen) AS INTEGER)
           THEN CAST(STRFTIME('%Y', ta.FirstSeen) AS INTEGER) - a.Manufactured
       END AS "Age At Observation"
FROM TRACKED_AIRCRAFT ta
JOIN SESSION s ON s.Id = ta.SessionId
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT JOIN MODEL m ON m.Id = a.ModelId
LEFT JOIN MANUFACTURER manufacturer ON manufacturer.Id = m.ManufacturerId
ORDER BY ta.FirstSeen, ta.Address;

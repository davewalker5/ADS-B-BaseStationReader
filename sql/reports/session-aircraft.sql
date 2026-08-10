SELECT ta.Address AS "Address",
       COALESCE(NULLIF(a.Registration, ''), 'Unidentified') AS "Registration",
       COALESCE(NULLIF(m.Name, ''), NULLIF(m.ICAO, ''), 'Unknown') AS "Aircraft Type",
       COALESCE(NULLIF(manufacturer.Name, ''), 'Unknown') AS "Manufacturer",
       COALESCE(NULLIF(airline.Name, ''), 'Unknown') AS "Operator",
       MIN(ta.FirstSeen) AS "First Observation",
       MAX(ta.LastSeen) AS "Last Observation",
       ROUND((JULIANDAY(MAX(ta.LastSeen)) - JULIANDAY(MIN(ta.FirstSeen))) * 86400.0) AS "Observation Span Seconds",
       COUNT(DISTINCT ta.Id) AS "Aircraft Observations",
       COUNT(p.Id) AS "Position Records",
       MAX(p.Distance) AS "Maximum Range",
       CASE WHEN a.Id IS NULL THEN 0 ELSE 1 END AS "Aircraft Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN POSITION p ON p.AircraftId = ta.Id
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT JOIN MODEL m ON m.Id = a.ModelId
LEFT JOIN MANUFACTURER manufacturer ON manufacturer.Id = m.ManufacturerId
LEFT JOIN FLIGHT flight ON flight.Callsign = TRIM(ta.Callsign)
LEFT JOIN AIRLINE airline ON airline.Id = flight.AirlineId
WHERE ta.SessionId = $session_id
GROUP BY ta.Address, a.Id, a.Registration, m.Name, m.ICAO, manufacturer.Name, airline.Name
ORDER BY "Position Records" DESC, "Observation Span Seconds" DESC, ta.Address;

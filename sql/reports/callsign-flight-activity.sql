SELECT COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       COALESCE(NULLIF(f.IATA, ''), 'Unresolved') AS "Flight",
       CASE
           WHEN NULLIF(TRIM(ta.Callsign), '') IS NULL THEN 'No callsign'
           WHEN al.Id IS NULL THEN 'Unresolved callsign'
           ELSE COALESCE(NULLIF(al.Name, ''), 'Unnamed airline')
       END AS "Airline",
       CASE
           WHEN origin.Id IS NOT NULL AND destination.Id IS NOT NULL
           THEN origin.IATA || ' - ' || destination.IATA
           ELSE 'Unresolved'
       END AS "Route",
       MIN(ta.FirstSeen) AS "First Observation",
       MAX(ta.LastSeen) AS "Most Recent Observation",
       COUNT(DISTINCT ta.Id) AS "Observations",
       COUNT(DISTINCT ta.SessionId) AS "Sessions",
       COUNT(DISTINCT ta.Address) AS "Aircraft",
       CASE WHEN f.Id IS NULL THEN 0 ELSE 1 END AS "Resolved"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN SIGHTING s ON s.Id = ta.Id
LEFT JOIN FLIGHT f ON f.Id = s.FlightId
LEFT JOIN AIRLINE al ON al.Id = s.AirlineId
LEFT JOIN AIRPORT origin ON origin.Id = f.OriginAirportId
LEFT JOIN AIRPORT destination ON destination.Id = f.DestinationAirportId
GROUP BY COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign'),
         f.IATA,
         CASE
             WHEN NULLIF(TRIM(ta.Callsign), '') IS NULL THEN 'No callsign'
             WHEN al.Id IS NULL THEN 'Unresolved callsign'
             ELSE COALESCE(NULLIF(al.Name, ''), 'Unnamed airline')
         END,
         origin.Id, origin.IATA, destination.Id, destination.IATA, f.Id
ORDER BY "Observations" DESC, "Callsign";

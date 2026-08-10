SELECT COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       COUNT(DISTINCT ta.Id) AS "Aircraft Observations",
       COUNT(DISTINCT ta.Address) AS "Aircraft",
       MIN(ta.FirstSeen) AS "First Observation",
       MAX(ta.LastSeen) AS "Last Observation",
       COALESCE(NULLIF(flight.IATA, ''), 'Unresolved') AS "Flight",
       COALESCE(NULLIF(airline.Name, ''), 'Unresolved') AS "Airline",
       COALESCE(NULLIF(origin.IATA, ''), 'Unresolved') AS "Origin",
       COALESCE(NULLIF(destination.IATA, ''), 'Unresolved') AS "Destination",
       CASE WHEN flight.Id IS NULL THEN 0 ELSE 1 END AS "Flight Identified"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN FLIGHT flight ON flight.Callsign = TRIM(ta.Callsign)
LEFT JOIN AIRLINE airline ON airline.Id = flight.AirlineId
LEFT JOIN AIRPORT origin ON origin.Id = flight.OriginAirportId
LEFT JOIN AIRPORT destination ON destination.Id = flight.DestinationAirportId
WHERE ta.SessionId = $session_id
GROUP BY COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign'),
         flight.Id, flight.IATA, airline.Name, origin.IATA, destination.IATA
ORDER BY "Aircraft Observations" DESC, "Callsign";

SELECT p.Timestamp AS "Timestamp",
       ta.SessionId AS "Session Id",
       ta.Address AS "Address",
       COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       p.Latitude AS "Latitude",
       p.Longitude AS "Longitude",
       p.Altitude AS "Altitude",
       p.Distance AS "Distance"
FROM POSITION p
INNER JOIN TRACKED_AIRCRAFT ta ON ta.Id = p.AircraftId
WHERE p.Latitude IS NOT NULL
  AND p.Longitude IS NOT NULL
ORDER BY p.Timestamp;

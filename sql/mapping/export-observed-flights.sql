SELECT DISTINCT a.Name AS "Airline",
                f.Number AS "FlightIATA",
                f.Embarkation AS "Embarkation",
                f.Destination AS "Destination"
FROM            SIGHTING s
INNER JOIN      FLIGHT f ON f.Id = s.flight_id
INNER JOIN      AIRLINE a ON a.Id = f.airline_id
WHERE           DATE( s.Date ) >= DATE('now', '-' || 90 || ' days');

SELECT DISTINCT a.Name AS "Airline",
                f.Number AS "FlightIATA",
                f.Embarkation AS "Embarkation",
                f.Destination AS "Destination"
FROM            SIGHTING s
INNER JOIN      FLIGHT f ON f.Id = s.FlightId
INNER JOIN      AIRLINE a ON a.Id = f.AirlineId
WHERE           DATE(s.Timestamp) >= DATE('now', '-90 days');

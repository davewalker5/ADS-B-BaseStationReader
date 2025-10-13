SELECT DISTINCT a.Name, f.Number, f.Embarkation, f.Destination
FROM            SIGHTING s
INNER JOIN      FLIGHT f ON f.Id = s.flight_id
INNER JOIN      AIRLINE a ON a.Id = f.airline_id
WHERE           DATE( s.Date ) >= DATE('now', '-' || 90 || ' days');

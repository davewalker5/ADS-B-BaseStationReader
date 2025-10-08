SELECT ta.Id, ta.Address, ta.LastSeen, ta.LookupTimestamp, f.Embarkation, f.Destination, f.Number, s.Timestamp
FROM TRACKED_AIRCRAFT ta
INNER JOIN AIRCRAFT a ON a.Address == ta.Address
INNER JOIN SIGHTING s ON s.AircraftId = a.Id
INNER JOIN FLIGHT f ON f.Id = s.FlightId
WHERE LookupTimestamp IS NULL
AND s.Timestamp LIKE '2025-10-02%';
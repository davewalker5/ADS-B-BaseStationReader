SELECT      s.Timestamp,
            f.IATA,
            f.Embarkation,
            f.Destination,
            a.Address,
            a.Registration,
            a.Manufactured,
            a.Age,
            m.Name AS "Model",
            ma.Name AS "Manufacturer"
FROM        SIGHTING s
INNER JOIN  AIRCRAFT a ON a.Id = s.AircraftId
INNER JOIN  MODEL m ON m.Id = a.ModelId
INNER JOIN  MANUFACTURER ma ON ma.Id = m.ManufacturerId
INNER JOIN  FLIGHT f ON f.Id = s.FlightId
ORDER BY    s.Timestamp ASC;
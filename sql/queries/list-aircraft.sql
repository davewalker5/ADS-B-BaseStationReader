SELECT      a.Address,
            a.Registration,
            m.IATA,
            m.ICAO,
            a.Manufactured
FROM        AIRCRAFT a
INNER JOIN  MODEL m ON m.Id = a.ModelId;

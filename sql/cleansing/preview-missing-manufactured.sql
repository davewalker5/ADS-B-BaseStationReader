SELECT      a.Address,
            a.Registration,
            m.ICAO,
            m.Name AS "Model",
            ma.Name AS "Manufacturer"
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  AIRCRAFT a ON a.Address = ta.Address
INNER JOIN  MODEL m ON m.Id = a.ModelId
INNER JOIN  MANUFACTURER ma ON ma.Id = m.ManufacturerId
WHERE       a.Manufactured IS NULL;

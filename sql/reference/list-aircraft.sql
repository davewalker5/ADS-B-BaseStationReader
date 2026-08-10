SELECT      a.Address,
            a.Registration,
            m.IATA AS "Model_IATA",
            m.ICAO AS "Model_ICAO",
            m.Name AS "Model_Name",
            ma.Name AS "Manufacturer_Name",
            a.Manufactured
FROM        AIRCRAFT a
INNER JOIN  MODEL m ON m.Id = a.ModelId
INNER JOIN  MANUFACTURER ma ON ma.Id = m.ManufacturerId;

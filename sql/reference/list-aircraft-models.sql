SELECT      m.IATA,
            m.ICAO,
            m.Name,
            ma.Name AS "Manufacturer"
FROM        MODEL m
INNER JOIN  MANUFACTURER ma ON ma.Id = m.ManufacturerId
ORDER BY    m.IATA ASC;

SELECT      m.ICAO,
            m.IATA,
            m.Name,
            ma.Name AS "Manufacturer"
FROM        MODEL m
INNER JOIN  MANUFACTURER ma ON ma.Id = m.ManufacturerId
WHERE       m.ICAO = '';

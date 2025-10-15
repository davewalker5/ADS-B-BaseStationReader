SELECT      m.ICAO,
            m.IATA,
            m.ManufacturerId,
            COUNT( m.Id )
FROM        MODEL m
GROUP BY    m.ICAO,
            m.IATA,
            m.ManufacturerId
HAVING      COUNT( m.Id ) > 1;